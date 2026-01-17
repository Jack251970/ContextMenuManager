using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using static ContextMenuManager.Controls.ShellList;
using static ContextMenuManager.Controls.ShellNewList;
using static ContextMenuManager.Methods.BackupList;

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
        // 文件类型——第二板块
        CustomExtension, PerceivedType, DirectoryType,
        // 其他规则——第一板块
        EnhanceMenu, DetailedEdit,
        // 其他规则——第二板块
        DragDrop, PublicReferences, InternetExplorer,
        // 其他规则——第三板块（不予备份）
        // 不予备份的场景
        MenuAnalysis, CustomRegPath, CustomExtensionPerceivedType,
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

    internal sealed class BackupHelper
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

        private bool CheckItemNeedChange(string itemName, string keyName, BackupItemType itemType, bool currentItemData)
        {
            foreach (var item in sceneRestoreList)
            {
                // 成功匹配到后的处理方式：KeyName和ItemType匹配后检查ItemVisible
                if (item.KeyName == keyName && item.ItemType == itemType)
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
            foreach (var item in sceneRestoreList)
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
            foreach (var item in sceneRestoreList)
            {
                // 成功匹配到后的处理方式：KeyName和ItemType匹配后检查itemData
                if (item.KeyName == keyName && item.ItemType == itemType)
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

        /*******************************ShellList.cs************************************/

        private void GetShellListItems()
        {
            string scenePath = null;
            currentExtension = null;
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
                case Scenes.CustomExtension:
                    foreach (var fileExtension in FileExtensionDialog.FileExtensionItems)
                    {
                        // From: FileExtensionDialog.Extension
                        var extensionProperty = fileExtension.Trim();
                        // From: FileExtensionDialog.RunDialog
                        var extension = ObjectPath.RemoveIllegalChars(extensionProperty);
                        var index = extension.LastIndexOf('.');
                        if (index >= 0) extensionProperty = extension[index..];
                        else extensionProperty = $".{extension}";
                        // From: ShellList.LoadItems
                        var isLnk = extensionProperty?.ToLower() == ".lnk";
                        if (isLnk) scenePath = GetOpenModePath(".lnk");
                        else scenePath = GetSysAssExtPath(extensionProperty);
                        currentExtension = extensionProperty;
                        GetShellListItems(scenePath);
                    }
                    return;
                case Scenes.PerceivedType:
                    foreach (var perceivedType in PerceivedTypes)
                    {
                        scenePath = GetSysAssExtPath(perceivedType);
                        currentExtension = perceivedType;
                        GetShellListItems(scenePath);
                    }
                    return;
                case Scenes.DirectoryType:
                    foreach (var directoryType in DirectoryTypes)
                    {
                        if (directoryType == null) scenePath = null;
                        else scenePath = GetSysAssExtPath($"Directory.{directoryType}");
                        currentExtension = directoryType;
                        GetShellListItems(scenePath);
                    }
                    return;
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
                    var item = new SelectItem(currentScene);
                    var dropEffect = ((int)DefaultDropEffect).ToString();
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
            // 获取ShellItem与ShellExItem类的备份项目
            GetShellListItems(scenePath);
            switch (currentScene)
            {
                case Scenes.Background:
                    var item = new VisibleRegRuleItem(VisibleRegRuleItem.CustomFolder);
                    var regPath = item.RegPath;
                    var valueName = item.ValueName;
                    var itemName = item.Text;
                    var ifItemInMenu = item.ItemVisible;
                    BackupRestoreItem(item, itemName, valueName, BackupItemType.VisibleRegRuleItem, ifItemInMenu, currentScene);
                    break;
                case Scenes.Computer:
                    item = new VisibleRegRuleItem(VisibleRegRuleItem.NetworkDrive);
                    regPath = item.RegPath;
                    valueName = item.ValueName;
                    itemName = item.Text;
                    ifItemInMenu = item.ItemVisible;
                    BackupRestoreItem(item, itemName, valueName, BackupItemType.VisibleRegRuleItem, ifItemInMenu, currentScene);
                    break;
                case Scenes.RecycleBin:
                    item = new VisibleRegRuleItem(VisibleRegRuleItem.RecycleBinProperties);
                    regPath = item.RegPath;
                    valueName = item.ValueName;
                    itemName = item.Text;
                    ifItemInMenu = item.ItemVisible;
                    BackupRestoreItem(item, itemName, valueName, BackupItemType.VisibleRegRuleItem, ifItemInMenu, currentScene);
                    break;
                case Scenes.Library:
                    var AddedScenePathes = new string[] { MENUPATH_LIBRARY_BACKGROUND, MENUPATH_LIBRARY_USER };
                    RegTrustedInstaller.TakeRegKeyOwnerShip(scenePath);
                    for (var j = 0; j < AddedScenePathes.Length; j++)
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

        private void GetShellListItems(string scenePath)
        {
            // 获取ShellItem与ShellExItem类的备份项目
            GetBackupItems(scenePath);
            if (WinOsVersion.Current >= WinOsVersion.Win10)
            {
                // 获取UwpModeItem类的备份项目
                GetBackupUwpModeItem();
            }
            // From: ShellList.LoadItems
            // 自选文件扩展名后加载对应的右键菜单
            if (currentScene == Scenes.CustomExtension && currentExtension != null)
            {
                GetBackupItems(GetOpenModePath(currentExtension));
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
            using var shellKey = RegistryEx.GetRegistryKey(shellPath);
            if (shellKey == null) return;
            RegTrustedInstaller.TakeRegTreeOwnerShip(shellKey.Name);
            foreach (var keyName in shellKey.GetSubKeyNames())
            {
                var regPath = $@"{shellPath}\{keyName}";
                var item = new ShellItem(regPath);
                var itemName = item.ItemText;
                var ifItemInMenu = item.ItemVisible;
                if (currentScene is Scenes.CustomExtension or Scenes.PerceivedType or Scenes.DirectoryType)
                {
                    // 加入Extension类别来区分这几个板块的不同备份项目
                    BackupRestoreItem(item, itemName, $"{currentExtension}{keyName}", BackupItemType.ShellItem, ifItemInMenu, currentScene);
                }
                else
                {
                    BackupRestoreItem(item, itemName, keyName, BackupItemType.ShellItem, ifItemInMenu, currentScene);
                }
            }
        }

        private void GetBackupShellExItems(string shellExPath)
        {
            var names = new List<string>();
            using var shellExKey = RegistryEx.GetRegistryKey(shellExPath);
            if (shellExKey == null) return;
            var isDragDrop = currentScene == Scenes.DragDrop;
            RegTrustedInstaller.TakeRegTreeOwnerShip(shellExKey.Name);
            var dic = ShellExItem.GetPathAndGuids(shellExPath, isDragDrop);
            FoldGroupItem groupItem = null;
            if (isDragDrop)
            {
                groupItem = GetDragDropGroupItem(shellExPath);
            }
            foreach (var path in dic.Keys)
            {
                var keyName = RegistryEx.GetKeyName(path);
                if (!names.Contains(keyName))
                {
                    var regPath = path; // 随是否显示于右键菜单中而改变
                    var guid = dic[path];
                    var item = new ShellExItem(guid, path);
                    var itemName = item.ItemText;
                    var ifItemInMenu = item.ItemVisible;
                    if (groupItem != null)
                    {
                        item.FoldGroupItem = groupItem;
                        item.Indent();
                    }
                    if (currentScene is Scenes.CustomExtension or Scenes.PerceivedType or Scenes.DirectoryType)
                    {
                        // 加入Extension类别来区分这几个板块的不同备份项目
                        BackupRestoreItem(item, itemName, $"{currentExtension}{keyName}", BackupItemType.ShellExItem, ifItemInMenu, currentScene);
                    }
                    else
                    {
                        BackupRestoreItem(item, itemName, keyName, BackupItemType.ShellExItem, ifItemInMenu, currentScene);
                    }

                    names.Add(keyName);
                }
            }
        }

        private void GetBackupStoreItems()
        {
            using var shellKey = RegistryEx.GetRegistryKey(ShellItem.CommandStorePath);
            foreach (var itemName in shellKey.GetSubKeyNames())
            {
                if (AppConfig.HideSysStoreItems && itemName.StartsWith("Windows.", StringComparison.OrdinalIgnoreCase)) continue;
                var item = new StoreShellItem($@"{ShellItem.CommandStorePath}\{itemName}", true, false);
                var regPath = item.RegPath;
                var ifItemInMenu = item.ItemVisible;
                BackupRestoreItem(item, itemName, itemName, BackupItemType.StoreShellItem, ifItemInMenu, currentScene);
            }
        }

        private void GetBackupUwpModeItem()
        {
            var guidList = new List<Guid>();
            foreach (var doc in XmlDicHelper.UwpModeItemsDic)
            {
                if (doc?.DocumentElement == null) continue;
                foreach (XmlNode sceneXN in doc.DocumentElement.ChildNodes)
                {
                    if (sceneXN.Name == currentScene.ToString())
                    {
                        foreach (XmlElement itemXE in sceneXN.ChildNodes)
                        {
                            if (GuidEx.TryParse(itemXE.GetAttribute("Guid"), out var guid))
                            {
                                if (guidList.Contains(guid)) continue;
                                if (GuidInfo.GetFilePath(guid) == null) continue;
                                guidList.Add(guid);
                                var uwpName = GuidInfo.GetUwpName(guid); // uwp程序的名称
                                var uwpItem = new UwpModeItem(uwpName, guid);
                                var keyName = uwpItem.Text; // 右键菜单索引
                                // TODO:修复名称显示错误的问题
                                var itemName = keyName;  // 右键菜单名称
                                var ifItemInMenu = uwpItem.ItemVisible;
                                BackupRestoreItem(uwpItem, itemName, keyName, BackupItemType.UwpModelItem, ifItemInMenu, currentScene);
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
            var path = shellExPath[..shellExPath.LastIndexOf('\\')];
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
            if (ShellNewLockItem.IsLocked)
            {
                var extensions = (string[])Registry.GetValue(ShellNewPath, "Classes", null);
                GetShellNewBackupItems(extensions.ToList());
            }
            else
            {
                var extensions = new List<string> { "Folder" };//文件夹
                using var root = Registry.ClassesRoot;
                extensions.AddRange(Array.FindAll(root.GetSubKeyNames(), keyName => keyName.StartsWith(".")));
                if (WinOsVersion.Current < WinOsVersion.Win10) extensions.Add("Briefcase");//公文包(Win10没有)
                GetShellNewBackupItems(extensions);
            }
        }

        private void GetShellNewBackupItems(List<string> extensions)
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
                        var item = new ShellNewItem(snKey.Name);
                        var regPath = item.RegPath;
                        var openMode = item.OpenMode;
                        var itemName = item.Text;
                        var ifItemInMenu = item.ItemVisible;
                        BackupRestoreItem(item, itemName, openMode, BackupItemType.ShellNewItem, ifItemInMenu, currentScene);
                        break;
                    }
                }
            }
        }

        /*******************************SendToList.cs************************************/

        private void GetSendToListItems()
        {
            string filePath, itemFileName, itemName;
            bool ifItemInMenu;
            foreach (var path in Directory.GetFileSystemEntries(SendToList.SendToPath))
            {
                if (Path.GetFileName(path).ToLower() == "desktop.ini") continue;
                var sendToItem = new SendToItem(path);
                filePath = sendToItem.FilePath;
                itemFileName = sendToItem.ItemFileName;
                itemName = sendToItem.Text;
                ifItemInMenu = sendToItem.ItemVisible;
                BackupRestoreItem(sendToItem, itemName, itemFileName, BackupItemType.SendToItem, ifItemInMenu, currentScene);
            }
            var item = new VisibleRegRuleItem(VisibleRegRuleItem.SendToDrive);
            var regPath = item.RegPath;
            var valueName = item.ValueName;
            itemName = item.Text;
            ifItemInMenu = item.ItemVisible;
            BackupRestoreItem(item, itemName, valueName, BackupItemType.VisibleRegRuleItem, ifItemInMenu, currentScene);
            item = new VisibleRegRuleItem(VisibleRegRuleItem.DeferBuildSendTo);
            regPath = item.RegPath;
            valueName = item.ValueName;
            itemName = item.Text;
            ifItemInMenu = item.ItemVisible;
            BackupRestoreItem(item, itemName, valueName, BackupItemType.VisibleRegRuleItem, ifItemInMenu, currentScene);
        }

        /*******************************OpenWithList.cs************************************/

        private void GetOpenWithListItems()
        {
            using (var root = Registry.ClassesRoot)
            using (var appKey = root.OpenSubKey("Applications"))
            {
                foreach (var appName in appKey.GetSubKeyNames())
                {
                    if (!appName.Contains('.')) continue;
                    using var shellKey = appKey.OpenSubKey($@"{appName}\shell");
                    if (shellKey == null) continue;

                    var names = shellKey.GetSubKeyNames().ToList();
                    if (names.Contains("open", StringComparer.OrdinalIgnoreCase)) names.Insert(0, "open");

                    var keyName = names.Find(name =>
                    {
                        using var cmdKey = shellKey.OpenSubKey(name);
                        return cmdKey.GetValue("NeverDefault") == null;
                    });
                    if (keyName == null) continue;

                    using var commandKey = shellKey.OpenSubKey($@"{keyName}\command");
                    var command = commandKey?.GetValue("")?.ToString();
                    if (ObjectPath.ExtractFilePath(command) != null)
                    {
                        var item = new OpenWithItem(commandKey.Name);
                        var regPath = item.RegPath;
                        var itemFileName = item.ItemFileName;
                        var itemName = item.Text;
                        var ifItemInMenu = item.ItemVisible;
                        BackupRestoreItem(item, itemName, itemFileName, BackupItemType.OpenWithItem, ifItemInMenu, currentScene);
                    }
                }
            }
            //Win8及以上版本系统才有在应用商店中查找应用
            if (WinOsVersion.Current >= WinOsVersion.Win8)
            {
                var storeItem = new VisibleRegRuleItem(VisibleRegRuleItem.UseStoreOpenWith);
                var regPath = storeItem.RegPath;
                var valueName = storeItem.ValueName;
                var itemName = storeItem.Text;
                var ifItemInMenu = storeItem.ItemVisible;
                BackupRestoreItem(storeItem, itemName, valueName, BackupItemType.VisibleRegRuleItem, ifItemInMenu, currentScene);
            }
        }

        /*******************************WinXList.cs************************************/

        private void GetWinXListItems()
        {
            if (WinOsVersion.Current >= WinOsVersion.Win8)
            {
                AppConfig.BackupWinX();
                var dirPaths1 = Directory.Exists(WinXList.WinXPath) ? Directory.GetDirectories(WinXList.WinXPath) : new string[] { };
                var dirPaths2 = Directory.Exists(WinXList.BackupWinXPath) ? Directory.GetDirectories(WinXList.BackupWinXPath) : new string[] { };
                var dirKeyPaths = new List<string> { };
                foreach (var dirPath in dirPaths1)
                {
                    var keyName = Path.GetFileNameWithoutExtension(dirPath);
                    dirKeyPaths.Add(keyName);
                }
                foreach (var dirPath in dirPaths2)
                {
                    var keyName = Path.GetFileNameWithoutExtension(dirPath);
                    if (!dirKeyPaths.Contains(keyName)) dirKeyPaths.Add(keyName);
                }
                dirKeyPaths.Sort();
                dirKeyPaths.Reverse();

                foreach (var dirKeyPath in dirKeyPaths)
                {
                    var dirPath1 = $@"{WinXList.WinXPath}\{dirKeyPath}";
                    var dirPath2 = $@"{WinXList.BackupWinXPath}\{dirKeyPath}";

                    var groupItem = new WinXGroupItem(dirPath1);

                    List<string> lnkPaths;
                    lnkPaths = WinXList.GetInkFiles(dirKeyPath);

                    foreach (var path in lnkPaths)
                    {
                        var item = new WinXItem(path, groupItem);
                        var filePath = item.FilePath;
                        var fileName = item.FileName;
                        // 删除文件名称里的顺序索引
                        var index = fileName.IndexOf(" - ");
                        fileName = fileName[(index + 3)..];
                        var itemName = item.Text;
                        var ifItemInMenu = item.ItemVisible;
                        BackupRestoreItem(item, itemName, fileName, BackupItemType.WinXItem, ifItemInMenu, currentScene);
                    }
                    groupItem.Dispose();
                }
            }
        }

        /*******************************IEList.cs************************************/

        private void GetIEItems()
        {
            var names = new List<string>();
            using var ieKey = RegistryEx.GetRegistryKey(IEList.IEPath);
            if (ieKey == null) return;
            foreach (var part in IEItem.MeParts)
            {
                using var meKey = ieKey.OpenSubKey(part);
                if (meKey == null) continue;
                foreach (var keyName in meKey.GetSubKeyNames())
                {
                    if (names.Contains(keyName, StringComparer.OrdinalIgnoreCase)) continue;
                    using var key = meKey.OpenSubKey(keyName);
                    if (!string.IsNullOrEmpty(key.GetValue("")?.ToString()))
                    {
                        var item = new IEItem(key.Name);
                        var itemName = item.Text;
                        var ifItemInMenu = item.ItemVisible;
                        BackupRestoreItem(item, itemName, keyName, BackupItemType.IEItem, ifItemInMenu, currentScene);
                        names.Add(keyName);
                    }
                }
            }
        }

        /*******************************DetailedEditList.cs************************************/

        private void GetDetailedEditListItems()
        {
            for (var index = 0; index < 2; index++)
            {
                // 获取系统字典或用户字典
                var doc = XmlDicHelper.DetailedEditDic[index];
                if (doc?.DocumentElement == null) return;
                // 遍历所有子节点
                foreach (XmlNode groupXN in doc.DocumentElement.ChildNodes)
                {
                    try
                    {
                        // 获取Guid列表
                        var guids = new List<Guid>();
                        var guidList = groupXN.SelectNodes("Guid");
                        foreach (XmlNode guidXN in guidList)
                        {
                            if (!GuidEx.TryParse(guidXN.InnerText, out var guid)) continue;
                            if (!File.Exists(GuidInfo.GetFilePath(guid))) continue;
                            guids.Add(guid);
                        }
                        if (guidList.Count > 0 && guids.Count == 0) continue;

                        // 获取groupItem列表
                        FoldGroupItem groupItem;
                        var isIniGroup = groupXN.SelectSingleNode("IsIniGroup") != null;
                        var attribute = isIniGroup ? "FilePath" : "RegPath";
                        var pathType = isIniGroup ? ObjectPath.PathType.File : ObjectPath.PathType.Registry;
                        groupItem = new FoldGroupItem(groupXN.SelectSingleNode(attribute)?.InnerText, pathType);

                        string GetRuleFullRegPath(string regPath)
                        {
                            if (string.IsNullOrEmpty(regPath)) regPath = groupItem.GroupPath;
                            else if (regPath.StartsWith("\\")) regPath = groupItem.GroupPath + regPath;
                            return regPath;
                        }
                        ;

                        // 遍历groupItem内所有Item节点
                        foreach (XmlElement itemXE in groupXN.SelectNodes("Item"))
                        {
                            try
                            {
                                if (!XmlDicHelper.JudgeOSVersion(itemXE)) continue;
                                RuleItem ruleItem;
                                var info = new ItemInfo();

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
                                    var ruleXE = (XmlElement)itemXE.SelectSingleNode("Rule");
                                    defaultValue = ruleXE.HasAttribute("Default") ? Convert.ToInt32(ruleXE.GetAttribute("Default")) : 0;
                                    maxValue = ruleXE.HasAttribute("Max") ? Convert.ToInt32(ruleXE.GetAttribute("Max")) : int.MaxValue;
                                    minValue = ruleXE.HasAttribute("Min") ? Convert.ToInt32(ruleXE.GetAttribute("Min")) : int.MinValue;
                                }

                                // 建立三种类型的RuleItem
                                if (isIniGroup)
                                {
                                    var ruleXE = (XmlElement)itemXE.SelectSingleNode("Rule");
                                    var iniPath = ruleXE.GetAttribute("FilePath");
                                    if (iniPath.IsNullOrWhiteSpace()) iniPath = groupItem.GroupPath;
                                    var section = ruleXE.GetAttribute("Section");
                                    var keyName = ruleXE.GetAttribute("KeyName");
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
                                        var itemName = ruleItem.Text;
                                        var infoText = info.Text;
                                        var itemValue = ((NumberIniRuleItem)ruleItem).ItemValue;
                                        BackupRestoreItem(ruleItem, itemName, infoText, BackupItemType.NumberIniRuleItem, itemValue, currentScene);
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
                                        var itemName = ruleItem.Text;
                                        var infoText = info.Text;
                                        var itemValue = ((StringIniRuleItem)ruleItem).ItemValue;
                                        BackupRestoreItem(ruleItem, itemName, infoText, BackupItemType.StringIniRuleItem, itemValue, currentScene);
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
                                        var infoText = info.Text;
                                        var itemName = ruleItem.Text;
                                        var itemVisible = ((VisbleIniRuleItem)ruleItem).ItemVisible;
                                        BackupRestoreItem(ruleItem, itemName, infoText, BackupItemType.VisbleIniRuleItem, itemVisible, currentScene);
                                    }
                                }
                                else
                                {
                                    if (itemXE.SelectSingleNode("IsNumberItem") != null)
                                    {
                                        var ruleXE = (XmlElement)itemXE.SelectSingleNode("Rule");
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
                                        var itemName = ruleItem.Text;
                                        var infoText = info.Text;
                                        var itemValue = ((NumberRegRuleItem)ruleItem).ItemValue;// 备份值
                                        BackupRestoreItem(ruleItem, itemName, infoText, BackupItemType.NumberRegRuleItem, itemValue, currentScene);
                                    }
                                    else if (itemXE.SelectSingleNode("IsStringItem") != null)
                                    {
                                        var ruleXE = (XmlElement)itemXE.SelectSingleNode("Rule");
                                        var rule = new StringRegRuleItem.RegRule
                                        {
                                            RegPath = GetRuleFullRegPath(ruleXE.GetAttribute("RegPath")),
                                            ValueName = ruleXE.GetAttribute("ValueName"),
                                        };
                                        ruleItem = new StringRegRuleItem(rule, info);
                                        var itemName = ruleItem.Text;
                                        var infoText = info.Text;
                                        var itemValue = ((StringRegRuleItem)ruleItem).ItemValue; // 备份值
                                        BackupRestoreItem(ruleItem, itemName, infoText, BackupItemType.StringRegRuleItem, itemValue, currentScene);
                                    }
                                    else
                                    {
                                        var ruleXNList = itemXE.SelectNodes("Rule");
                                        var rules = new VisibleRegRuleItem.RegRule[ruleXNList.Count];
                                        for (var i = 0; i < ruleXNList.Count; i++)
                                        {
                                            var ruleXE = (XmlElement)ruleXNList[i];
                                            rules[i] = new VisibleRegRuleItem.RegRule
                                            {
                                                RegPath = GetRuleFullRegPath(ruleXE.GetAttribute("RegPath")),
                                                ValueName = ruleXE.GetAttribute("ValueName"),
                                                ValueKind = XmlDicHelper.GetValueKind(ruleXE.GetAttribute("ValueKind"), RegistryValueKind.DWord)
                                            };
                                            var turnOn = ruleXE.HasAttribute("On") ? ruleXE.GetAttribute("On") : null;
                                            var turnOff = ruleXE.HasAttribute("Off") ? ruleXE.GetAttribute("Off") : null;
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
                                        var itemName = ruleItem.Text;
                                        var infoText = info.Text;
                                        var itemVisible = ((VisibleRegRuleItem)ruleItem).ItemVisible;  // 备份值
                                        BackupRestoreItem(ruleItem, itemName, infoText, BackupItemType.VisibleRegRuleItem, itemVisible, currentScene);
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
            for (var index = 0; index < 2; index++)
            {
                var doc = XmlDicHelper.EnhanceMenusDic[index];
                if (doc?.DocumentElement == null) return;
                foreach (XmlNode xn in doc.DocumentElement.ChildNodes)
                {
                    try
                    {
                        string text = null;
                        var path = xn.SelectSingleNode("RegPath")?.InnerText;
                        foreach (XmlElement textXE in xn.SelectNodes("Text"))
                        {
                            if (XmlDicHelper.JudgeCulture(textXE))
                            {
                                text = ResourceString.GetDirectString(textXE.GetAttribute("Value"));
                            }
                        }
                        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(text)) continue;

                        var groupItem = new FoldGroupItem(path, ObjectPath.PathType.Registry)
                        {
                            Image = null,
                            Text = text
                        };
                        var shellXN = xn.SelectSingleNode("Shell");
                        var shellExXN = xn.SelectSingleNode("ShellEx");
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
                var keyName = itemXE.GetAttribute("KeyName");
                if (keyName.IsNullOrWhiteSpace()) continue;
                var item = new EnhanceShellItem()
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
                    var cmdXE = (XmlElement)itemXE.SelectSingleNode("SubKey/Command");
                    if (cmdXE != null)
                    {
                        Icon icon = null;
                        if (cmdXE.HasAttribute("Default"))
                        {
                            var filePath = ObjectPath.ExtractFilePath(cmdXE.GetAttribute("Default"));
                            icon = ResourceIcon.GetIcon(filePath);
                        }
                        else
                        {
                            var fileXE = cmdXE.SelectSingleNode("FileName");
                            if (fileXE != null)
                            {
                                var filePath = ObjectPath.ExtractFilePath(fileXE.InnerText);
                                icon = ResourceIcon.GetIcon(filePath);
                            }
                        }
                        item.Image = icon?.ToBitmap();
                        icon?.Dispose();
                    }
                }
                if (item.Image == null) item.Image = AppImage.NotFound;
                if (item.Text.IsNullOrWhiteSpace()) item.Text = keyName;
                var tip = "";
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
                var itemName = item.Text;
                var regPath = item.RegPath;
                var pathSegments = regPath.Split('\\');
                var index = Array.LastIndexOf(pathSegments, "shell");
                string itemKey;
                if (index != -1 && index < pathSegments.Length - 1)
                {
                    var targetFields = new string[pathSegments.Length - index];
                    Array.Copy(pathSegments, index, targetFields, 0, targetFields.Length);
                    itemKey = string.Join("\\", targetFields);
                }
                else
                {
                    itemKey = regPath;
                }
                var itemVisible = item.ItemVisible;
                BackupRestoreItem(item, itemName, itemKey, BackupItemType.EnhanceShellItem, itemVisible, currentScene);
            }
        }

        private void GetEnhanceMenuListShellExItems(XmlNode shellExXN, FoldGroupItem groupItem)
        {
            foreach (XmlNode itemXN in shellExXN.SelectNodes("Item"))
            {
                if (!XmlDicHelper.FileExists(itemXN)) continue;
                if (!XmlDicHelper.JudgeCulture(itemXN)) continue;
                if (!XmlDicHelper.JudgeOSVersion(itemXN)) continue;
                if (!GuidEx.TryParse(itemXN.SelectSingleNode("Guid")?.InnerText, out var guid)) continue;
                var item = new EnhanceShellExItem
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
                var tip = "";
                foreach (XmlElement tipXE in itemXN.SelectNodes("Tip"))
                {
                    if (XmlDicHelper.JudgeCulture(tipXE)) tip = tipXE.GetAttribute("Text");
                }
                ToolTipBox.SetToolTip(item.ChkVisible, tip);
                var itemName = item.Text;
                var regPath = item.RegPath;
                var pathSegments = regPath.Split('\\');
                var index = Array.LastIndexOf(pathSegments, "ShellEx");
                string itemKey;
                if (index != -1 && index < pathSegments.Length - 1)
                {
                    var targetFields = new string[pathSegments.Length - index];
                    Array.Copy(pathSegments, index, targetFields, 0, targetFields.Length);
                    itemKey = string.Join("\\", targetFields);
                }
                else
                {
                    itemKey = regPath;
                }
                var itemVisible = item.ItemVisible;
                BackupRestoreItem(item, itemName, itemKey, BackupItemType.EnhanceShellExItem, itemVisible, currentScene);
            }
        }
    }

    public sealed class BackupList
    {
        // 元数据缓存区
        public static MetaData metaData = new();

        // 备份列表/恢复列表缓存区
        private static List<BackupItem> backupRestoreList = new();

        // 单场景恢复列表暂存区
        public static List<BackupItem> sceneRestoreList = new();

        // 创建一个XmlSerializer实例并设置根节点
        private static readonly XmlSerializer backupDataSerializer = new(typeof(BackupData),
            new XmlRootAttribute("ContextMenuManager"));
        // 自定义命名空间
        private static readonly XmlSerializerNamespaces namespaces = new();

        // 创建一个XmlSerializer实例并设置根节点
        private static readonly XmlSerializer metaDataSerializer = new(typeof(MetaData),
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
            var myData = new BackupData()
            {
                MetaData = metaData,
                BackupList = backupRestoreList,
            };

            // 序列化root对象并保存到XML文档
            using var stream = new FileStream(filePath, FileMode.Create);
            backupDataSerializer.Serialize(stream, myData, namespaces);
        }

        public static void LoadBackupList(string filePath)
        {
            // 反序列化XML文件并获取根对象
            BackupData myData;
            using (var stream = new FileStream(filePath, FileMode.Open))
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
            foreach (var item in backupRestoreList)
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
            using var stream = new FileStream(filePath, FileMode.Open);
            // 读取 <MetaData> 节点
            using var reader = XmlReader.Create(stream);
            // 寻找第一个<MetaData>节点
            reader.ReadToFollowing("MetaData");

            // 清理metaData变量
            metaData = null;

            // 反序列化<MetaData>节点为MetaData对象
            metaData = (MetaData)metaDataSerializer.Deserialize(reader);
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
