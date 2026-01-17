using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Controls.Interfaces;
using ContextMenuManager.Methods;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace ContextMenuManager.Controls
{
    internal sealed class ShellNewList : MyList // 主页 新建菜单
    {
        public const string ShellNewPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Discardable\PostSetup\ShellNew";

        public ShellNewSeparator Separator;

        public void LoadItems()
        {
            AddNewItem();
            var item = new ShellNewLockItem(this);
            AddItem(item);
            Separator = new ShellNewSeparator();
            AddItem(Separator);
            if (ShellNewLockItem.IsLocked) LoadLockItems();
            else LoadUnlockItems();
        }

        /// <summary>直接扫描所有扩展名</summary>
        private void LoadUnlockItems()
        {
            var extensions = new List<string> { "Folder" };//文件夹
            using var root = Registry.ClassesRoot;
            extensions.AddRange(Array.FindAll(root.GetSubKeyNames(), keyName => keyName.StartsWith(".")));
            if (WinOsVersion.Current < WinOsVersion.Win10) extensions.Add("Briefcase");//公文包(Win10没有)
            LoadItems(extensions);
        }

        /// <summary>根据ShellNewPath的Classes键值扫描</summary>
        private void LoadLockItems()
        {
            var extensions = (string[])Registry.GetValue(ShellNewPath, "Classes", null);
            LoadItems(extensions.ToList());
        }

        private void LoadItems(List<string> extensions)
        {
            foreach (var extension in ShellNewItem.UnableSortExtensions)
            {
                if (extensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    extensions.Remove(extension);
                    extensions.Insert(0, extension);
                }
            }
            using var root = Registry.ClassesRoot;
            foreach (var extension in extensions)
            {
                using var extKey = root.OpenSubKey(extension);
                var defalutOpenMode = extKey?.GetValue("")?.ToString();
                if (string.IsNullOrEmpty(defalutOpenMode) || defalutOpenMode.Length > 255) continue;
                using (var openModeKey = root.OpenSubKey(defalutOpenMode))
                {
                    if (openModeKey == null) continue;
                    var value1 = openModeKey.GetValue("FriendlyTypeName")?.ToString();
                    var value2 = openModeKey.GetValue("")?.ToString();
                    value1 = ResourceString.GetDirectString(value1);
                    if (value1.IsNullOrWhiteSpace() && value2.IsNullOrWhiteSpace()) continue;
                }
                using var tKey = extKey.OpenSubKey(defalutOpenMode);
                foreach (var part in ShellNewItem.SnParts)
                {
                    var snPart = part;
                    if (tKey != null) snPart = $@"{defalutOpenMode}\{snPart}";
                    using var snKey = extKey.OpenSubKey(snPart);
                    if (ShellNewItem.EffectValueNames.Any(valueName => snKey?.GetValue(valueName) != null))
                    {
                        var item = new ShellNewItem(snKey.Name, this);
                        if (item.BeforeSeparator)
                        {
                            var index2 = GetItemIndex(Separator);
                            InsertItem(item, index2);
                        }
                        else
                        {
                            AddItem(item);
                        }
                        break;
                    }
                }
            }
        }

        public void MoveItem(ShellNewItem shellNewItem, bool isUp)
        {
            var index = GetItemIndex(shellNewItem);
            index += isUp ? -1 : 1;
            if (index == Controls.Count) return;
            var ctr = Controls[index];
            if (ctr is ShellNewItem item && item.CanSort)
            {
                SetItemIndex(shellNewItem, index);
                SaveSorting();
            }
        }

        public void SaveSorting()
        {
            var extensions = new List<string>();
            for (var i = 2; i < Controls.Count; i++)
            {
                if (Controls[i] is ShellNewItem item)
                {
                    extensions.Add(item.Extension);
                }
            }
            ShellNewLockItem.UnLock();
            Registry.SetValue(ShellNewPath, "Classes", extensions.ToArray());
            ShellNewLockItem.Lock();
        }

        private void AddNewItem()
        {
            var newItem = new NewItem();
            AddItem(newItem);
            newItem.AddNewItem += async () =>
            {
                using var dlg = new FileExtensionDialog();
                if (dlg.ShowDialog() != DialogResult.OK) return;
                var extension = dlg.Extension;
                if (extension == ".") return;
                var openMode = FileExtension.GetOpenMode(extension);
                if (string.IsNullOrEmpty(openMode))
                {
                    if (AppMessageBox.Show(AppString.Message.NoOpenModeExtension,
                        MessageBoxButtons.OKCancel) == DialogResult.OK)
                    {
                        ExternalProgram.ShowOpenWithDialog(extension);
                    }
                    return;
                }
                foreach (Control ctr in Controls)
                {
                    if (ctr is ShellNewItem shellItem)
                    {
                        if (shellItem.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase))
                        {
                            AppMessageBox.Show(AppString.Message.HasBeenAdded);
                            return;
                        }
                    }
                }

                using var root = Registry.ClassesRoot;
                using var exKey = root.OpenSubKey(extension, true) ?? root.CreateSubKey(extension, true);
                using var snKey = exKey.CreateSubKey("ShellNew", true);
                var defaultOpenMode = exKey.GetValue("")?.ToString();
                if (string.IsNullOrEmpty(defaultOpenMode)) exKey.SetValue("", openMode);

                var bytes = await GetWebShellNewData(extension);
                if (bytes != null) snKey.SetValue("Data", bytes, RegistryValueKind.Binary);
                else snKey.SetValue("NullFile", "", RegistryValueKind.String);

                var item = new ShellNewItem(snKey.Name, this);
                AddItem(item);
                item.Focus();
                if (item.ItemText.IsNullOrWhiteSpace())
                {
                    item.ItemText = FileExtension.GetExtentionInfo(FileExtension.AssocStr.FriendlyDocName, extension);
                }
                if (ShellNewLockItem.IsLocked) SaveSorting();
            };
        }

        private static async Task<byte[]> GetWebShellNewData(string extension)
        {
            var apiUrl = AppConfig.RequestUseGithub ? AppConfig.GithubShellNewApi : AppConfig.GiteeShellNewApi;
            using var client = new UAWebClient();
            var doc = await client.GetWebJsonToXmlAsync(apiUrl);
            if (doc == null) return null;
            foreach (XmlNode node in doc.FirstChild.ChildNodes)
            {
                var nameXN = node.SelectSingleNode("name");
                var str = Path.GetExtension(nameXN.InnerText);
                if (string.Equals(str, extension, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var dirUrl = AppConfig.RequestUseGithub ? AppConfig.GithubShellNewRawDir : AppConfig.GiteeShellNewRawDir;
                        var fileUrl = $"{dirUrl}/{nameXN.InnerText}";
                        return await client.GetWebDataAsync(fileUrl);
                    }
                    catch { return null; }
                }
            }
            return null;
        }

        public sealed class ShellNewLockItem : MyListItem, IChkVisibleItem, IBtnShowMenuItem, ITsiWebSearchItem, ITsiRegPathItem
        {
            public ShellNewLockItem(ShellNewList list)
            {
                Owner = list;
                Image = AppImage.Lock;
                Text = AppString.Other.LockNewMenu;
                BtnShowMenu = new MenuButton(this);
                ChkVisible = new VisibleCheckBox(this) { Checked = IsLocked };
                ToolTipBox.SetToolTip(ChkVisible, AppString.Tip.LockNewMenu);
                TsiSearch = new WebSearchMenuItem(this);
                TsiRegLocation = new RegLocationMenuItem(this);
                ContextMenuStrip.Items.AddRange(new ToolStripItem[]
                    { TsiSearch, new RToolStripSeparator(), TsiRegLocation });
            }

            public MenuButton BtnShowMenu { get; set; }
            public WebSearchMenuItem TsiSearch { get; set; }
            public RegLocationMenuItem TsiRegLocation { get; set; }
            public VisibleCheckBox ChkVisible { get; set; }
            public ShellNewList Owner { get; private set; }

            public bool ItemVisible // 锁定新建菜单是否锁定
            {
                get => IsLocked;
                set
                {
                    if (value) Owner.SaveSorting();
                    else UnLock();
                    foreach (Control ctr in Owner.Controls)
                    {
                        if (ctr is ShellNewItem item)
                        {
                            item.SetSortabled(value);
                        }
                    }
                }
            }

            public string SearchText => Text;
            public string RegPath => ShellNewPath;
            public string ValueName => "Classes";

            public static bool IsLocked
            {
                get
                {
                    using var key = RegistryEx.GetRegistryKey(ShellNewPath);
                    var rs = key.GetAccessControl();
                    foreach (RegistryAccessRule rar in rs.GetAccessRules(true, true, typeof(NTAccount)))
                    {
                        if (rar.AccessControlType.ToString().Equals("Deny", StringComparison.OrdinalIgnoreCase))
                        {
                            if (rar.IdentityReference.ToString().Equals("Everyone", StringComparison.OrdinalIgnoreCase)) return true;
                        }
                    }
                    return false;
                }
            }

            public static void Lock()
            {
                using var key = RegistryEx.GetRegistryKey(ShellNewPath, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions);
                var rs = new RegistrySecurity();
                var rar = new RegistryAccessRule("Everyone", RegistryRights.Delete | RegistryRights.WriteKey, AccessControlType.Deny);
                rs.AddAccessRule(rar);
                key.SetAccessControl(rs);
            }

            public static void UnLock()
            {
                using var key = RegistryEx.GetRegistryKey(ShellNewPath, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions);
                var rs = key.GetAccessControl();
                foreach (RegistryAccessRule rar in rs.GetAccessRules(true, true, typeof(NTAccount)))
                {
                    if (rar.AccessControlType.ToString().Equals("Deny", StringComparison.OrdinalIgnoreCase))
                    {
                        if (rar.IdentityReference.ToString().Equals("Everyone", StringComparison.OrdinalIgnoreCase))
                        {
                            rs.RemoveAccessRule(rar);
                        }
                    }
                }
                key.SetAccessControl(rs);
            }
        }

        public sealed class ShellNewSeparator : MyListItem
        {
            public ShellNewSeparator()
            {
                Text = AppString.Other.Separator;
                HasImage = false;
            }
        }
    }
}