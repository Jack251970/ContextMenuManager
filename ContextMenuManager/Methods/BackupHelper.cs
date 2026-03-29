using ContextMenuManager.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using static ContextMenuManager.Methods.BackupList;

namespace ContextMenuManager.Methods
{
    internal sealed partial class BackupHelper
    {
        /*******************************外部变量、函数************************************/

        // 目前备份版本号
        public const int BackupVersion = 4;

        // 弃用备份版本号
        public const int DeprecatedBackupVersion = 1;

        // 右键菜单备份场景，包含全部场景（确保顺序与右键菜单场景Scenes相同）（新增备份类别处2）
        public static string[] BackupScenesText =
        [
            // 主页——第一板块
            AppString.SideBar.File, AppString.SideBar.Folder, AppString.SideBar.Directory, AppString.SideBar.Background,
            AppString.SideBar.Desktop, AppString.SideBar.Drive, AppString.SideBar.AllObjects, AppString.SideBar.Computer,
            AppString.SideBar.RecycleBin, AppString.SideBar.Library,
            // 主页——第二板块
            AppString.SideBar.New, AppString.SideBar.SendTo, AppString.SideBar.OpenWith,
            // 主页——第三板块
            AppString.SideBar.WinX,
            // 文件类型——第一板块
            AppString.SideBar.LnkFile, AppString.SideBar.UwpLnk, AppString.SideBar.ExeFile, AppString.SideBar.UnknownType,
            // 文件类型——第二板块
            AppString.SideBar.CustomExtension, AppString.SideBar.PerceivedType, AppString.SideBar.DirectoryType,
            // 其他规则——第一板块
            AppString.SideBar.EnhanceMenu, AppString.SideBar.DetailedEdit,
            // 其他规则——第二板块
            AppString.SideBar.DragDrop, AppString.SideBar.PublicReferences, AppString.SideBar.IEMenu,
        ];

        // 右键菜单备份场景，包含主页、文件类型、其他规则三个板块
        public static string[] HomeBackupScenesText =
        [
            // 主页——第一板块
            AppString.SideBar.File, AppString.SideBar.Folder, AppString.SideBar.Directory, AppString.SideBar.Background,
            AppString.SideBar.Desktop, AppString.SideBar.Drive, AppString.SideBar.AllObjects, AppString.SideBar.Computer,
            AppString.SideBar.RecycleBin, AppString.SideBar.Library,
            // 主页——第二板块
            AppString.SideBar.New, AppString.SideBar.SendTo, AppString.SideBar.OpenWith,
            // 主页——第三板块
            AppString.SideBar.WinX,
        ];
        public static string[] TypeBackupScenesText =
        [
            // 文件类型——第一板块
            AppString.SideBar.LnkFile, AppString.SideBar.UwpLnk, AppString.SideBar.ExeFile, AppString.SideBar.UnknownType,
            // 文件类型——第二板块
            AppString.SideBar.CustomExtension, AppString.SideBar.PerceivedType, AppString.SideBar.DirectoryType,
        ];
        public static string[] RuleBackupScenesText =
        [
            // 其他规则——第一板块
            AppString.SideBar.EnhanceMenu, AppString.SideBar.DetailedEdit,
            // 其他规则——第二板块
            AppString.SideBar.DragDrop, AppString.SideBar.PublicReferences, AppString.SideBar.IEMenu,
        ];

        public int backupCount = 0;     // 备份项目总数量
        public List<RestoreChangedItem> restoreList = [];    // 恢复改变项目
        public string createTime;       // 本次备份文件创建时间
        public string filePath;         // 本次备份文件目录

        public BackupHelper()
        {
            CheckDeprecatedBackup();
        }

        // 获取备份恢复场景文字
        public string[] GetBackupRestoreScenesText(List<Scenes> scenes)
        {
            List<string> scenesTextList = [];
            foreach (var scene in scenes)
            {
                scenesTextList.Add(BackupScenesText[(int)scene]);
            }
            return scenesTextList.ToArray();
        }

