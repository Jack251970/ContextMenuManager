using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static ContextMenuManager.Controls.ShellNewList;

namespace ContextMenuManager.Methods
{
    internal sealed partial class BackupHelper
    {
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
            using var ieKey = GetRegistryKeySafe(IEList.IEPath);
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
    }
}
