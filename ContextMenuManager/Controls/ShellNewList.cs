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
using System.Windows.Forms;
using System.Xml;

namespace ContextMenuManager.Controls
{
    sealed class ShellNewList : MyList // 主页 新建菜单
    {
        public const string ShellNewPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Discardable\PostSetup\ShellNew";

        public ShellNewSeparator Separator;

        public void LoadItems()
        {
            AddNewItem();
#if DEBUG
            if (AppConfig.EnableLog)
            {
                using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                {
                    sw.WriteLine($@"LoadShellNewItems:");
                }
            }
            int i = 0;
#endif
            // TODO:加入可以锁定的功能；新建菜单可以进行排序
            ShellNewLockItem item = new ShellNewLockItem(this);
            AddItem(item);
#if DEBUG
            string regPath = item.RegPath;
            string valueName = item.ValueName;
            string itemName = item.Text;
            bool ifItemInMenu = item.ItemVisible;
            i++;
            if (AppConfig.EnableLog)
            {
                using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                {
                    sw.WriteLine("\tShellNewLockItems");
                    sw.WriteLine("\t\t" + $@"{i}. {valueName} {itemName} {ifItemInMenu} {regPath}");
                }
            }
#endif
            Separator = new ShellNewSeparator();
            AddItem(Separator);
            if (ShellNewLockItem.IsLocked) LoadLockItems();
            else LoadUnlockItems();
        }

        /// <summary>直接扫描所有扩展名</summary>
        private void LoadUnlockItems()
        {
#if DEBUG
            if (AppConfig.EnableLog)
            {
                using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                {
                    sw.WriteLine("\tLoadUnlockItems");
                }
            }
#endif
            List<string> extensions = new List<string> { "Folder" };//文件夹
            using (RegistryKey root = Registry.ClassesRoot)
            {
                extensions.AddRange(Array.FindAll(root.GetSubKeyNames(), keyName => keyName.StartsWith(".")));
                if (WinOsVersion.Current < WinOsVersion.Win10) extensions.Add("Briefcase");//公文包(Win10没有)
                LoadItems(extensions);
            }
        }

        /// <summary>根据ShellNewPath的Classes键值扫描</summary>
        private void LoadLockItems()
        {
#if DEBUG
            if (AppConfig.EnableLog)
            {
                using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                {
                    sw.WriteLine("\tLoadLockItems");
                }
            }
#endif
            string[] extensions = (string[])Registry.GetValue(ShellNewPath, "Classes", null);
            LoadItems(extensions.ToList());
        }

