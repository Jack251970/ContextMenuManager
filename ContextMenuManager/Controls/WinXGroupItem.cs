using BluePointLilac.Methods;
using ContextMenuManager.Controls.Interfaces;
using ContextMenuManager.Methods;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    sealed class WinXGroupItem : FoldGroupItem, IChkVisibleItem, ITsiDeleteItem, ITsiTextItem
    {
        public WinXGroupItem(string groupPath) : base(groupPath, ObjectPath.PathType.Directory)
        {
            InitializeComponents();
            RefreshKeyPath();
        }

        private string keyPath = null;
        private void RefreshKeyPath()
        {
            if (WinOsVersion.Current >= WinOsVersion.Win11)
            {
                keyPath = GroupPath.Substring(WinXList.WinXPath.Length);
            }
        }

        private string BackupGroupPath => $@"{WinXList.BackupWinXPath}{keyPath}";
        private string DefaultGroupPath => $@"{WinXList.DefaultWinXPath}{keyPath}";

        private string DefaultFolderPath => $@"{((WinOsVersion.Current >= WinOsVersion.Win11) ? WinXList.WinXDefaultPath : WinXList.DefaultWinXPath)}\{ItemText}";

        public bool ignoreChange = false;

        public bool ItemVisible
        {
            get
            {
                return (WinOsVersion.Current >= WinOsVersion.Win11) ?
                    Directory.Exists(GroupPath) && Directory.GetFiles(GroupPath, "*.lnk").Length != 0 :
                    (File.GetAttributes(GroupPath) & FileAttributes.Hidden) != FileAttributes.Hidden;
            }
            set
            {
                if (WinOsVersion.Current >= WinOsVersion.Win11)
                {
                    if (ignoreChange)
                    {
                        // 在WinXItem的启用禁用导致的WinXGroupItem的启用禁用改变，不触发ItemVisible的set方法
                        ignoreChange = false; return;
                    }

                    bool flag = false;
                    foreach (WinXItem item in winXItems)
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
                        DeletePath(new string[] { BackupGroupPath });
                    }
                    if (flag) ExplorerRestarter.Show();
                }
                else
                {
                    FileAttributes attributes = File.GetAttributes(GroupPath);
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
                void MoveDirectory(string oldPath, string newPath)
                {
                    if (Directory.Exists(oldPath))
                    {
                        if (Directory.Exists(newPath)) Directory.Delete(newPath, true);
                        Directory.Move(oldPath, newPath);
                    }
                }

                string newKeyPath = $@"\{ObjectPath.RemoveIllegalChars(value)}";
                string newGroupPath = $@"{WinXList.WinXPath}{newKeyPath}";
                MoveDirectory(GroupPath, newGroupPath);

                if (WinOsVersion.Current >= WinOsVersion.Win11)
                {
                    string newBackupGroupPath = $@"{WinXList.BackupWinXPath}{newKeyPath}";
                    MoveDirectory(BackupGroupPath, newBackupGroupPath);

                    string newDefaultGroupPath = $@"{WinXList.DefaultWinXPath}{newKeyPath}";
                    MoveDirectory(DefaultGroupPath, newDefaultGroupPath);

                    keyPath = newKeyPath;
                }

                GroupPath = newGroupPath;

                RefreshList();
                ExplorerRestarter.Show();
            }
        }

        private readonly List<WinXItem> winXItems = new List<WinXItem> { };
        
        public void AddWinXItem(WinXItem item)
        {
            winXItems.Add(item);
        }
        public void RemoveWinXItem(WinXItem item)
        {
            if (winXItems.Contains(item))
            {
                winXItems.Remove(item);
            }
        }

        public VisibleCheckBox ChkVisible { get; set; }
        public DeleteMeMenuItem TsiDeleteMe { get; set; }
        public ChangeTextMenuItem TsiChangeText { get; set; }
        readonly RToolStripMenuItem TsiRestoreDefault = new RToolStripMenuItem(AppString.Menu.RestoreDefault);

        public bool ChkChecked
        {
            get => ItemVisible;
            set => ChkVisible.Checked = value;
        }

        private void InitializeComponents()
        {
            ChkVisible = new VisibleCheckBox(this);
            SetCtrIndex(ChkVisible, 1);
            TsiDeleteMe = new DeleteMeMenuItem(this);
            TsiChangeText = new ChangeTextMenuItem(this);
            ContextMenuStrip.Items.AddRange(new ToolStripItem[] { new RToolStripSeparator(),
                TsiChangeText, TsiRestoreDefault, new RToolStripSeparator(), TsiDeleteMe });
            ContextMenuStrip.Opening += (sender, e) => TsiRestoreDefault.Enabled = Directory.Exists(DefaultFolderPath);
            TsiRestoreDefault.Click += (sender, e) => RestoreDefault();
        }

        private void RefreshList()
        {
            WinXList list = (WinXList)Parent;
            list.ClearItems();
            list.LoadItems();
        }

        private void RestoreDefault()
        {
            if (AppMessageBox.Show(AppString.Message.RestoreDefault, MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                void RestoreDefaultFolder(bool isWinX)
                {
                    string meGroupPath = isWinX ? GroupPath : DefaultGroupPath;

                    File.SetAttributes(meGroupPath, FileAttributes.Normal);
                    Directory.Delete(meGroupPath, true);
                    Directory.CreateDirectory(meGroupPath);
                    File.SetAttributes(meGroupPath, File.GetAttributes(DefaultFolderPath));

                    foreach (string srcPath in Directory.GetFiles(DefaultFolderPath))
                    {
                        string dstPath = $@"{meGroupPath}\{Path.GetFileName(srcPath)}";
                        File.Copy(srcPath, dstPath);
                    }
                }

                RestoreDefaultFolder(true);
                if (WinOsVersion.Current >= WinOsVersion.Win11)
                {
                    // Win11需要将默认WinX菜单也恢复，同时删除备份WinX菜单
                    RestoreDefaultFolder(false);
                    DeletePath(new string[] { BackupGroupPath });
                }

                RefreshList();
                ExplorerRestarter.Show();
            }
        }

        public void DeleteMe()
        {
            bool flag = Directory.GetFiles(GroupPath, "*.lnk").Length > 0;
            if(flag && AppMessageBox.Show(AppString.Message.DeleteGroup, MessageBoxButtons.OKCancel) != DialogResult.OK) return;
            DeletePath(new string[] { GroupPath, BackupGroupPath, DefaultGroupPath });
            if (flag)
            {
                RefreshList();
                ExplorerRestarter.Show();
            }
        }

        private void DeletePath(string[] paths)
        {
            foreach (string path in paths)
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