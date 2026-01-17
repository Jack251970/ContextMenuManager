using BluePointLilac.Controls;
using ContextMenuManager.Methods;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    internal sealed class BackupListBox : MyList, ITsiRestoreFile
    {
        private readonly BackupHelper helper = new();

        public void LoadItems()
        {
            // 获取备份根目录
            var rootPath = AppConfig.MenuBackupRootDir;
            // 获取rootPath下的所有子目录
            var deviceDirs = Directory.GetDirectories(rootPath);
            // 仅获取deviceDir下的.xml备份文件
            foreach (var deviceDir in deviceDirs)
            {
                // 获取当前设备目录下的所有XML文件
                var xmlFiles = Directory.GetFiles(deviceDir, "*.xml");
                // 遍历所有XML文件
                foreach (var xmlFile in xmlFiles)
                {
                    // 加载项目元数据
                    BackupList.LoadBackupDataMetaData(xmlFile);
                    // 新增备份项目
                    var deviceName = BackupList.metaData?.Device;
                    var createTime = BackupList.metaData?.CreateTime.ToString("G");
                    AddItem(new RestoreItem(this, xmlFile, deviceName ?? AppString.Other.Unknown,
                        createTime ?? AppString.Other.Unknown));
                }
            }
            SortItemByText();
            AddNewBackupItem();
        }

        private void AddNewBackupItem()
        {
            var newItem = new NewItem(AppString.Dialog.NewBackupItem);
            InsertItem(newItem, 0);
            newItem.AddNewItem += BackupItems;
        }

        private void BackupItems()
        {
            // 获取备份选项
            BackupMode backupMode;
            List<string> backupScenes;
            // 构建备份对话框
            using (var dlg = new BackupDialog())
            {
                dlg.Title = AppString.Dialog.NewBackupItem;
                dlg.TvTitle = AppString.Dialog.BackupContent;
                dlg.TvItems = BackupHelper.BackupScenesText;
                dlg.CmbTitle = AppString.Dialog.BackupMode;
                dlg.CmbItems = new[] { AppString.Dialog.BackupMode1, AppString.Dialog.BackupMode2,
                    AppString.Dialog.BackupMode3 };
                if (dlg.ShowDialog() != DialogResult.OK) return;
                backupMode = dlg.CmbSelectedIndex switch
                {
                    1 => BackupMode.OnlyVisible,
                    2 => BackupMode.OnlyInvisible,
                    _ => BackupMode.All,
                };
                backupScenes = dlg.TvSelectedItems;
            }
            // 未选择备份项目，不进行备份
            if (backupScenes.Count == 0)
            {
                AppMessageBox.Show(AppString.Message.NotChooseAnyBackup);
                return;
            }
            // 开始备份项目
            Cursor = Cursors.WaitCursor;
            _ = LoadingDialog.ShowDialog(FindForm(), AppString.SideBar.BackupRestore,
                dialogInterface =>
                {
                    helper.BackupItems(backupScenes, backupMode, dialogInterface);
                });
            Cursor = Cursors.Default;
            // 新增备份项目（项目已加载元数据）
            var deviceName = BackupList.metaData.Device;
            var createTime = BackupList.metaData.CreateTime.ToString("G");
            AddItem(new RestoreItem(this, helper.filePath, deviceName, createTime));
            // 弹窗提示结果
            var backupCount = helper.backupCount;
            AppMessageBox.Show(AppString.Message.BackupSucceeded.Replace("%s", backupCount.ToString()));
        }

        public void RestoreItems(string filePath)
        {
            // 获取恢复选项
            RestoreMode restoreMode;
            List<string> restoreScenes;
            BackupList.LoadBackupDataMetaData(filePath);
            // 备份版本提示
            if (BackupList.metaData.Version <= BackupHelper.DeprecatedBackupVersion)
            {
                AppMessageBox.Show(AppString.Message.DeprecatedBackupVersion);
                return;
            }
            else if (BackupList.metaData.Version < BackupHelper.BackupVersion)
            {
                AppMessageBox.Show(AppString.Message.OldBackupVersion);
            }
            // 构建恢复对话框
            using (var dlg = new BackupDialog())
            {
                dlg.Title = AppString.Dialog.RestoreBackupItem;
                dlg.TvTitle = AppString.Dialog.RestoreContent;
                dlg.TvItems = helper.GetBackupRestoreScenesText(BackupList.metaData.BackupScenes);
                dlg.CmbTitle = AppString.Dialog.RestoreMode;
                dlg.CmbItems = new[] { AppString.Dialog.RestoreMode1, AppString.Dialog.RestoreMode2, AppString.Dialog.RestoreMode3 };
                if (dlg.ShowDialog() != DialogResult.OK) return;
                restoreMode = dlg.CmbSelectedIndex switch
                {
                    1 => RestoreMode.DisableNotOnList,
                    2 => RestoreMode.EnableNotOnList,
                    _ => RestoreMode.NotHandleNotOnList,
                };
                restoreScenes = dlg.TvSelectedItems;
            }
            // 未选择恢复项目，不进行恢复
            if (restoreScenes.Count == 0)
            {
                AppMessageBox.Show(AppString.Message.NotChooseAnyRestore);
                return;
            }
            // 开始恢复项目
            Cursor = Cursors.WaitCursor;
            _ = LoadingDialog.ShowDialog(FindForm(), AppString.SideBar.BackupRestore,
                dialogInterface =>
                {
                    helper.RestoreItems(filePath, restoreScenes, restoreMode, dialogInterface);
                });
            // 弹窗提示结果
            var restoreList = helper.restoreList;
            ShowRestoreDialog(restoreList);
            Cursor = Cursors.Default;
        }

        private void ShowRestoreDialog(List<RestoreChangedItem> restoreList)
        {
            if (restoreList == null || restoreList.Count == 0)
            {
                AppMessageBox.Show(AppString.Message.NoNeedRestore);
                return;
            }
            using var dlg = new RestoreListDialog();
            dlg.RestoreData = restoreList;
            dlg.ShowDialog();
        }
    }
}