        private void LoadItems(List<string> extensions)
        {
#if DEBUG
            int i = 0;
#endif
            foreach (string extension in ShellNewItem.UnableSortExtensions)
            {
                if (extensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    extensions.Remove(extension);
                    extensions.Insert(0, extension);
                }
            }
            using (RegistryKey root = Registry.ClassesRoot)
            {
                foreach (string extension in extensions)
                {
                    using (RegistryKey extKey = root.OpenSubKey(extension))
                    {
                        string defalutOpenMode = extKey?.GetValue("")?.ToString();
                        if (string.IsNullOrEmpty(defalutOpenMode) || defalutOpenMode.Length > 255) continue;
                        using (RegistryKey openModeKey = root.OpenSubKey(defalutOpenMode))
                        {
                            if (openModeKey == null) continue;
                            string value1 = openModeKey.GetValue("FriendlyTypeName")?.ToString();
                            string value2 = openModeKey.GetValue("")?.ToString();
                            value1 = ResourceString.GetDirectString(value1);
                            if (value1.IsNullOrWhiteSpace() && value2.IsNullOrWhiteSpace()) continue;
                        }
                        using (RegistryKey tKey = extKey.OpenSubKey(defalutOpenMode))
                        {
                            foreach (string part in ShellNewItem.SnParts)
                            {
                                string snPart = part;
                                if (tKey != null) snPart = $@"{defalutOpenMode}\{snPart}";
                                using (RegistryKey snKey = extKey.OpenSubKey(snPart))
                                {
                                    if (ShellNewItem.EffectValueNames.Any(valueName => snKey?.GetValue(valueName) != null))
                                    {
                                        ShellNewItem item = new ShellNewItem(snKey.Name, this);
#if DEBUG
                                        string regPath = item.RegPath;
                                        string openMode = item.OpenMode;
                                        string itemName = item.Text;
                                        bool ifItemInMenu = item.ItemVisible;
                                        i++;
                                        if (AppConfig.EnableLog)
                                        {
                                            using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                                            {
                                                sw.WriteLine("\t\t" + $@"{i}. {openMode} {itemName} {ifItemInMenu} {regPath}");
                                            }
                                        }
#endif
                                        if (item.BeforeSeparator)
                                        {
                                            int index2 = GetItemIndex(Separator);
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
                    }
                }
            }
        }

        public void MoveItem(ShellNewItem shellNewItem, bool isUp)
        {
            int index = GetItemIndex(shellNewItem);
            index += isUp ? -1 : 1;
            if (index == Controls.Count) return;
            Control ctr = Controls[index];
            if (ctr is ShellNewItem item && item.CanSort)
            {
                SetItemIndex(shellNewItem, index);
                SaveSorting();
            }
        }

        public void SaveSorting()
        {
            List<string> extensions = new List<string>();
            for (int i = 2; i < Controls.Count; i++)
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
            NewItem newItem = new NewItem();
            AddItem(newItem);
            newItem.AddNewItem += () =>
            {
                using (FileExtensionDialog dlg = new FileExtensionDialog())
                {
                    if (dlg.ShowDialog() != DialogResult.OK) return;
                    string extension = dlg.Extension;
                    if (extension == ".") return;
                    string openMode = FileExtension.GetOpenMode(extension);
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
                        if (ctr is ShellNewItem item)
                        {
                            if (item.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase))
                            {
                                AppMessageBox.Show(AppString.Message.HasBeenAdded);
                                return;
                            }
                        }
                    }

                    using (RegistryKey root = Registry.ClassesRoot)
                    using (RegistryKey exKey = root.OpenSubKey(extension, true))
                    using (RegistryKey snKey = exKey.CreateSubKey("ShellNew", true))
                    {
                        string defaultOpenMode = exKey.GetValue("")?.ToString();
                        if (string.IsNullOrEmpty(defaultOpenMode)) exKey.SetValue("", openMode);

                        byte[] bytes = GetWebShellNewData(extension);
                        if (bytes != null) snKey.SetValue("Data", bytes, RegistryValueKind.Binary);
                        else snKey.SetValue("NullFile", "", RegistryValueKind.String);

                        ShellNewItem item = new ShellNewItem(snKey.Name, this);
                        AddItem(item);
                        item.Focus();
                        if (item.ItemText.IsNullOrWhiteSpace())
                        {
                            item.ItemText = FileExtension.GetExtentionInfo(FileExtension.AssocStr.FriendlyDocName, extension);
                        }
                        if (ShellNewLockItem.IsLocked) SaveSorting();
                    }
                }
            };
        }

        private static byte[] GetWebShellNewData(string extension)
        {
            string apiUrl = AppConfig.RequestUseGithub ? AppConfig.GithubShellNewApi : AppConfig.GiteeShellNewApi;
            using (UAWebClient client = new UAWebClient())
            {
                XmlDocument doc = client.GetWebJsonToXml(apiUrl);
                if (doc == null) return null;
                foreach (XmlNode node in doc.FirstChild.ChildNodes)
                {
                    XmlNode nameXN = node.SelectSingleNode("name");
                    string str = Path.GetExtension(nameXN.InnerText);
                    if (string.Equals(str, extension, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            string dirUrl = AppConfig.RequestUseGithub ? AppConfig.GithubShellNewRawDir : AppConfig.GiteeShellNewRawDir;
                            string fileUrl = $"{dirUrl}/{nameXN.InnerText}";
                            return client.DownloadData(fileUrl);
                        }
                        catch { return null; }
                    }
                }
                return null;
            }
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
                    using (RegistryKey key = RegistryEx.GetRegistryKey(ShellNewPath))
                    {
                        RegistrySecurity rs = key.GetAccessControl();
                        foreach (RegistryAccessRule rar in rs.GetAccessRules(true, true, typeof(NTAccount)))
                        {
                            if (rar.AccessControlType.ToString().Equals("Deny", StringComparison.OrdinalIgnoreCase))
                            {
                                if (rar.IdentityReference.ToString().Equals("Everyone", StringComparison.OrdinalIgnoreCase)) return true;
                            }
                        }
                    }
                    return false;
                }
            }

            public static void Lock()
            {
                using (RegistryKey key = RegistryEx.GetRegistryKey(ShellNewPath, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions))
                {
                    RegistrySecurity rs = new RegistrySecurity();
                    RegistryAccessRule rar = new RegistryAccessRule("Everyone", RegistryRights.Delete | RegistryRights.WriteKey, AccessControlType.Deny);
                    rs.AddAccessRule(rar);
                    key.SetAccessControl(rs);
                }
            }

            public static void UnLock()
            {
                using (RegistryKey key = RegistryEx.GetRegistryKey(ShellNewPath, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions))
                {
                    RegistrySecurity rs = key.GetAccessControl();
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