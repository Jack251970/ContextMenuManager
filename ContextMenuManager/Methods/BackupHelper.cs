using BluePointLilac.Methods;
using ContextMenuManager.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static ContextMenuManager.Controls.ShellList;
using System.Xml;
using System.Drawing;
using static ContextMenuManager.Methods.BackupList;
using System.Xml.Serialization;
using static ContextMenuManager.Controls.ShellNewList;
using BluePointLilac.Controls;

namespace ContextMenuManager.Methods
{
    /*******************************外部枚举变量************************************/

    // 右键菜单场景（新增备份类别处1）
    public enum Scenes
    {
        // 主页——第一板块
        File, Folder, Directory, Background, Desktop, Drive, AllObjects, Computer, RecycleBin, Library,
        // 主页——第二板块
        New, SendTo, OpenWith,
        // 主页——第三板块
        WinX,
        // 文件类型——第一板块
        LnkFile, UwpLnk, ExeFile, UnknownType,
        // 文件类型——第二板块（不予备份）
        // 其他规则——第一板块
        EnhanceMenu, DetailedEdit,
        // 其他规则——第二板块
        DragDrop, PublicReferences, InternetExplorer,
        // 其他规则——第三板块（不予备份）
        // 不予备份的场景
        CustomExtension, PerceivedType, DirectoryType, MenuAnalysis, CustomRegPath, CustomExtensionPerceivedType,
    };

    // 备份项目类型（新增备份类别处3）
    public enum BackupItemType
    {
        ShellItem, ShellExItem, UwpModelItem, VisibleRegRuleItem, ShellNewItem, SendToItem,
        OpenWithItem, WinXItem, SelectItem, StoreShellItem, IEItem, EnhanceShellItem, EnhanceShellExItem,
        NumberIniRuleItem, StringIniRuleItem, VisbleIniRuleItem, NumberRegRuleItem, StringRegRuleItem,
    }

    // 备份选项
    public enum BackupMode
    {
        All,            // 备份全部菜单项目
        OnlyVisible,    // 仅备份启用的菜单项目
        OnlyInvisible   // 仅备份禁用的菜单项目
    };

    // 恢复模式
    public enum RestoreMode
    {
        NotHandleNotOnList,     // 启用备份列表上可见的菜单项，禁用备份列表上不可见的菜单项，不处理不位于备份列表上的菜单项
        DisableNotOnList,       // 启用备份列表上可见的菜单项，禁用备份列表上不可见以及不位于备份列表上的菜单项
        EnableNotOnList,        // 启用备份列表上可见的菜单项以及不位于备份列表上的菜单项，禁用备份列表上不可见
    };

    sealed class BackupHelper
    {
        /*******************************外部变量、函数************************************/

        // 目前备份版本号
        public const int BackupVersion = 3;

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
            // 文件类型——第二板块（不予备份）
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
                // 文件类型——第二板块（不予备份）
            };
        public static string[] RuleBackupScenesText = new string[] {
                // 其他规则——第一板块
                AppString.SideBar.EnhanceMenu, AppString.SideBar.DetailedEdit,
                // 其他规则——第二板块
                AppString.SideBar.DragDrop, AppString.SideBar.PublicReferences, AppString.SideBar.IEMenu,
            };

        public int backupCount = 0;     // 备份项目总数量
        public List<RestoreChangedItem> restoreList = new List<RestoreChangedItem>();    // 恢复改变项目
        public string createTime;       // 本次备份文件创建时间
        public string filePath;         // 本次备份文件目录

        public BackupHelper()
        {
            CheckDeprecatedBackup();
        }

        // 获取备份恢复场景文字
        public string[] GetBackupRestoreScenesText(List<Scenes> scenes)
        {
            List<string> scenesTextList = new List<string>();
            foreach(Scenes scene in scenes)
            {
                scenesTextList.Add(BackupScenesText[(int)scene]);
            }
            return scenesTextList.ToArray();
        }

        // 备份指定场景内容
        public void BackupItems(List<string> sceneTexts, BackupMode backupMode)
        {
            ClearBackupList();
            GetBackupRestoreScenes(sceneTexts);
            backup = true;
            this.backupMode = backupMode;
            DateTime dateTime = DateTime.Now;
            string date = DateTime.Today.ToString("yyyy-MM-dd");
            string time = dateTime.ToString("HH-mm-ss");
            createTime = $@"{date} {time}";
            filePath = $@"{AppConfig.MenuBackupDir}\{createTime}.xml";
            // 构建备份元数据
            metaData.CreateTime = dateTime;
            metaData.Device = AppConfig.ComputerHostName;
            metaData.BackupScenes = currentScenes;
            metaData.Version = BackupVersion;
            // 加载备份文件到缓冲区
            BackupRestoreItems();
            // 保存缓冲区的备份文件
            SaveBackupList(filePath);
            backupCount = GetBackupListCount();
            ClearBackupList();
        }

        // 恢复指定场景内容
        public void RestoreItems(string filePath, List<string> sceneTexts, RestoreMode restoreMode)
        {
            ClearBackupList();
            GetBackupRestoreScenes(sceneTexts);
            backup = false;
            this.restoreMode = restoreMode;
            restoreList.Clear();
            // 加载备份文件到缓冲区
            LoadBackupList(filePath);
            // 还原缓冲区的备份文件
            BackupRestoreItems();
            ClearBackupList();
        }

        /*******************************内部变量、函数************************************/

        // 目前备份恢复场景
        private readonly List<Scenes> currentScenes = new List<Scenes>();

        private bool backup;                // 目前备份还是恢复
        private Scenes currentScene;        // 目前处理场景
        private BackupMode backupMode;      // 目前备份模式
        private RestoreMode restoreMode;    // 目前恢复模式

        // 删除弃用版本的备份
        private void CheckDeprecatedBackup()
        {
            string rootPath = AppConfig.MenuBackupRootDir;
            string[] deviceDirs = Directory.GetDirectories(rootPath);
            foreach (string deviceDir in deviceDirs)
            {
                string[] xmlFiles = Directory.GetFiles(deviceDir, "*.xml");
                foreach (string xmlFile in xmlFiles)
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
                string device = Path.GetFileName(deviceDir);
                if ((Directory.GetFiles(deviceDir).Length == 0) && (device != AppConfig.ComputerHostName))
                {
                    Directory.Delete(deviceDir);
                }
            }
        }

        // 获取目前备份恢复场景
        private void GetBackupRestoreScenes(List<string> sceneTexts)
        {
            currentScenes.Clear();
            for (int i = 0; i < BackupScenesText.Length; i++)
            {
                string text = BackupScenesText[i];
                if (sceneTexts.Contains(text))
                {
                    // 顺序对应，直接转换
                    currentScenes.Add((Scenes)i);
                }
            }
        }