        // 备份指定场景内容
        public void BackupItems(List<string> sceneTexts, BackupMode backupMode, LoadingDialogInterface dialogInterface)
        {
            ClearBackupList();
            var count = GetBackupRestoreScenes(sceneTexts);
            dialogInterface.SetMaximum(count + 1);
            dialogInterface.SetProgress(0);
            backup = true;
            this.backupMode = backupMode;
            var dateTime = DateTime.Now;
            var date = DateTime.Today.ToString("yyyy-MM-dd");
            var time = dateTime.ToString("HH-mm-ss");
            createTime = $@"{date} {time}";
            filePath = $@"{AppConfig.MenuBackupDir}\{createTime}.xml";
            // 构建备份元数据
            metaData.CreateTime = dateTime;
            metaData.Device = AppConfig.ComputerHostName;
            metaData.BackupScenes = currentScenes;
            metaData.Version = BackupVersion;
            // 加载备份文件到缓冲区
            BackupRestoreItems(dialogInterface);
            // 保存缓冲区的备份文件
            if (dialogInterface.IsCancelled) return;
            SaveBackupList(filePath);
            backupCount = GetBackupListCount();
            ClearBackupList();
            dialogInterface.SetProgress(count + 1);
            if (dialogInterface.IsCancelled) File.Delete(filePath);
        }

        // 恢复指定场景内容
        public void RestoreItems(string filePath, List<string> sceneTexts, RestoreMode restoreMode, LoadingDialogInterface dialogInterface)
        {
            ClearBackupList();
            var count = GetBackupRestoreScenes(sceneTexts);
            dialogInterface.SetMaximum(count + 1);
            dialogInterface.SetProgress(0);
            backup = false;
            this.restoreMode = restoreMode;
            restoreList.Clear();
            // 加载备份文件到缓冲区
            if (dialogInterface.IsCancelled) return;
            LoadBackupList(filePath);
            // 还原缓冲区的备份文件
            BackupRestoreItems(dialogInterface);
            ClearBackupList();
            dialogInterface.SetProgress(count + 1);
        }

        /*******************************内部变量、函数************************************/

        // 目前备份恢复场景
        private readonly List<Scenes> currentScenes = new();

        private bool backup;                // 目前备份还是恢复
        private Scenes currentScene;        // 目前处理场景
        private BackupMode backupMode;      // 目前备份模式
        private RestoreMode restoreMode;    // 目前恢复模式

        /*******************************Misc.cs************************************/

        private void GetShellNewListBackupItems()
        {
            // TODO: 实现 ShellNewList 备份逻辑
        }

        private void GetSendToListItems()
        {
            // TODO: 实现 SendToList 备份逻辑
        }

        private void GetOpenWithListItems()
        {
            // TODO: 实现 OpenWithList 备份逻辑
        }

        private void GetWinXListItems()
        {
            // TODO: 实现 WinXList 备份逻辑
        }

        private void GetIEItems()
        {
            // TODO: 实现 IEList 备份逻辑
        }

        /*******************************Rules.cs************************************/

        private void GetDetailedEditListItems()
        {
            // TODO: 实现 DetailedEditList 备份逻辑
        }

        private void GetEnhanceMenuListItems()
        {
            // TODO: 实现 EnhanceMenuList 备份逻辑
        }

        // 以下方法定义在部分类文件中:
        // - BackupHelper.Scenes.cs: GetRegistryKeySafe, CheckDeprecatedBackup, GetBackupRestoreScenes, BackupRestoreItems, GetSceneName, GetBackupItems
        // - BackupHelper.Items.cs: BackupRestoreItem (3个重载), CheckItemNeedChange (3个重载), BackupRestoreSelectItem
        // - BackupHelper.Shell.cs: GetShellListItems, GetBackupItems, GetBackupShellItems, GetBackupShellExItems, GetBackupStoreItems, GetBackupUwpModeItem, GetDragDropGroupItem
    }
}
