using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Xml;
using static ContextMenuManager.Controls.ShellList;

namespace ContextMenuManager.Methods
{
    internal sealed partial class BackupHelper
    {
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
                    if (!backup) RegTrustedInstaller.TakeRegKeyOwnerShip(scenePath);
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
            if (!backup) RegTrustedInstaller.TakeRegKeyOwnerShip(scenePath);
            GetBackupShellItems(GetShellPath(scenePath));
            GetBackupShellExItems(GetShellExPath(scenePath));
        }

        private void GetBackupShellItems(string shellPath)
        {
            using var shellKey = GetRegistryKeySafe(shellPath);
            if (shellKey == null) return;
            if (!backup) RegTrustedInstaller.TakeRegTreeOwnerShip(shellKey.Name);
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
            using var shellExKey = GetRegistryKeySafe(shellExPath);
            if (shellExKey == null) return;
            var isDragDrop = currentScene == Scenes.DragDrop;
            if (!backup) RegTrustedInstaller.TakeRegTreeOwnerShip(shellExKey.Name);
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
            using var shellKey = GetRegistryKeySafe(ShellItem.CommandStorePath);
            if (shellKey == null) return;
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
    }
}