        // 按照目前处理场景逐个备份或恢复
        private void BackupRestoreItems()
        {
            foreach(Scenes scene in currentScenes)
            {
                currentScene = scene;
                // 加载某个Scene的恢复列表
                if (!backup)
                {
                    LoadTempRestoreList(currentScene);
                }
                GetBackupItems();
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

        private void BackupRestoreItem(MyListItem item, string keyName, BackupItemType backupItemType, bool itemData, Scenes currentScene)
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
                if (CheckItemNeedChange(keyName, backupItemType, itemData))
                {
                    switch (backupItemType)
                    {
                        case BackupItemType.ShellItem:
                            ((ShellItem)item).ItemVisible = !itemData; break;
                        case BackupItemType.ShellExItem:
                            ((ShellExItem)item).ItemVisible = !itemData; break;
                        case BackupItemType.UwpModelItem:
                            ((UwpModeItem)item).ItemVisible = !itemData; break;
                        case BackupItemType.VisibleRegRuleItem:
                            ((VisibleRegRuleItem)item).ItemVisible = !itemData; break;
                        case BackupItemType.ShellNewItem:
                            ((ShellNewItem)item).ItemVisible = !itemData; break;
                        case BackupItemType.SendToItem:
                            ((SendToItem)item).ItemVisible = !itemData; break;
                        case BackupItemType.OpenWithItem:
                            ((OpenWithItem)item).ItemVisible = !itemData; break;
                        case BackupItemType.WinXItem:
                            ((WinXItem)item).ItemVisible = !itemData; break;
                        case BackupItemType.StoreShellItem:
                            ((StoreShellItem)item).ItemVisible = !itemData; break;
                        case BackupItemType.IEItem:
                            ((IEItem)item).ItemVisible = !itemData; break;
                        case BackupItemType.VisbleIniRuleItem:
                            ((VisbleIniRuleItem)item).ItemVisible = !itemData; break;
                        case BackupItemType.EnhanceShellItem:
                            ((EnhanceShellItem)item).ItemVisible = !itemData; break;
                        case BackupItemType.EnhanceShellExItem:
                            ((EnhanceShellExItem)item).ItemVisible = !itemData; break;
                    }
                }
            }
            // 释放资源
            item.Dispose();
        }

