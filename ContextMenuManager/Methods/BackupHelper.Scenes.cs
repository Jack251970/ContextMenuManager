using ContextMenuManager.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using static ContextMenuManager.Methods.BackupList;

namespace ContextMenuManager.Methods
{
    internal sealed partial class BackupHelper
    {
        /*******************************场景处理************************************/

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
                if (dialogInterface.IsCancelled) return;
                currentScene = scene;
                // 加载某个Scene的恢复列表
                if (!backup)
                {
                    LoadTempRestoreList(currentScene);
                }
                GetBackupItems(dialogInterface);
                dialogInterface?.SetProgress(currentScenes.IndexOf(scene) + 1, GetSceneName(scene));
            }
        }

        private static string GetSceneName(Scenes scene)
        {
            return scene switch
            {
                Scenes.File => AppString.SideBar.File,
                Scenes.Folder => AppString.SideBar.Folder,
                Scenes.Directory => AppString.SideBar.Directory,
                Scenes.Background => AppString.SideBar.Background,
                Scenes.Desktop => AppString.SideBar.Desktop,
                Scenes.Drive => AppString.SideBar.Drive,
                Scenes.AllObjects => AppString.SideBar.AllObjects,
                Scenes.Computer => AppString.SideBar.Computer,
                Scenes.RecycleBin => AppString.SideBar.RecycleBin,
                Scenes.Library => AppString.SideBar.Library,
                Scenes.New => AppString.SideBar.New,
                Scenes.SendTo => AppString.SideBar.SendTo,
                Scenes.OpenWith => AppString.SideBar.OpenWith,
                Scenes.WinX => AppString.SideBar.WinX,
                Scenes.LnkFile => AppString.SideBar.LnkFile,
                Scenes.UwpLnk => AppString.SideBar.UwpLnk,
                Scenes.ExeFile => AppString.SideBar.ExeFile,
                Scenes.UnknownType => AppString.SideBar.UnknownType,
                Scenes.CustomExtension => AppString.SideBar.CustomExtension,
                Scenes.PerceivedType => AppString.SideBar.PerceivedType,
                Scenes.DirectoryType => AppString.SideBar.DirectoryType,
                Scenes.MenuAnalysis => AppString.SideBar.MenuAnalysis,
                Scenes.EnhanceMenu => AppString.SideBar.EnhanceMenu,
                Scenes.DetailedEdit => AppString.SideBar.DetailedEdit,
                Scenes.DragDrop => AppString.SideBar.DragDrop,
                Scenes.PublicReferences => AppString.SideBar.PublicReferences,
                Scenes.InternetExplorer => AppString.SideBar.IEMenu,
                Scenes.CustomRegPath => AppString.SideBar.CustomRegPath,
                _ => null
            } ?? throw new ArgumentException("Unsupported scene for GetSceneName", nameof(scene));
        }

        // 开始进行备份或恢复
        // （新增备份类别处5）
        private void GetBackupItems(LoadingDialogInterface dialogInterface)
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
                    GetShellListItems(dialogInterface); break;
            }
        }
    }
}
