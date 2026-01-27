using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Controls;
using ContextMenuManager.Controls.Interfaces;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using static ContextMenuManager.Controls.ShellList;
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
        public static string[] BackupScenesText = new string[] {
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
        };

        // 右键菜单备份场景，包含主页、文件类型、其他规则三个板块
        public static string[] HomeBackupScenesText = new string[] {
                // 主页——第一板块
                AppString.SideBar.File, AppString.SideBar.Folder, AppString.SideBar.Directory, AppString.SideBar.Background,
                AppString.SideBar.Desktop, AppString.SideBar.Drive, AppString.SideBar.AllObjects, AppString.SideBar.Computer,
                AppString.SideBar.RecycleBin, AppString.SideBar.Library,
                // 主页——第二板块
                AppString.SideBar.New, AppString.SideBar.SendTo, AppString.SideBar.OpenWith,
                // 主页——第三板块
                AppString.SideBar.WinX,
            };
        public static string[] TypeBackupScenesText = new string[] {
                // 文件类型——第一板块
                AppString.SideBar.LnkFile, AppString.SideBar.UwpLnk, AppString.SideBar.ExeFile, AppString.SideBar.UnknownType,
                // 文件类型——第二板块
                AppString.SideBar.CustomExtension, AppString.SideBar.PerceivedType, AppString.SideBar.DirectoryType,
            };
        public static string[] RuleBackupScenesText = new string[] {
                // 其他规则——第一板块
                AppString.SideBar.EnhanceMenu, AppString.SideBar.DetailedEdit,
                // 其他规则——第二板块
                AppString.SideBar.DragDrop, AppString.SideBar.PublicReferences, AppString.SideBar.IEMenu,
            };

        public int backupCount = 0;     // 备份项目总数量
        public List<RestoreChangedItem> restoreList = new();    // 恢复改变项目
        public string createTime;       // 本次备份文件创建时间
        public string filePath;         // 本次备份文件目录

        public BackupHelper()
        {
            CheckDeprecatedBackup();
        }

        // 获取备份恢复场景文字
        public string[] GetBackupRestoreScenesText(List<Scenes> scenes)
        {
            var scenesTextList = new List<string>();
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
            SaveBackupList(filePath);
            backupCount = GetBackupListCount();
            ClearBackupList();
            dialogInterface.SetProgress(count + 1);
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
        private string currentExtension;    // 二级菜单项目
        private BackupMode backupMode;      // 目前备份模式
        private RestoreMode restoreMode;    // 目前恢复模式

        private RegistryKey GetRegistryKeySafe(string regPath)
        {
            if (backup)
            {
                try
                {
                    RegistryEx.GetRootAndSubRegPath(regPath, out var root, out var keyPath);
                    return root.OpenSubKey(keyPath, false);
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                return RegistryEx.GetRegistryKey(regPath);
            }
        }

        // 删除弃用版本的备份
        private void CheckDeprecatedBackup()
        {
            var rootPath = AppConfig.MenuBackupRootDir;
            var deviceDirs = Directory.GetDirectories(rootPath);
            foreach (var deviceDir in deviceDirs)
            {
                var xmlFiles = Directory.GetFiles(deviceDir, "*.xml");
                foreach (var xmlFile in xmlFiles)
                {
                    // 加载项目元数据
                    LoadBackupDataMetaData(xmlFile);
                    // 如果备份版本号小于等于最高弃用备份版本号，则删除该备份
                    try
                    {
                        if (metaData.Version <= DeprecatedBackupVersion)
                        {
                            File.Delete(xmlFile);
                        }
                    }
                    catch
                    {
                        File.Delete(xmlFile);
                    }
                }
                // 如果设备目录为空且不为本机目录，则删除该设备目录
                var device = Path.GetFileName(deviceDir);
                if ((Directory.GetFiles(deviceDir).Length == 0) && (device != AppConfig.ComputerHostName))
                {
                    Directory.Delete(deviceDir);
                }
            }
        }

        // 获取目前备份恢复场景
        private int GetBackupRestoreScenes(List<string> sceneTexts)
        {
            currentScenes.Clear();
            for (var i = 0; i < BackupScenesText.Length; i++)
            {
                var text = BackupScenesText[i];
                if (sceneTexts.Contains(text))
                {
                    // 顺序对应，直接转换
                    currentScenes.Add((Scenes)i);
                }
            }
            return currentScenes.Count;
        }

        // 按照目前处理场景逐个备份或恢复
        private void BackupRestoreItems(LoadingDialogInterface dialogInterface)
        {
            foreach (var scene in currentScenes)
            {
                currentScene = scene;
                // 加载某个Scene的恢复列表
                if (!backup)
                {
                    LoadTempRestoreList(currentScene);
                }
                GetBackupItems();
                dialogInterface?.SetProgress(currentScenes.IndexOf(scene) + 1);
            }
        }

        // 开始进行备份或恢复
        // （新增备份类别处5）
        private void GetBackupItems()
        {
            switch (currentScene)
            {
                case Scenes.New:    // 新建
                    GetShellNewListBackupItems(); break;
                case Scenes.SendTo: // 发送到
                    GetSendToListItems(); break;
                case Scenes.OpenWith:   // 打开方式
                    GetOpenWithListItems(); break;
                case Scenes.WinX:   // Win+X
                    GetWinXListItems(); break;
                case Scenes.InternetExplorer:   // IE浏览器
                    GetIEItems(); break;
                case Scenes.EnhanceMenu:   // 增强菜单
                    GetEnhanceMenuListItems(); break;
                case Scenes.DetailedEdit:   // 详细编辑
                    GetDetailedEditListItems(); break;
                default:    // 位于ShellList.cs内的备份项目
                    GetShellListItems(); break;
            }
        }

        /*******************************单个Item处理************************************/

        private void BackupRestoreItem(MyListItem item, string itemName, string keyName, BackupItemType backupItemType, bool itemData, Scenes currentScene)
        {
            if (backup)
            {
                // 加入备份列表
                switch (backupMode)
                {
                    case BackupMode.All:
                    default:
                        AddItem(keyName, backupItemType, itemData, currentScene);
                        break;
                    case BackupMode.OnlyVisible:
                        if (itemData) AddItem(keyName, backupItemType, itemData, currentScene);
                        break;
                    case BackupMode.OnlyInvisible:
                        if (!itemData) AddItem(keyName, backupItemType, itemData, currentScene);
                        break;
                }
            }
            else
            {
                // 恢复备份列表（新增备份类别处4）
                if (CheckItemNeedChange(itemName, keyName, backupItemType, itemData))
                {
                    if (item is IChkVisibleItem visibleItem)
                    {
                        visibleItem.ItemVisible = !itemData;
                    }
                }
            }
            // 释放资源
            item.Dispose();
        }

        private bool CheckItemNeedChange(string itemName, string keyName, BackupItemType itemType, bool currentItemData)
        {
            var item = GetItem(currentScene, keyName, itemType);
            if (item != null)
            {
                var itemData = false;
                try
                {
                    itemData = Convert.ToBoolean(item.ItemData);
                }
                catch
                {
                    return false;
                }
                if (itemData != currentItemData)
                {
                    restoreList.Add(new RestoreChangedItem(currentScene, itemName, itemData.ToString()));
                    return true;
                }
                else
                {
                    return false;
                }
            }
            if ((restoreMode == RestoreMode.DisableNotOnList && currentItemData) ||
                (restoreMode == RestoreMode.EnableNotOnList && !currentItemData))
            {
                restoreList.Add(new RestoreChangedItem(currentScene, itemName, (!currentItemData).ToString()));
                return true;
            }
            return false;
        }

        private void BackupRestoreItem(MyListItem item, string itemName, string keyName, BackupItemType backupItemType, int itemData, Scenes currentScene)
        {
            if (backup)
            {
                // 加入备份列表
                AddItem(keyName, backupItemType, itemData, currentScene);
            }
            else
            {
                // 恢复备份列表（新增备份类别处4）
                if (CheckItemNeedChange(itemName, keyName, backupItemType, itemData, out var restoreItemData))
                {
                    switch (backupItemType)
                    {
                        case BackupItemType.NumberIniRuleItem:
                            ((NumberIniRuleItem)item).ItemValue = restoreItemData; break;
                        case BackupItemType.NumberRegRuleItem:
                            ((NumberRegRuleItem)item).ItemValue = restoreItemData; break;
                    }
                }
            }
            // 释放资源
            item.Dispose();
        }

        private bool CheckItemNeedChange(string itemName, string keyName, BackupItemType itemType, int currentItemData, out int restoreItemData)
        {
            var item = GetItem(currentScene, keyName, itemType);
            if (item != null)
            {
                int itemData;
                try
                {
                    itemData = Convert.ToInt32(item.ItemData);
                }
                catch
                {
                    restoreItemData = 0;
                    return false;
                }
                if (itemData != currentItemData)
                {
                    restoreList.Add(new RestoreChangedItem(currentScene, itemName, itemData.ToString()));
                    restoreItemData = itemData;
                    return true;
                }
                else
                {
                    restoreItemData = 0;
                    return false;
                }
            }
            restoreItemData = 0;
            return false;
        }

        private void BackupRestoreItem(MyListItem item, string itemName, string keyName, BackupItemType backupItemType, string itemData, Scenes currentScene)
        {
            if (backup)
            {
                // 加入备份列表
                AddItem(keyName, backupItemType, itemData, currentScene);
            }
            else
            {
                // 恢复备份列表（新增备份类别处4）
                if (CheckItemNeedChange(itemName, keyName, backupItemType, itemData, out var restoreItemData))
                {
                    switch (backupItemType)
                    {
                        case BackupItemType.StringIniRuleItem:
                            ((StringIniRuleItem)item).ItemValue = restoreItemData; break;
                        case BackupItemType.StringRegRuleItem:
                            ((StringRegRuleItem)item).ItemValue = restoreItemData; break;
                    }
                }
            }
            // 释放资源
            item.Dispose();
        }

        private bool CheckItemNeedChange(string itemName, string keyName, BackupItemType itemType, string currentItemData, out string restoreItemData)
        {
            var item = GetItem(currentScene, keyName, itemType);
            if (item != null)
            {
                var itemData = item.ItemData;
                if (itemData != currentItemData)
                {
                    restoreList.Add(new RestoreChangedItem(currentScene, itemName, itemData.ToString()));
                    restoreItemData = itemData;
                    return true;
                }
                else
                {
                    restoreItemData = "";
                    return false;
                }
            }
            restoreItemData = "";
            return false;
        }

        // SelectItem有单独的备份恢复机制
        private void BackupRestoreSelectItem(SelectItem item, string itemData, Scenes currentScene)
        {
            var keyName = "";
            if (backup)
            {
                AddItem(keyName, BackupItemType.SelectItem, itemData, currentScene);
            }
            else
            {
                foreach (var restoreItem in sceneRestoreList)
                {
                    // 成功匹配到后的处理方式：只需检查ItemData和ItemType
                    if (restoreItem.ItemType == BackupItemType.SelectItem)
                    {
                        var restoreItemData = restoreItem.ItemData;
                        if (restoreItemData != itemData)
                        {
                            int.TryParse(restoreItem.KeyName, out var itemDataIndex);
                            switch (currentScene)
                            {
                                case Scenes.DragDrop:
                                    var dropEffect = (DropEffect)itemDataIndex;
                                    if (DefaultDropEffect != dropEffect)
                                    {
                                        DefaultDropEffect = dropEffect;
                                    }
                                    break;
                            }
                            var itemName = keyName;
                            restoreList.Add(new RestoreChangedItem(currentScene, itemName, restoreItemData.ToString()));
                        }
                    }
                }
            }
            item.Dispose();
            return;
        }
    }
}