        private bool CheckItemNeedChange(string keyName, BackupItemType itemType, bool currentItemData)
        {
            foreach (BackupItem item in sceneRestoreList)
            {
                // 成功匹配到后的处理方式：KeyName和ItemType匹配后检查ItemVisible
                if (item.KeyName == keyName && item.ItemType == itemType)
                {
                    bool itemData = false;
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
                        restoreList.Add(new RestoreChangedItem(currentScene, keyName, itemData.ToString()));
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            if ((restoreMode == RestoreMode.DisableNotOnList && currentItemData) || 
                (restoreMode == RestoreMode.EnableNotOnList && !currentItemData))
            {
                restoreList.Add(new RestoreChangedItem(currentScene, keyName, (!currentItemData).ToString()));
                return true;
            }
            return false;
        }

        private void BackupRestoreItem(MyListItem item, string keyName, BackupItemType backupItemType, int itemData, Scenes currentScene)
        {
            if (backup)
            {
                // 加入备份列表
                AddItem(keyName, backupItemType, itemData, currentScene);
            }
            else
            {
                // 恢复备份列表（新增备份类别处4）
                int restoreItemData;
                if (CheckItemNeedChange(keyName, backupItemType, itemData, out restoreItemData))
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

        private bool CheckItemNeedChange(string keyName, BackupItemType itemType, int currentItemData, out int restoreItemData)
        {
            foreach (BackupItem item in sceneRestoreList)
            {
                // 成功匹配到后的处理方式：KeyName和ItemType匹配后检查itemData
                if (item.KeyName == keyName && item.ItemType == itemType)
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
                        restoreList.Add(new RestoreChangedItem(currentScene, keyName, itemData.ToString()));
                        restoreItemData = itemData;
                        return true;
                    }
                    else
                    {
                        restoreItemData = 0;
                        return false;
                    }
                }
            }
            restoreItemData = 0;
            return false;
        }

        private void BackupRestoreItem(MyListItem item, string keyName, BackupItemType backupItemType, string itemData, Scenes currentScene)
        {
            if (backup)
            {
                // 加入备份列表
                AddItem(keyName, backupItemType, itemData, currentScene);
            }
            else
            {
                // 恢复备份列表（新增备份类别处4）
                string restoreItemData;
                if (CheckItemNeedChange(keyName, backupItemType, itemData, out restoreItemData))
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

        private bool CheckItemNeedChange(string keyName, BackupItemType itemType, string currentItemData, out string restoreItemData)
        {
            foreach (BackupItem item in sceneRestoreList)
            {
                // 成功匹配到后的处理方式：KeyName和ItemType匹配后检查itemData
                if (item.KeyName == keyName && item.ItemType == itemType)
                {
                    string itemData = item.ItemData;
                    if (itemData != currentItemData)
                    {
                        restoreList.Add(new RestoreChangedItem(currentScene, keyName, itemData.ToString()));
                        restoreItemData = itemData;
                        return true;
                    }
                    else
                    {
                        restoreItemData = "";
                        return false;
                    }
                }
            }
            restoreItemData = "";
            return false;
        }

        // SelectItem有单独的备份恢复机制
        private void BackupRestoreSelectItem(SelectItem item, string itemData, Scenes currentScene)
        {
            string keyName = "";
            if (backup)
            {
                AddItem(keyName, BackupItemType.SelectItem, itemData, currentScene);
            }
            else
            {
                foreach (BackupItem restoreItem in sceneRestoreList)
                {
                    // 成功匹配到后的处理方式：只需检查ItemData和ItemType
                    if (restoreItem.ItemType == BackupItemType.SelectItem)
                    {
                        string restoreItemData = restoreItem.ItemData;
                        if (restoreItemData != itemData)
                        {
                            int.TryParse(restoreItem.KeyName, out int itemDataIndex);
                            switch (currentScene)
                            {
                                case Scenes.DragDrop:
                                    DropEffect dropEffect = (DropEffect)itemDataIndex;
                                    if (DefaultDropEffect != dropEffect)
                                    {
                                        DefaultDropEffect = dropEffect;
                                    }
                                    break;
                            }
                            restoreList.Add(new RestoreChangedItem(currentScene, keyName, restoreItemData.ToString()));
                        }
                    }
                }
            }
            item.Dispose();
            return;
        }

        /*******************************ShellList.cs************************************/

        private void GetShellListItems()
        {
            string scenePath = null;
            switch (currentScene)
            {
                case Scenes.File:
                    scenePath = MENUPATH_FILE; break;
                case Scenes.Folder:
                    scenePath = MENUPATH_FOLDER; break;
                case Scenes.Directory:
                    scenePath = MENUPATH_DIRECTORY; break;
                case Scenes.Background:
                    scenePath = MENUPATH_BACKGROUND; break;
                case Scenes.Desktop:
                    //Vista系统没有这一项
                    if (WinOsVersion.Current == WinOsVersion.Vista) return;
                    scenePath = MENUPATH_DESKTOP; break;
                case Scenes.Drive:
                    scenePath = MENUPATH_DRIVE; break;
                case Scenes.AllObjects:
                    scenePath = MENUPATH_ALLOBJECTS; break;
                case Scenes.Computer:
                    scenePath = MENUPATH_COMPUTER; break;
                case Scenes.RecycleBin:
                    scenePath = MENUPATH_RECYCLEBIN; break;
                case Scenes.Library:
                    //Vista系统没有这一项
                    if (WinOsVersion.Current == WinOsVersion.Vista) return;
                    scenePath = MENUPATH_LIBRARY; break;
                case Scenes.LnkFile:
                    scenePath = GetOpenModePath(".lnk"); break;
                case Scenes.UwpLnk:
                    //Win8之前没有Uwp
                    if (WinOsVersion.Current < WinOsVersion.Win8) return;
                    scenePath = MENUPATH_UWPLNK; break;
                case Scenes.ExeFile:
                    scenePath = GetSysAssExtPath(".exe"); break;
                case Scenes.UnknownType:
                    scenePath = MENUPATH_UNKNOWN; break;
                case Scenes.DragDrop:
                    SelectItem item = new SelectItem(currentScene);
                    string dropEffect = ((int)DefaultDropEffect).ToString();
                    BackupRestoreSelectItem(item, dropEffect, currentScene);
                    GetBackupShellExItems(GetShellExPath(MENUPATH_FOLDER));
                    GetBackupShellExItems(GetShellExPath(MENUPATH_DIRECTORY));
                    GetBackupShellExItems(GetShellExPath(MENUPATH_DRIVE));
                    GetBackupShellExItems(GetShellExPath(MENUPATH_ALLOBJECTS));
                    return;
                case Scenes.PublicReferences:
                    //Vista系统没有这一项
                    if (WinOsVersion.Current == WinOsVersion.Vista) return;
                    GetBackupStoreItems();
                    return;
            }
#if DEBUG
            if (AppConfig.EnableLog)
            {
                using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                {
                    sw.WriteLine($@"BackupItems: {currentScene}");
                }
            }
            int i = 0;
#endif
            // 获取ShellItem与ShellExItem类的备份项目
            GetBackupItems(scenePath);
            if (WinOsVersion.Current >= WinOsVersion.Win10)
            {
                // 获取UwpModeItem类的备份项目
                GetBackupUwpModeItem();
            }
            switch (currentScene)
            {
                case Scenes.Background:
                    VisibleRegRuleItem item = new VisibleRegRuleItem(VisibleRegRuleItem.CustomFolder);
                    string regPath = item.RegPath;
                    string valueName = item.ValueName;
                    string itemName = item.Text;
                    bool ifItemInMenu = item.ItemVisible;
                    BackupRestoreItem(item, valueName, BackupItemType.VisibleRegRuleItem, ifItemInMenu, currentScene);
#if DEBUG
                    i++;
                    if (AppConfig.EnableLog)
                    {
                        using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                        {
                            sw.WriteLine("\tBackupAddedItems");
                            sw.WriteLine("\t\t" + $@"{i}. {valueName} {itemName} {ifItemInMenu} {regPath}");
                        }
                    }
#endif
                    break;
                case Scenes.Computer:
                    item = new VisibleRegRuleItem(VisibleRegRuleItem.NetworkDrive);
                    regPath = item.RegPath;
                    valueName = item.ValueName;
                    itemName = item.Text;
                    ifItemInMenu = item.ItemVisible;
                    BackupRestoreItem(item, valueName, BackupItemType.VisibleRegRuleItem, ifItemInMenu, currentScene);
#if DEBUG
                    i++;
                    if (AppConfig.EnableLog)
                    {
                        using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                        {
                            sw.WriteLine("\tBackupAddedItems");
                            sw.WriteLine("\t\t" + $@"{i}. {valueName} {itemName} {ifItemInMenu} {regPath}");
                        }
                    }
#endif
                    break;
                case Scenes.RecycleBin:
                    item = new VisibleRegRuleItem(VisibleRegRuleItem.RecycleBinProperties);
                    regPath = item.RegPath;
                    valueName = item.ValueName;
                    itemName = item.Text;
                    ifItemInMenu = item.ItemVisible;
                    BackupRestoreItem(item, valueName, BackupItemType.VisibleRegRuleItem, ifItemInMenu, currentScene);
#if DEBUG
                    i++;
                    if (AppConfig.EnableLog)
                    {
                        using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                        {
                            sw.WriteLine("\tBackupAddedItems");
                            sw.WriteLine("\t\t" + $@"{i}. {valueName} {itemName} {ifItemInMenu} {regPath}");
                        }
                    }
#endif
                    break;
                case Scenes.Library:
#if DEBUG
                    if (AppConfig.EnableLog)
                    {
                        using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                        {
                            sw.WriteLine("\tBackupAddedItems");
                        }
                    }
#endif
                    string[] AddedScenePathes = new string[] { MENUPATH_LIBRARY_BACKGROUND, MENUPATH_LIBRARY_USER };
                    RegTrustedInstaller.TakeRegKeyOwnerShip(scenePath);
                    for (int j = 0; j < AddedScenePathes.Length; j++)
                    {
                        scenePath = AddedScenePathes[j];
                        GetBackupShellItems(GetShellPath(scenePath));
                        GetBackupShellExItems(GetShellExPath(scenePath));
                    }
                    break;
                case Scenes.ExeFile:
                    GetBackupItems(GetOpenModePath(".exe"));
                    break;
            }
        }

        private void GetBackupItems(string scenePath)
        {
            if (scenePath == null) return;
            RegTrustedInstaller.TakeRegKeyOwnerShip(scenePath);
            GetBackupShellItems(GetShellPath(scenePath));
            GetBackupShellExItems(GetShellExPath(scenePath));
        }

        private void GetBackupShellItems(string shellPath)
        {
#if DEBUG
            if (AppConfig.EnableLog)
            {
                using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                {
                    sw.WriteLine("\tGetBackupShellItems");
                }
            }
            int i = 0;
#endif
            using (RegistryKey shellKey = RegistryEx.GetRegistryKey(shellPath))
            {
                if (shellKey == null) return;
                RegTrustedInstaller.TakeRegTreeOwnerShip(shellKey.Name);
                foreach (string keyName in shellKey.GetSubKeyNames())
                {
                    string regPath = $@"{shellPath}\{keyName}";
                    ShellItem item = new ShellItem(regPath);
                    string itemName = item.ItemText;
                    bool ifItemInMenu = item.ItemVisible;
                    BackupRestoreItem(item, keyName, BackupItemType.ShellItem, ifItemInMenu, currentScene);
#if DEBUG
                    i++;
                    if (AppConfig.EnableLog)
                    {
                        using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                        {
                            sw.WriteLine("\t\t" + $@"{i}. {keyName} {itemName} {ifItemInMenu} {regPath}");
                        }
                    }
#endif
                }
            }
        }

        private void GetBackupShellExItems(string shellExPath)
        {
#if DEBUG
            if (AppConfig.EnableLog)
            {
                using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                {
                    sw.WriteLine("\tGetBackupShellExItems");
                }
            }
            int i = 0;
#endif
            List<string> names = new List<string>();
            using (RegistryKey shellExKey = RegistryEx.GetRegistryKey(shellExPath))
            {
                if (shellExKey == null) return;
                bool isDragDrop = currentScene == Scenes.DragDrop;
                RegTrustedInstaller.TakeRegTreeOwnerShip(shellExKey.Name);
                Dictionary<string, Guid> dic = ShellExItem.GetPathAndGuids(shellExPath, isDragDrop);
                FoldGroupItem groupItem = null;
                if (isDragDrop)
                {
                    groupItem = GetDragDropGroupItem(shellExPath);
#if DEBUG
                    if (AppConfig.EnableLog)
                    {
                        using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                        {
                            sw.WriteLine($@"\t\t!!!!!!{shellExPath}(FoldGroupItem)");
                        }
                    }
#endif
                }
                foreach (string path in dic.Keys)
                {
                    string keyName = RegistryEx.GetKeyName(path);
                    if (!names.Contains(keyName))
                    {
                        string regPath = path; // 随是否显示于右键菜单中而改变
                        Guid guid = dic[path];
                        ShellExItem item = new ShellExItem(guid, path);
                        string itemName = item.ItemText;
                        bool ifItemInMenu = item.ItemVisible;
                        if (groupItem != null)
                        {
                            item.FoldGroupItem = groupItem;
                            item.Indent();
                        }
                        BackupRestoreItem(item, keyName, BackupItemType.ShellExItem, ifItemInMenu, currentScene);
                        names.Add(keyName);
#if DEBUG
                        i++;
                        if (AppConfig.EnableLog)
                        {
                            using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                            {
                                sw.WriteLine("\t\t" + $@"{i}. {keyName} {itemName} {ifItemInMenu} {regPath}");
                            }
                        }
#endif
                    }
                }
            }
        }

        private void GetBackupStoreItems()
        {
#if DEBUG
            if (AppConfig.EnableLog)
            {
                using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                {
                    sw.WriteLine($@"BackupItems: {currentScene}");
                }
            }
            int i = 0;
#endif
            using (RegistryKey shellKey = RegistryEx.GetRegistryKey(ShellItem.CommandStorePath))
            {
                foreach (string itemName in shellKey.GetSubKeyNames())
                {
                    if (AppConfig.HideSysStoreItems && itemName.StartsWith("Windows.", StringComparison.OrdinalIgnoreCase)) continue;
                    StoreShellItem item = new StoreShellItem($@"{ShellItem.CommandStorePath}\{itemName}", true, false);
                    string regPath = item.RegPath;
                    bool ifItemInMenu = item.ItemVisible;
                    BackupRestoreItem(item, itemName, BackupItemType.StoreShellItem, ifItemInMenu, currentScene);
#if DEBUG
                    i++;
                    if (AppConfig.EnableLog)
                    {
                        using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                        {
                            sw.WriteLine("\tBackupStoreItems");
                            sw.WriteLine("\t\t" + $@"{i}. {itemName} {itemName} {ifItemInMenu} {regPath}");
                        }
                    }
#endif
                }
            }
        }

        private void GetBackupUwpModeItem()
        {
#if DEBUG
            if (AppConfig.EnableLog)
            {
                using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                {
                    sw.WriteLine("\tGetBackupUwpModeItem");
                }
            }
            int i = 0;
#endif
            List<Guid> guidList = new List<Guid>();
            foreach (XmlDocument doc in XmlDicHelper.UwpModeItemsDic)
            {
                if (doc?.DocumentElement == null) continue;
                foreach (XmlNode sceneXN in doc.DocumentElement.ChildNodes)
                {
                    if (sceneXN.Name == currentScene.ToString())
                    {
                        foreach (XmlElement itemXE in sceneXN.ChildNodes)
                        {
                            if (GuidEx.TryParse(itemXE.GetAttribute("Guid"), out Guid guid))
                            {
                                if (guidList.Contains(guid)) continue;
                                if (GuidInfo.GetFilePath(guid) == null) continue;
                                guidList.Add(guid);
                                string uwpName = GuidInfo.GetUwpName(guid); // uwp程序的名称
                                UwpModeItem uwpItem = new UwpModeItem(uwpName, guid);
                                string keyName = uwpItem.Text; // 右键菜单索引
                                // TODO:修复名称显示错误的问题
                                string itemName = keyName;  // 右键菜单名称
                                bool ifItemInMenu = uwpItem.ItemVisible;
                                BackupRestoreItem(uwpItem, keyName, BackupItemType.UwpModelItem, ifItemInMenu, currentScene);
#if DEBUG
                                i++;
                                if (AppConfig.EnableLog)
                                {
                                    using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                                    {
                                        sw.WriteLine("\t\t" + $@"{i}. {keyName}({uwpName}) {itemName} {ifItemInMenu} {guid}");
                                    }
                                }
#endif
                            }
                        }
                    }
                }
            }
        }

        private FoldGroupItem GetDragDropGroupItem(string shellExPath)
        {
            string text = null;
            Image image = null;
            string path = shellExPath.Substring(0, shellExPath.LastIndexOf('\\'));
            switch (path)
            {
                case MENUPATH_FOLDER:
                    text = AppString.SideBar.Folder;
                    image = AppImage.Folder;
                    break;
                case MENUPATH_DIRECTORY:
                    text = AppString.SideBar.Directory;
                    image = AppImage.Directory;
                    break;
                case MENUPATH_DRIVE:
                    text = AppString.SideBar.Drive;
                    image = AppImage.Drive;
                    break;
                case MENUPATH_ALLOBJECTS:
                    text = AppString.SideBar.AllObjects;
                    image = AppImage.AllObjects;
                    break;
            }
            return new FoldGroupItem(shellExPath, ObjectPath.PathType.Registry) { Text = text, Image = image };
        }

        /*******************************ShellNewList.cs************************************/

        private void GetShellNewListBackupItems()
        {
#if DEBUG
            if (AppConfig.EnableLog)
            {
                using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                {
                    sw.WriteLine($@"BackupShellNewItems:");
                }
            }
            int i = 0;
#endif
            if (ShellNewLockItem.IsLocked)
            {
#if DEBUG
                if (AppConfig.EnableLog)
                {
                    using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                    {
                        sw.WriteLine("\tBackupLockItems");
                    }
                }
#endif
                string[] extensions = (string[])Registry.GetValue(ShellNewPath, "Classes", null);
                GetShellNewBackupItems(extensions.ToList());
            }
            else
            {
#if DEBUG
                if (AppConfig.EnableLog)
                {
                    using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                    {
                        sw.WriteLine("\tBackupUnlockItems");
                    }
                }
#endif
                List<string> extensions = new List<string> { "Folder" };//文件夹
                using (RegistryKey root = Registry.ClassesRoot)
                {
                    extensions.AddRange(Array.FindAll(root.GetSubKeyNames(), keyName => keyName.StartsWith(".")));
                    if (WinOsVersion.Current < WinOsVersion.Win10) extensions.Add("Briefcase");//公文包(Win10没有)
                    GetShellNewBackupItems(extensions);
                }
            }
        }

        private void GetShellNewBackupItems(List<string> extensions)
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
                                        ShellNewItem item = new ShellNewItem(snKey.Name);
                                        string regPath = item.RegPath;
                                        string openMode = item.OpenMode;
                                        string itemName = item.Text;
                                        bool ifItemInMenu = item.ItemVisible;
                                        BackupRestoreItem(item, openMode, BackupItemType.ShellNewItem, ifItemInMenu, currentScene);
#if DEBUG
                                        i++;
                                        if (AppConfig.EnableLog)
                                        {
                                            using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                                            {
                                                sw.WriteLine("\t\t" + $@"{i}. {openMode} {itemName} {ifItemInMenu} {regPath}");
                                            }
                                        }
#endif
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /*******************************SendToList.cs************************************/

        private void GetSendToListItems()
        {
#if DEBUG
            if (AppConfig.EnableLog)
            {
                using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                {
                    sw.WriteLine("BackupSendToItems");
                    sw.WriteLine("\tGetSendToItems");
                }
            }
            int i = 0;
#endif
            string filePath, itemFileName, itemName;
            bool ifItemInMenu;
            foreach (string path in Directory.GetFileSystemEntries(SendToList.SendToPath))
            {
                if (Path.GetFileName(path).ToLower() == "desktop.ini") continue;
                SendToItem sendToItem = new SendToItem(path);
                filePath = sendToItem.FilePath;
                itemFileName = sendToItem.ItemFileName;
                itemName = sendToItem.Text;
                ifItemInMenu = sendToItem.ItemVisible;
                BackupRestoreItem(sendToItem, itemFileName, BackupItemType.SendToItem, ifItemInMenu, currentScene);
#if DEBUG
                i = 0;
                i++;
                if (AppConfig.EnableLog)
                {
                    using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                    {
                        sw.WriteLine("\t\t" + $@"{i}. {itemFileName} {itemName} {ifItemInMenu} {filePath}");
                    }
                }
#endif
            }
            VisibleRegRuleItem item = new VisibleRegRuleItem(VisibleRegRuleItem.SendToDrive);
            string regPath = item.RegPath;
            string valueName = item.ValueName;
            itemName = item.Text;
            ifItemInMenu = item.ItemVisible;
            BackupRestoreItem(item, valueName, BackupItemType.VisibleRegRuleItem, ifItemInMenu, currentScene);
#if DEBUG
            i = 0;
            i++;
            if (AppConfig.EnableLog)
            {
                using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                {
                    sw.WriteLine("\tBackupAddedItems");
                    sw.WriteLine("\t\t" + $@"{i}. {valueName} {itemName} {ifItemInMenu} {regPath}");
                }
            }
#endif
            item = new VisibleRegRuleItem(VisibleRegRuleItem.DeferBuildSendTo);
            regPath = item.RegPath;
            valueName = item.ValueName;
            itemName = item.Text;
            ifItemInMenu = item.ItemVisible;
            BackupRestoreItem(item, valueName, BackupItemType.VisibleRegRuleItem, ifItemInMenu, currentScene);
#if DEBUG
            i++;
            if (AppConfig.EnableLog)
            {
                using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                {
                    sw.WriteLine("\t\t" + $@"{i}. {valueName} {itemName} {ifItemInMenu} {regPath}");
                }
            }
#endif
        }

        /*******************************OpenWithList.cs************************************/

        private void GetOpenWithListItems()
        {
#if DEBUG
            if (AppConfig.EnableLog)
            {
                using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                {
                    sw.WriteLine("BackupOpenWithItems");
                    sw.WriteLine("\tGetOpenWithItems");
                }
            }
            int i = 0;
#endif
            using (RegistryKey root = Registry.ClassesRoot)
            using (RegistryKey appKey = root.OpenSubKey("Applications"))
            {
                foreach (string appName in appKey.GetSubKeyNames())
                {
                    if (!appName.Contains('.')) continue;
                    using (RegistryKey shellKey = appKey.OpenSubKey($@"{appName}\shell"))
                    {
                        if (shellKey == null) continue;

                        List<string> names = shellKey.GetSubKeyNames().ToList();
                        if (names.Contains("open", StringComparer.OrdinalIgnoreCase)) names.Insert(0, "open");

                        string keyName = names.Find(name =>
                        {
                            using (RegistryKey cmdKey = shellKey.OpenSubKey(name))
                                return cmdKey.GetValue("NeverDefault") == null;
                        });
                        if (keyName == null) continue;

                        using (RegistryKey commandKey = shellKey.OpenSubKey($@"{keyName}\command"))
                        {
                            string command = commandKey?.GetValue("")?.ToString();
                            if (ObjectPath.ExtractFilePath(command) != null)
                            {
                                OpenWithItem item = new OpenWithItem(commandKey.Name);
                                string regPath = item.RegPath;
                                string itemFileName = item.ItemFileName;
                                string itemName = item.Text;
                                bool ifItemInMenu = item.ItemVisible;
                                BackupRestoreItem(item, itemFileName, BackupItemType.OpenWithItem, ifItemInMenu, currentScene);
#if DEBUG
                                i++;
                                if (AppConfig.EnableLog)
                                {
                                    using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                                    {
                                        sw.WriteLine("\tBackupAddedItems");
                                        sw.WriteLine("\t\t" + $@"{i}. {itemFileName} {itemName} {ifItemInMenu} {regPath}");
                                    }
                                }
#endif
                            }
                        }
                    }
                }
            }
            //Win8及以上版本系统才有在应用商店中查找应用
            if (WinOsVersion.Current >= WinOsVersion.Win8)
            {
                VisibleRegRuleItem storeItem = new VisibleRegRuleItem(VisibleRegRuleItem.UseStoreOpenWith);
                string regPath = storeItem.RegPath;
                string valueName = storeItem.ValueName;
                string itemName = storeItem.Text;
                bool ifItemInMenu = storeItem.ItemVisible;
                BackupRestoreItem(storeItem, valueName, BackupItemType.VisibleRegRuleItem, ifItemInMenu, currentScene);
#if DEBUG
                i = 1;
                if (AppConfig.EnableLog)
                {
                    using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                    {
                        sw.WriteLine("\tBackupAddedItems");
                        sw.WriteLine("\t\t" + $@"{i}. {valueName} {itemName} {ifItemInMenu} {regPath}");
                    }
                }
#endif
            }
        }

        /*******************************WinXList.cs************************************/

        private void GetWinXListItems()
        {
#if DEBUG
            if (AppConfig.EnableLog)
            {
                using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                {
                    sw.WriteLine("BackupWinXItems");
                    sw.WriteLine("\tGetWinXItems");
                }
            }
            int i = 0;
#endif
            if (WinOsVersion.Current >= WinOsVersion.Win8)
            {
                AppConfig.BackupWinX();
                string[] dirPaths1 = Directory.Exists(WinXList.WinXPath) ? Directory.GetDirectories(WinXList.WinXPath) : new string[] { };
                string[] dirPaths2 = Directory.Exists(WinXList.BackupWinXPath) ? Directory.GetDirectories(WinXList.BackupWinXPath) : new string[] { };
                List<string> dirKeyPaths = new List<string> { };
                foreach (string dirPath in dirPaths1)
                {
                    string keyName = Path.GetFileNameWithoutExtension(dirPath);
                    dirKeyPaths.Add(keyName);
                }
                foreach (string dirPath in dirPaths2)
                {
                    string keyName = Path.GetFileNameWithoutExtension(dirPath);
                    if (!dirKeyPaths.Contains(keyName)) dirKeyPaths.Add(keyName);
                }
                dirKeyPaths.Sort();
                dirKeyPaths.Reverse();

                foreach (string dirKeyPath in dirKeyPaths)
                {
                    string dirPath1 = $@"{WinXList.WinXPath}\{dirKeyPath}";
                    string dirPath2 = $@"{WinXList.BackupWinXPath}\{dirKeyPath}";

                    WinXGroupItem groupItem = new WinXGroupItem(dirPath1);

                    List<string> lnkPaths;
                    lnkPaths = WinXList.GetInkFiles(dirKeyPath);

                    foreach (string path in lnkPaths)
                    {
                        WinXItem item = new WinXItem(path, groupItem);
                        string filePath = item.FilePath;
                        string fileName = item.FileName;
                        // 删除文件名称里的顺序索引
                        int index = fileName.IndexOf(" - ");
                        fileName = fileName.Substring(index + 3);
                        string itemName = item.Text;
                        bool ifItemInMenu = item.ItemVisible;
                        BackupRestoreItem(item, fileName, BackupItemType.WinXItem, ifItemInMenu, currentScene);
#if DEBUG
                        i++;
                        if (AppConfig.EnableLog)
                        {
                            using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                            {
                                sw.WriteLine("\t\t" + $@"{i}. {fileName} {itemName} {ifItemInMenu} {filePath}");
                            }
                        }
#endif
                    }
                    groupItem.Dispose();
                }
            }
        }

        /*******************************IEList.cs************************************/

        private void GetIEItems()
        {
            List<string> names = new List<string>();
            using (RegistryKey ieKey = RegistryEx.GetRegistryKey(IEList.IEPath))
            {
                if (ieKey == null) return;
                foreach (string part in IEItem.MeParts)
                {
                    using (RegistryKey meKey = ieKey.OpenSubKey(part))
                    {
                        if (meKey == null) continue;
                        foreach (string keyName in meKey.GetSubKeyNames())
                        {
                            if (names.Contains(keyName, StringComparer.OrdinalIgnoreCase)) continue;
                            using (RegistryKey key = meKey.OpenSubKey(keyName))
                            {
                                if (!string.IsNullOrEmpty(key.GetValue("")?.ToString()))
                                {
                                    IEItem item = new IEItem(key.Name);
                                    bool ifItemInMenu = item.ItemVisible;
                                    BackupRestoreItem(item, keyName, BackupItemType.IEItem, ifItemInMenu, currentScene);
                                    names.Add(keyName);
                                }
                            }
                        }
                    }
                }
            }
        }

        /*******************************DetailedEditList.cs************************************/

        private void GetDetailedEditListItems()
        {
            for (int index = 0; index < 2; index++)
            {
                // 获取系统字典或用户字典
                XmlDocument doc = XmlDicHelper.DetailedEditDic[index];
                if (doc?.DocumentElement == null) return;
                // 遍历所有子节点
                foreach (XmlNode groupXN in doc.DocumentElement.ChildNodes)
                {
                    try
                    {
                        // 获取Guid列表
                        List<Guid> guids = new List<Guid>();
                        XmlNodeList guidList = groupXN.SelectNodes("Guid");
                        foreach (XmlNode guidXN in guidList)
                        {
                            if (!GuidEx.TryParse(guidXN.InnerText, out Guid guid)) continue;
                            if (!File.Exists(GuidInfo.GetFilePath(guid))) continue;
                            guids.Add(guid);
                        }
                        if (guidList.Count > 0 && guids.Count == 0) continue;

                        // 获取groupItem列表
                        FoldGroupItem groupItem;
                        bool isIniGroup = groupXN.SelectSingleNode("IsIniGroup") != null;
                        string attribute = isIniGroup ? "FilePath" : "RegPath";
                        ObjectPath.PathType pathType = isIniGroup ? ObjectPath.PathType.File : ObjectPath.PathType.Registry;
                        groupItem = new FoldGroupItem(groupXN.SelectSingleNode(attribute)?.InnerText, pathType);

                        string GetRuleFullRegPath(string regPath)
                        {
                            if (string.IsNullOrEmpty(regPath)) regPath = groupItem.GroupPath;
                            else if (regPath.StartsWith("\\")) regPath = groupItem.GroupPath + regPath;
                            return regPath;
                        };

                        // 遍历groupItem内所有Item节点
                        foreach (XmlElement itemXE in groupXN.SelectNodes("Item"))
                        {
                            try
                            {
                                if (!XmlDicHelper.JudgeOSVersion(itemXE)) continue;
                                RuleItem ruleItem;
                                ItemInfo info = new ItemInfo();

                                // 获取文本、提示文本
                                foreach (XmlElement textXE in itemXE.SelectNodes("Text"))
                                {
                                    if (XmlDicHelper.JudgeCulture(textXE)) info.Text = ResourceString.GetDirectString(textXE.GetAttribute("Value"));
                                }
                                foreach (XmlElement tipXE in itemXE.SelectNodes("Tip"))
                                {
                                    if (XmlDicHelper.JudgeCulture(tipXE)) info.Tip = ResourceString.GetDirectString(tipXE.GetAttribute("Value"));
                                }
                                info.RestartExplorer = itemXE.SelectSingleNode("RestartExplorer") != null;

                                // 如果是数值类型的，初始化默认值、最大值、最小值
                                int defaultValue = 0, maxValue = 0, minValue = 0;
                                if (itemXE.SelectSingleNode("IsNumberItem") != null)
                                {
                                    XmlElement ruleXE = (XmlElement)itemXE.SelectSingleNode("Rule");
                                    defaultValue = ruleXE.HasAttribute("Default") ? Convert.ToInt32(ruleXE.GetAttribute("Default")) : 0;
                                    maxValue = ruleXE.HasAttribute("Max") ? Convert.ToInt32(ruleXE.GetAttribute("Max")) : int.MaxValue;
                                    minValue = ruleXE.HasAttribute("Min") ? Convert.ToInt32(ruleXE.GetAttribute("Min")) : int.MinValue;
                                }

                                // 建立三种类型的RuleItem
                                if (isIniGroup)
                                {
                                    XmlElement ruleXE = (XmlElement)itemXE.SelectSingleNode("Rule");
                                    string iniPath = ruleXE.GetAttribute("FilePath");
                                    if (iniPath.IsNullOrWhiteSpace()) iniPath = groupItem.GroupPath;
                                    string section = ruleXE.GetAttribute("Section");
                                    string keyName = ruleXE.GetAttribute("KeyName");
                                    if (itemXE.SelectSingleNode("IsNumberItem") != null)
                                    {
                                        var rule = new NumberIniRuleItem.IniRule
                                        {
                                            IniPath = iniPath,
                                            Section = section,
                                            KeyName = keyName,
                                            DefaultValue = defaultValue,
                                            MaxValue = maxValue,
                                            MinValue = maxValue
                                        };
                                        ruleItem = new NumberIniRuleItem(rule, info);
                                        string infoText = info.Text;
                                        int itemValue = ((NumberIniRuleItem)ruleItem).ItemValue;
                                        BackupRestoreItem(ruleItem, infoText, BackupItemType.NumberIniRuleItem, itemValue, currentScene);
                                    }
                                    else if (itemXE.SelectSingleNode("IsStringItem") != null)
                                    {
                                        var rule = new StringIniRuleItem.IniRule
                                        {
                                            IniPath = iniPath,
                                            Secation = section,
                                            KeyName = keyName
                                        };
                                        ruleItem = new StringIniRuleItem(rule, info);
                                        string infoText = info.Text;
                                        string itemValue = ((StringIniRuleItem)ruleItem).ItemValue;
                                        BackupRestoreItem(ruleItem, infoText, BackupItemType.StringIniRuleItem, itemValue, currentScene);
                                    }
                                    else
                                    {
                                        var rule = new VisbleIniRuleItem.IniRule
                                        {
                                            IniPath = iniPath,
                                            Section = section,
                                            KeyName = keyName,
                                            TurnOnValue = ruleXE.HasAttribute("On") ? ruleXE.GetAttribute("On") : null,
                                            TurnOffValue = ruleXE.HasAttribute("Off") ? ruleXE.GetAttribute("Off") : null,
                                        };
                                        ruleItem = new VisbleIniRuleItem(rule, info);
                                        string infoText = info.Text;
                                        bool itemVisible = ((VisbleIniRuleItem)ruleItem).ItemVisible;
                                        BackupRestoreItem(ruleItem, infoText, BackupItemType.VisbleIniRuleItem, itemVisible, currentScene);
                                    }
                                }
                                else
                                {
                                    if (itemXE.SelectSingleNode("IsNumberItem") != null)
                                    {
                                        XmlElement ruleXE = (XmlElement)itemXE.SelectSingleNode("Rule");
                                        var rule = new NumberRegRuleItem.RegRule
                                        {
                                            RegPath = GetRuleFullRegPath(ruleXE.GetAttribute("RegPath")),
                                            ValueName = ruleXE.GetAttribute("ValueName"),
                                            ValueKind = XmlDicHelper.GetValueKind(ruleXE.GetAttribute("ValueKind"), RegistryValueKind.DWord),
                                            DefaultValue = defaultValue,
                                            MaxValue = maxValue,
                                            MinValue = minValue
                                        };
                                        ruleItem = new NumberRegRuleItem(rule, info);
                                        string infoText = info.Text;
                                        int itemValue = ((NumberRegRuleItem)ruleItem).ItemValue;// 备份值
                                        BackupRestoreItem(ruleItem, infoText, BackupItemType.NumberRegRuleItem, itemValue, currentScene);
                                    }
                                    else if (itemXE.SelectSingleNode("IsStringItem") != null)
                                    {
                                        XmlElement ruleXE = (XmlElement)itemXE.SelectSingleNode("Rule");
                                        var rule = new StringRegRuleItem.RegRule
                                        {
                                            RegPath = GetRuleFullRegPath(ruleXE.GetAttribute("RegPath")),
                                            ValueName = ruleXE.GetAttribute("ValueName"),
                                        };
                                        ruleItem = new StringRegRuleItem(rule, info);
                                        string infoText = info.Text;
                                        string itemValue = ((StringRegRuleItem)ruleItem).ItemValue; // 备份值
                                        BackupRestoreItem(ruleItem, infoText, BackupItemType.StringRegRuleItem, itemValue, currentScene);
                                    }
                                    else
                                    {
                                        XmlNodeList ruleXNList = itemXE.SelectNodes("Rule");
                                        var rules = new VisibleRegRuleItem.RegRule[ruleXNList.Count];
                                        for (int i = 0; i < ruleXNList.Count; i++)
                                        {
                                            XmlElement ruleXE = (XmlElement)ruleXNList[i];
                                            rules[i] = new VisibleRegRuleItem.RegRule
                                            {
                                                RegPath = GetRuleFullRegPath(ruleXE.GetAttribute("RegPath")),
                                                ValueName = ruleXE.GetAttribute("ValueName"),
                                                ValueKind = XmlDicHelper.GetValueKind(ruleXE.GetAttribute("ValueKind"), RegistryValueKind.DWord)
                                            };
                                            string turnOn = ruleXE.HasAttribute("On") ? ruleXE.GetAttribute("On") : null;
                                            string turnOff = ruleXE.HasAttribute("Off") ? ruleXE.GetAttribute("Off") : null;
                                            switch (rules[i].ValueKind)
                                            {
                                                case RegistryValueKind.Binary:
                                                    rules[i].TurnOnValue = turnOn != null ? XmlDicHelper.ConvertToBinary(turnOn) : null;
                                                    rules[i].TurnOffValue = turnOff != null ? XmlDicHelper.ConvertToBinary(turnOff) : null;
                                                    break;
                                                case RegistryValueKind.DWord:
                                                    if (turnOn == null) rules[i].TurnOnValue = null;
                                                    else rules[i].TurnOnValue = Convert.ToInt32(turnOn);
                                                    if (turnOff == null) rules[i].TurnOffValue = null;
                                                    else rules[i].TurnOffValue = Convert.ToInt32(turnOff);
                                                    break;
                                                default:
                                                    rules[i].TurnOnValue = turnOn;
                                                    rules[i].TurnOffValue = turnOff;
                                                    break;
                                            }
                                        }
                                        ruleItem = new VisibleRegRuleItem(rules, info);
                                        string infoText = info.Text;
                                        bool itemVisible = ((VisibleRegRuleItem)ruleItem).ItemVisible;  // 备份值
                                        BackupRestoreItem(ruleItem, infoText, BackupItemType.VisibleRegRuleItem, itemVisible, currentScene);
                                    }
                                }
                                groupItem.Dispose();
                            }
                            catch { continue; }
                        }
                    }
                    catch { continue; }
                }
            }
        }

        /*******************************EnhanceMenusListList.cs************************************/

        private void GetEnhanceMenuListItems()
        {
            for (int index = 0; index < 2; index++)
            {
                XmlDocument doc = XmlDicHelper.EnhanceMenusDic[index];
                if (doc?.DocumentElement == null) return;
                foreach (XmlNode xn in doc.DocumentElement.ChildNodes)
                {
                    try
                    {
                        string text = null;
                        string path = xn.SelectSingleNode("RegPath")?.InnerText;
                        foreach (XmlElement textXE in xn.SelectNodes("Text"))
                        {
                            if (XmlDicHelper.JudgeCulture(textXE))
                            {
                                text = ResourceString.GetDirectString(textXE.GetAttribute("Value"));
                            }
                        }
                        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(text)) continue;

                        FoldGroupItem groupItem = new FoldGroupItem(path, ObjectPath.PathType.Registry)
                        {
                            Image = null,
                            Text = text
                        };
                        XmlNode shellXN = xn.SelectSingleNode("Shell");
                        XmlNode shellExXN = xn.SelectSingleNode("ShellEx");
                        if (shellXN != null) GetEnhanceMenuListShellItems(shellXN, groupItem);
                        if (shellExXN != null) GetEnhanceMenuListShellExItems(shellExXN, groupItem);
                        groupItem.Dispose();
                    }
                    catch { continue; }
                }
            }
        }

        private void GetEnhanceMenuListShellItems(XmlNode shellXN, FoldGroupItem groupItem)
        {
            foreach (XmlElement itemXE in shellXN.SelectNodes("Item"))
            {
                if (!XmlDicHelper.FileExists(itemXE)) continue;
                if (!XmlDicHelper.JudgeCulture(itemXE)) continue;
                if (!XmlDicHelper.JudgeOSVersion(itemXE)) continue;
                string keyName = itemXE.GetAttribute("KeyName");
                if (keyName.IsNullOrWhiteSpace()) continue;
                EnhanceShellItem item = new EnhanceShellItem()
                {
                    RegPath = $@"{groupItem.GroupPath}\shell\{keyName}",
                    FoldGroupItem = groupItem,
                    ItemXE = itemXE
                };
                foreach (XmlElement szXE in itemXE.SelectNodes("Value/REG_SZ"))
                {
                    if (!XmlDicHelper.JudgeCulture(szXE)) continue;
                    if (szXE.HasAttribute("MUIVerb")) item.Text = ResourceString.GetDirectString(szXE.GetAttribute("MUIVerb"));
                    if (szXE.HasAttribute("Icon")) item.Image = ResourceIcon.GetIcon(szXE.GetAttribute("Icon"))?.ToBitmap();
                    else if (szXE.HasAttribute("HasLUAShield")) item.Image = AppImage.Shield;
                }
                if (item.Image == null)
                {
                    XmlElement cmdXE = (XmlElement)itemXE.SelectSingleNode("SubKey/Command");
                    if (cmdXE != null)
                    {
                        Icon icon = null;
                        if (cmdXE.HasAttribute("Default"))
                        {
                            string filePath = ObjectPath.ExtractFilePath(cmdXE.GetAttribute("Default"));
                            icon = ResourceIcon.GetIcon(filePath);
                        }
                        else
                        {
                            XmlNode fileXE = cmdXE.SelectSingleNode("FileName");
                            if (fileXE != null)
                            {
                                string filePath = ObjectPath.ExtractFilePath(fileXE.InnerText);
                                icon = ResourceIcon.GetIcon(filePath);
                            }
                        }
                        item.Image = icon?.ToBitmap();
                        icon?.Dispose();
                    }
                }
                if (item.Image == null) item.Image = AppImage.NotFound;
                if (item.Text.IsNullOrWhiteSpace()) item.Text = keyName;
                string tip = "";
                foreach (XmlElement tipXE in itemXE.SelectNodes("Tip"))
                {
                    if (XmlDicHelper.JudgeCulture(tipXE)) tip = tipXE.GetAttribute("Value");
                }
                if (itemXE.GetElementsByTagName("CreateFile").Count > 0)
                {
                    if (!tip.IsNullOrWhiteSpace()) tip += "\n";
                    tip += AppString.Tip.CommandFiles;
                }
                ToolTipBox.SetToolTip(item.ChkVisible, tip);
                string itemText = item.Text;
                bool itemVisible = item.ItemVisible;
                BackupRestoreItem(item, itemText, BackupItemType.EnhanceShellItem, itemVisible, currentScene);
            }
        }

        private void GetEnhanceMenuListShellExItems(XmlNode shellExXN, FoldGroupItem groupItem)
        {
            foreach (XmlNode itemXN in shellExXN.SelectNodes("Item"))
            {
                if (!XmlDicHelper.FileExists(itemXN)) continue;
                if (!XmlDicHelper.JudgeCulture(itemXN)) continue;
                if (!XmlDicHelper.JudgeOSVersion(itemXN)) continue;
                if (!GuidEx.TryParse(itemXN.SelectSingleNode("Guid")?.InnerText, out Guid guid)) continue;
                EnhanceShellExItem item = new EnhanceShellExItem
                {
                    FoldGroupItem = groupItem,
                    ShellExPath = $@"{groupItem.GroupPath}\ShellEx",
                    Image = ResourceIcon.GetIcon(itemXN.SelectSingleNode("Icon")?.InnerText)?.ToBitmap() ?? AppImage.SystemFile,
                    DefaultKeyName = itemXN.SelectSingleNode("KeyName")?.InnerText,
                    Guid = guid
                };
                foreach (XmlNode textXE in itemXN.SelectNodes("Text"))
                {
                    if (XmlDicHelper.JudgeCulture(textXE))
                    {
                        item.Text = ResourceString.GetDirectString(textXE.InnerText);
                    }
                }
                if (item.Text.IsNullOrWhiteSpace()) item.Text = GuidInfo.GetText(guid);
                if (item.DefaultKeyName.IsNullOrWhiteSpace()) item.DefaultKeyName = guid.ToString("B");
                string tip = "";
                foreach (XmlElement tipXE in itemXN.SelectNodes("Tip"))
                {
                    if (XmlDicHelper.JudgeCulture(tipXE)) tip = tipXE.GetAttribute("Text");
                }
                ToolTipBox.SetToolTip(item.ChkVisible, tip);
                string itemText = item.Text;
                bool itemVisible = item.ItemVisible;
                BackupRestoreItem(item, itemText, BackupItemType.EnhanceShellExItem, itemVisible, currentScene);
            }
        }
    }

    public sealed class BackupList
    {
        // 元数据缓存区
        public static MetaData metaData = new MetaData();  

        // 备份列表/恢复列表缓存区
        private static List<BackupItem> backupRestoreList = new List<BackupItem>();

        // 单场景恢复列表暂存区
        public static List<BackupItem> sceneRestoreList = new List<BackupItem>();

        // 创建一个XmlSerializer实例并设置根节点
        private static readonly XmlSerializer backupDataSerializer = new XmlSerializer(typeof(BackupData), 
            new XmlRootAttribute("ContextMenuManager"));
        // 自定义命名空间
        private static readonly XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();

        // 创建一个XmlSerializer实例并设置根节点
        private static readonly XmlSerializer metaDataSerializer = new XmlSerializer(typeof(MetaData),
            new XmlRootAttribute("MetaData"));

        static BackupList()
        {
            // 禁用默认命名空间
            namespaces.Add(string.Empty, string.Empty);
        }

        public static void AddItem(string keyName, BackupItemType backupItemType, string itemData, Scenes scene)
        {
            backupRestoreList.Add(new BackupItem
            {
                KeyName = keyName,
                ItemType = backupItemType,
                ItemData = itemData,
                BackupScene = scene,
            });
        }

        public static void AddItem(string keyName, BackupItemType backupItemType, bool itemData, Scenes scene)
        {
            AddItem(keyName, backupItemType, itemData.ToString(), scene);
        }

        public static void AddItem(string keyName, BackupItemType backupItemType, int itemData, Scenes scene)
        {
            AddItem(keyName, backupItemType, itemData.ToString(), scene);
        }

        public static int GetBackupListCount()
        {
            return backupRestoreList.Count;
        }

        public static void ClearBackupList()
        {
            backupRestoreList.Clear();
        }

        public static void SaveBackupList(string filePath)
        {
            // 创建一个父对象，并将BackupList和MetaData对象包装到其中
            BackupData myData = new BackupData()
            {
                MetaData = metaData,
                BackupList = backupRestoreList,
            };

            // 序列化root对象并保存到XML文档
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            {
                backupDataSerializer.Serialize(stream, myData, namespaces);
            }
        }

        public static void LoadBackupList(string filePath)
        {
            // 反序列化XML文件并获取根对象
            BackupData myData;
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            {
                myData = (BackupData)backupDataSerializer.Deserialize(stream);
            }

            // 获取MetaData对象
            metaData = myData.MetaData;

            // 清理backupRestoreList变量
            backupRestoreList.Clear();
            backupRestoreList = null;

            // 获取BackupList对象
            backupRestoreList = myData.BackupList;
        }

        public static void LoadTempRestoreList(Scenes scene)
        {
            sceneRestoreList.Clear();
            // 根据backupScene加载列表
            foreach (BackupItem item in backupRestoreList)
            {
                if (item.BackupScene == scene)
                {
                    sceneRestoreList.Add(item);
                }
            }
        }

        public static void LoadBackupDataMetaData(string filePath)
        {
            // 反序列化root对象并保存到XML文档
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            {
                // 读取 <MetaData> 节点
                using (XmlReader reader = XmlReader.Create(stream))
                {
                    // 寻找第一个<MetaData>节点
                    reader.ReadToFollowing("MetaData");

                    // 清理metaData变量
                    metaData = null;

                    // 反序列化<MetaData>节点为MetaData对象
                    metaData = (MetaData)metaDataSerializer.Deserialize(reader);
                }
            }
        }
    }

    // 定义一个类来表示备份数据
    [Serializable, XmlType("BackupData")]
    public sealed class BackupData
    {
        [XmlElement("MetaData")]
        public MetaData MetaData { get; set; }

        [XmlElement("BackupList")]
        public List<BackupItem> BackupList { get; set; }
    }

    // 定义一个类来表示备份项目
    [Serializable, XmlType("BackupItem")]
    public sealed class BackupItem
    {
        [XmlElement("KeyName")]
        public string KeyName { get; set; } // 查询索引名字

        [XmlElement("ItemType")]
        public BackupItemType ItemType { get; set; } // 备份项目类型

        [XmlElement("ItemData")]
        public string ItemData { get; set; } // 备份数据：是否位于右键菜单中，数字，或者字符串

        [XmlElement("Scene")]
        public Scenes BackupScene { get; set; } // 右键菜单位置
    }

    // 定义一个类来表示备份项目的元数据
    [Serializable, XmlType("MetaData")]
    public sealed class MetaData
    {
        [XmlElement("Version")]
        public int Version { get; set; } // 备份版本

        [XmlElement("BackupScenes")]
        public List<Scenes> BackupScenes { get; set; } // 备份场景

        [XmlElement("CreateTime")]
        public DateTime CreateTime { get; set; } // 备份时间

        [XmlElement("Device")]
        public string Device { get; set; } // 备份设备
    }

    // 定义一个类来表示恢复项目
    public sealed class RestoreChangedItem
    {
        public RestoreChangedItem(Scenes scene, string keyName, string itemData)
        {
            BackupScene = scene;
            KeyName = keyName;
            ItemData = itemData;
        }

        public Scenes BackupScene { get; set; } // 右键菜单位置

        public string KeyName { get; set; } // 查询索引名字

        public string ItemData { get; set; } // 备份数据：是否位于右键菜单中，数字，或者字符串
    }
}
