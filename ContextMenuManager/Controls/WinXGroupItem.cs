using ContextMenuManager.Controls.Interfaces;
using ContextMenuManager.Methods;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace ContextMenuManager.Controls
{
    internal sealed class WinXGroupItem : FoldGroupItem, IChkVisibleItem, ITsiDeleteItem, ITsiTextItem
    {
        public new WinXList List;

        public WinXGroupItem(WinXList list, string groupPath) : base(list, groupPath, ObjectPath.PathType.Directory)
        {
            List = list;
            RefreshKeyPath();
            if (List != null)
            {
                InitializeComponents();
            }
        }

        private string keyPath = null;
        private void RefreshKeyPath()
        {
            if (WinOsVersion.Current >= WinOsVersion.Win11)
            {
                keyPath = GroupPath[WinXList.WinXPath.Length..];
            }
        }

        private string BackupGroupPath => $@"{WinXList.BackupWinXPath}{keyPath}";
        private string DefaultGroupPath => $@"{WinXList.DefaultWinXPath}{keyPath}";

        private string DefaultFolderPath => $@"{((WinOsVersion.Current >= WinOsVersion.Win11) ? WinXList.WinXDefaultPath : WinXList.DefaultWinXPath)}\{ItemText}";

        public bool ignoreChange = false;

        public bool ItemVisible
        {
            get => (WinOsVersion.Current >= WinOsVersion.Win11) ?
                    Directory.Exists(GroupPath) && Directory.GetFiles(GroupPath, "*.lnk").Length != 0 :
                    (File.GetAttributes(GroupPath) & FileAttributes.Hidden) != FileAttributes.Hidden;
            set
            {
                if (WinOsVersion.Current >= WinOsVersion.Win11)
                {
                    if (ignoreChange)
                    {
                        // 在WinXItem的启用禁用导致的WinXGroupItem的启用禁用改变，不触发ItemVisible的set方法
                        ignoreChange = false; return;
                    }

                    var flag = false;
                    foreach (var item in winXItems)
                    {
                        if (item.ChkChecked != value)
                        {
                            item.ChkChecked = value;
                            flag = true;
                        }
                    }

                    // 在WinXGroupItem的启用禁用导致的WinXItem的启用禁用改变，下一次会触发ItemVisible的set方法
                    ignoreChange = false;

                    if (value)
                    {
                        DeletePath([BackupGroupPath]);
                    }
                    if (flag) ExplorerRestarter.Show();
                }
                else
                {
                    var attributes = File.GetAttributes(GroupPath);
                    if (value) attributes &= ~FileAttributes.Hidden;
                    else attributes |= FileAttributes.Hidden;
                    File.SetAttributes(GroupPath, attributes);
                    if (Directory.GetFiles(GroupPath, "*.lnk").Length > 0) ExplorerRestarter.Show();
                }
            }
        }

        public string ItemText
        {
            get => Path.GetFileNameWithoutExtension(GroupPath);
            set
            {
                static void MoveDirectory(string oldPath, string newPath)
                {
                    if (Directory.Exists(oldPath))
                    {
                        if (Directory.Exists(newPath)) Directory.Delete(newPath, true);
                        Directory.Move(oldPath, newPath);
                    }
                }

                var newKeyPath = $@"\{ObjectPath.RemoveIllegalChars(value)}";
                var newGroupPath = $@"{WinXList.WinXPath}{newKeyPath}";
                MoveDirectory(GroupPath, newGroupPath);

                if (WinOsVersion.Current >= WinOsVersion.Win11)
                {
                    var newBackupGroupPath = $@"{WinXList.BackupWinXPath}{newKeyPath}";
                    MoveDirectory(BackupGroupPath, newBackupGroupPath);

                    var newDefaultGroupPath = $@"{WinXList.DefaultWinXPath}{newKeyPath}";
                    MoveDirectory(DefaultGroupPath, newDefaultGroupPath);

                    keyPath = newKeyPath;
                }

                GroupPath = newGroupPath;

                RefreshList();
                ExplorerRestarter.Show();
            }
        }

        private readonly List<WinXItem> winXItems = new() { };

        public void AddWinXItem(WinXItem item)
        {
            winXItems.Add(item);
        }
        public void RemoveWinXItem(WinXItem item)
        {
            winXItems.Remove(item);
        }

        public VisibleCheckBox ChkVisible { get; set; }
        public DeleteMeMenuItem TsiDeleteMe { get; set; }
        public ChangeTextMenuItem TsiChangeText { get; set; }
        private RToolStripMenuItem TsiRestoreDefault { get; set; }

        public bool ChkChecked
        {
            get => ItemVisible;
            set => ChkVisible.IsOn = value;
        }

        private void InitializeComponents()
        {
            TsiRestoreDefault = new(AppString.Menu.RestoreDefault);
            ChkVisible = new VisibleCheckBox(this);
            SetCtrIndex(ChkVisible, 2);
            TsiDeleteMe = new DeleteMeMenuItem(this);
            TsiChangeText = new ChangeTextMenuItem(this);

            foreach (var item in new Control[] { new RToolStripSeparator(),
                TsiChangeText, TsiRestoreDefault, new RToolStripSeparator(), TsiDeleteMe })
            {
                ContextMenu.Items.Add(item);
            }

            ContextMenu.Opened += (sender, e) => TsiRestoreDefault.IsEnabled = Directory.Exists(DefaultFolderPath);
            TsiRestoreDefault.Click += (sender, e) => RestoreDefault();
        }

        private void RefreshList()
        {
            List.ClearItems();
            List.LoadItems();
        }

        private void RestoreDefault()
        {
            if (AppMessageBox.Show(AppString.Message.RestoreDefault, null, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                void RestoreDefaultFolder(bool isWinX)
                {
                    var meGroupPath = isWinX ? GroupPath : DefaultGroupPath;

                    File.SetAttributes(meGroupPath, FileAttributes.Normal);
                    Directory.Delete(meGroupPath, true);
                    Directory.CreateDirectory(meGroupPath);
                    File.SetAttributes(meGroupPath, File.GetAttributes(DefaultFolderPath));

                    foreach (var srcPath in Directory.GetFiles(DefaultFolderPath))
                    {
                        var dstPath = $@"{meGroupPath}\{Path.GetFileName(srcPath)}";
                        File.Copy(srcPath, dstPath);
                    }
                }

                RestoreDefaultFolder(true);
                if (WinOsVersion.Current >= WinOsVersion.Win11)
                {
                    // Win11需要将默认WinX菜单也恢复，同时删除备份WinX菜单
                    RestoreDefaultFolder(false);
                    DeletePath([BackupGroupPath]);
                }

                RefreshList();
                ExplorerRestarter.Show();
            }
        }

        public void DeleteMe()
        {
            var flag = Directory.GetFiles(GroupPath, "*.lnk").Length > 0;
            if (flag && AppMessageBox.Show(AppString.Message.DeleteGroup, null, MessageBoxButton.OKCancel) != MessageBoxResult.OK) return;
            DeletePath([GroupPath, BackupGroupPath, DefaultGroupPath]);
            if (flag)
            {
                RefreshList();
                ExplorerRestarter.Show();
            }
        }

        private void DeletePath(string[] paths)
        {
            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    File.SetAttributes(path, FileAttributes.Normal);
                    Directory.Delete(path, true);
                }
            }
        }
    }
}
