using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.BluePointLilac.Controls;
using ContextMenuManager.Methods;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace ContextMenuManager.Controls
{
    public sealed class ShellList : MyList // 文件类型 Ink文件 uwp Ink exe文件 未知格式 // 主页 第一栏
    {
        public const string MENUPATH_FILE = @"HKEY_CLASSES_ROOT\*";//文件
        public const string MENUPATH_FOLDER = @"HKEY_CLASSES_ROOT\Folder";//文件夹
        public const string MENUPATH_DIRECTORY = @"HKEY_CLASSES_ROOT\Directory";//目录
        public const string MENUPATH_BACKGROUND = @"HKEY_CLASSES_ROOT\Directory\Background";//目录背景
        public const string MENUPATH_DESKTOP = @"HKEY_CLASSES_ROOT\DesktopBackground";//桌面背景
        public const string MENUPATH_DRIVE = @"HKEY_CLASSES_ROOT\Drive";//磁盘分区
        public const string MENUPATH_ALLOBJECTS = @"HKEY_CLASSES_ROOT\AllFilesystemObjects";//所有对象
        public const string MENUPATH_COMPUTER = @"HKEY_CLASSES_ROOT\CLSID\{20D04FE0-3AEA-1069-A2D8-08002B30309D}";//此电脑
        public const string MENUPATH_RECYCLEBIN = @"HKEY_CLASSES_ROOT\CLSID\{645FF040-5081-101B-9F08-00AA002F954E}";//回收站

        public const string MENUPATH_LIBRARY = @"HKEY_CLASSES_ROOT\LibraryFolder";//库
        public const string MENUPATH_LIBRARY_BACKGROUND = @"HKEY_CLASSES_ROOT\LibraryFolder\Background";//库背景
        public const string MENUPATH_LIBRARY_USER = @"HKEY_CLASSES_ROOT\UserLibraryFolder";//用户库

        public const string MENUPATH_UWPLNK = @"HKEY_CLASSES_ROOT\Launcher.ImmersiveApplication";//UWP快捷方式
        public const string MENUPATH_UNKNOWN = @"HKEY_CLASSES_ROOT\Unknown";//未知格式
        public const string SYSFILEASSPATH = @"HKEY_CLASSES_ROOT\SystemFileAssociations";//系统扩展名注册表父项路径
        private const string LASTKEYPATH = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Applets\Regedit";//上次打开的注册表项路径记录

        public event EventHandler ItemsLoaded;
        private CancellationTokenSource cts;

        private struct ShellItemData
        {
            public string RegPath;
            public string Text;
            public Image Image;
            public bool IsMultiItem;
        }

        private static readonly Dictionary<string, int> DefaultNameIndexs
            = new(StringComparer.OrdinalIgnoreCase)
            {
                { "open", 8496 }, { "edit", 8516 }, { "print", 8497 }, { "find", 8503 },
                { "play", 8498 }, { "runas", 8505 }, { "explore", 8502 }, { "preview", 8499 }
            };

        public static readonly List<string> DirectoryTypes = new()
        {
            "Document", "Image", "Video", "Audio"
        };
        public static readonly List<string> PerceivedTypes = new()
        {
            null, "Text", "Document", "Image",
            "Video", "Audio", "Compressed", "System"
        };
        private static readonly string[] PerceivedTypeNames =
        {
            AppString.Dialog.NoPerceivedType, AppString.Dialog.TextFile, AppString.Dialog.DocumentFile, AppString.Dialog.ImageFile,
            AppString.Dialog.VideoFile, AppString.Dialog.AudioFile, AppString.Dialog.CompressedFile, AppString.Dialog.SystemFile
        };
        private static readonly string[] DirectoryTypeNames =
        {
            AppString.Dialog.DocumentDirectory, AppString.Dialog.ImageDirectory,
            AppString.Dialog.VideoDirectory, AppString.Dialog.AudioDirectory
        };

        private static string GetDirectoryTypeName(string directoryType)
        {
            if (directoryType == null) return null;
            var index = DirectoryTypes.FindIndex(type => directoryType.Equals(type, StringComparison.OrdinalIgnoreCase));
            if (index >= 0) return DirectoryTypeNames[index];
            else return null;
        }

        private static string GetPerceivedTypeName(string perceivedType)
        {
            var index = 0;
            if (perceivedType != null) index = PerceivedTypes.FindIndex(type => perceivedType.Equals(type, StringComparison.OrdinalIgnoreCase));
            if (index == -1) index = 0;
            return PerceivedTypeNames[index];
        }

        private static readonly string[] DropEffectPaths =
        {
            MENUPATH_FILE, MENUPATH_ALLOBJECTS,
            MENUPATH_FOLDER, MENUPATH_DIRECTORY
        };
        private static readonly string[] DropEffectNames =
        {
            AppString.Dialog.DefaultDropEffect, AppString.Dialog.CopyDropEffect,
            AppString.Dialog.MoveDropEffect, AppString.Dialog.CreateLinkDropEffect
        };

        public enum DropEffect { Default = 0, Copy = 1, Move = 2, CreateLink = 4 }

        public static DropEffect DefaultDropEffect
        {
            get
            {
                foreach (var path in DropEffectPaths)
                {
                    var value = Registry.GetValue(path, "DefaultDropEffect", null);
                    if (value != null)
                    {
                        switch (value)
                        {
                            case 1:
                                return DropEffect.Copy;
                            case 2:
                                return DropEffect.Move;
                            case 4:
                                return DropEffect.CreateLink;
                        }
                    }
                }
                return DropEffect.Default;
            }
            set
            {
                var data = value switch
                {
                    DropEffect.Copy => 1,
                    DropEffect.Move => 2,
                    DropEffect.CreateLink => 4,
                    _ => (object)0,
                };
                foreach (var path in DropEffectPaths)
                {
                    Registry.SetValue(path, "DefaultDropEffect", data, RegistryValueKind.DWord);
                }
            }
        }

        private static string GetDropEffectName()
        {
            return DefaultDropEffect switch
            {
                DropEffect.Copy => DropEffectNames[1],
                DropEffect.Move => DropEffectNames[2],
                DropEffect.CreateLink => DropEffectNames[3],
                _ => DropEffectNames[0],
            };
        }

        private static string CurrentExtension = null;
        private static string CurrentDirectoryType = null;
        private static string CurrentPerceivedType = null;
        public static string CurrentCustomRegPath = null;
        public static string CurrentFileObjectPath = null;

        private static string CurrentExtensionPerceivedType
        {
            get => GetPerceivedType(CurrentExtension);
            set
            {
                var path = $@"{RegistryEx.CLASSES_ROOT}\{CurrentExtension}";
                if (value == null) RegistryEx.DeleteValue(path, "PerceivedType");
                else Registry.SetValue(path, "PerceivedType", value, RegistryValueKind.String);
            }
        }

        public static string GetShellPath(string scenePath)
        {
            return $@"{scenePath}\shell";
        }

        public static string GetShellExPath(string scenePath)
        {
            return $@"{scenePath}\ShellEx";
        }

        public static string GetSysAssExtPath(string typeName)
        {
            return typeName != null ? $@"{SYSFILEASSPATH}\{typeName}" : null;
        }

        private static string GetOpenMode(string extension)
        {
            return FileExtension.GetOpenMode(extension);
        }

        public static string GetOpenModePath(string extension)
        {
            return extension != null ? $@"{RegistryEx.CLASSES_ROOT}\{GetOpenMode(extension)}" : null;
        }

        private static string GetPerceivedType(string extension)
        {
            return Registry.GetValue($@"{RegistryEx.CLASSES_ROOT}\{extension}", "PerceivedType", null)?.ToString();
        }

        public Scenes Scene { get; set; }

        public void LoadItems()
        {
            string scenePath = null;
            switch (Scene)
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
                case Scenes.CustomExtension:
                    var isLnk = CurrentExtension?.ToLower() == ".lnk";
                    if (isLnk) scenePath = GetOpenModePath(".lnk");
                    else scenePath = GetSysAssExtPath(CurrentExtension);
                    break;
                case Scenes.PerceivedType:
                    scenePath = GetSysAssExtPath(CurrentPerceivedType); break;
                case Scenes.DirectoryType:
                    if (CurrentDirectoryType == null) scenePath = null;
                    else scenePath = GetSysAssExtPath($"Directory.{CurrentDirectoryType}"); break;
                case Scenes.MenuAnalysis:
                    AddItem(new SelectItem(Scene));
                    LoadAnalysisItems();
                    return;
                case Scenes.DragDrop:
                    AddItem(new SelectItem(Scene));
                    AddNewItem(MENUPATH_FOLDER);
                    LoadShellExItems(GetShellExPath(MENUPATH_FOLDER));
                    LoadShellExItems(GetShellExPath(MENUPATH_DIRECTORY));
                    LoadShellExItems(GetShellExPath(MENUPATH_DRIVE));
                    LoadShellExItems(GetShellExPath(MENUPATH_ALLOBJECTS));
                    return;
                case Scenes.PublicReferences:
                    //Vista系统没有这一项
                    if (WinOsVersion.Current == WinOsVersion.Vista) return;
                    AddNewItem(RegistryEx.GetParentPath(ShellItem.CommandStorePath));
                    LoadStoreItems();
                    return;
                case Scenes.CustomRegPath:
                    scenePath = CurrentCustomRegPath; break;
            }
            //Win11系统切换旧版菜单
            if (WinOsVersion.Current >= WinOsVersion.Win11)
            {
                switch (Scene)
                {
                    case Scenes.File:
                    case Scenes.Folder:
                    case Scenes.Directory:
                    case Scenes.Background:
                    case Scenes.Desktop:
                    case Scenes.Drive:
                    case Scenes.AllObjects:
                    case Scenes.Computer:
                    case Scenes.RecycleBin:
                    case Scenes.Library:
                        AddItem(new SwitchContextMenuStyleItem()); break;
                }
            }
            AddNewItem(scenePath); // 新建一个菜单项目
            LoadItems(scenePath);
            if (WinOsVersion.Current >= WinOsVersion.Win10)
            {
                LoadUwpModeItem();
            }
            switch (Scene)
            {
                case Scenes.Background:
                    var item = new VisibleRegRuleItem(VisibleRegRuleItem.CustomFolder);
                    AddItem(item);
                    break;
                case Scenes.Computer:
                    item = new VisibleRegRuleItem(VisibleRegRuleItem.NetworkDrive);
                    AddItem(item);
                    break;
                case Scenes.RecycleBin:
                    item = new VisibleRegRuleItem(VisibleRegRuleItem.RecycleBinProperties);
                    AddItem(item);
                    break;
                case Scenes.Library:
                    LoadItems(MENUPATH_LIBRARY_BACKGROUND);
                    LoadItems(MENUPATH_LIBRARY_USER);
                    break;
                case Scenes.ExeFile:
                    LoadItems(GetOpenModePath(".exe"));
                    break;
                case Scenes.CustomExtension:
                case Scenes.PerceivedType:
                case Scenes.DirectoryType:
                case Scenes.CustomRegPath:
                    InsertItem(new SelectItem(Scene), 0); // 请选择一个文件扩展名项目
                    // 自选文件扩展名后加载对应的右键菜单
                    if (Scene == Scenes.CustomExtension && CurrentExtension != null)
                    {
                        LoadItems(GetOpenModePath(CurrentExtension));
                        InsertItem(new SelectItem(Scenes.CustomExtensionPerceivedType), 1);
                    }
                    break;
            }
        }

        private void LoadItems(string scenePath)
        {
            if (scenePath == null) return;
            
            // Cancel previous task
            cts?.Cancel();
            cts = new CancellationTokenSource();
            var token = cts.Token;

            // Start async loading
            Task.Run(() =>
            {
                if (token.IsCancellationRequested) return;

                RegTrustedInstaller.TakeRegKeyOwnerShip(scenePath);
                
                var shellPath = GetShellPath(scenePath);
                var shellItemsData = GetShellItemsData(shellPath);

                if (token.IsCancellationRequested) return;

                Invoke(new Action(() =>
                {
                    if (token.IsCancellationRequested) return;

                    // Add ShellItems from preloaded data
                    foreach (var data in shellItemsData)
                    {
                        AddItem(new ShellItem(data.RegPath, data.Text, data.Image, data.IsMultiItem));
                    }

                    // Continue with ShellEx items (sync for now, to keep it simple)
                    LoadShellExItems(GetShellExPath(scenePath));
                    
                    ItemsLoaded?.Invoke(this, EventArgs.Empty);
                }));
            }, token);
        }

        private List<ShellItemData> GetShellItemsData(string shellPath)
        {
            var list = new List<ShellItemData>();
            using var shellKey = RegistryEx.GetRegistryKey(shellPath);
            if (shellKey == null) return list;
            
            // RegTrustedInstaller.TakeRegTreeOwnerShip(shellKey.Name); // Assuming this is fast enough or handled

            foreach (var keyName in shellKey.GetSubKeyNames())
            {
                var regPath = $@"{shellPath}\{keyName}";
                var isMultiItem = GetIsMultiItem(regPath);
                var text = GetItemText(regPath, keyName, isMultiItem);
                var image = GetItemIcon(regPath);
                list.Add(new ShellItemData { RegPath = regPath, Text = text, Image = image, IsMultiItem = isMultiItem });
            }
            return list;
        }

        // Helper methods copied from ShellItem logic
        private bool GetIsMultiItem(string regPath)
        {
            var value = Registry.GetValue(regPath, "SubCommands", null);
            if (value != null) return true;
            value = Registry.GetValue(regPath, "ExtendedSubCommandsKey", null);
            if (!string.IsNullOrEmpty(value?.ToString())) return true;
            return false;
        }

        private string GetItemText(string regPath, string keyName, bool isMultiItem)
        {
            string name;
            var valueNames = new List<string> { "MUIVerb" };
            if (!isMultiItem) valueNames.Add("");
            foreach (var valueName in valueNames)
            {
                name = Registry.GetValue(regPath, valueName, null)?.ToString();
                name = ResourceString.GetDirectString(name);
                if (!string.IsNullOrEmpty(name)) return name;
            }
            if (DefaultNameIndexs.TryGetValue(RegistryEx.GetKeyName(regPath), out var index))
            {
                name = $"@windows.storage.dll,-{index}";
                name = ResourceString.GetDirectString(name);
                if (!string.IsNullOrEmpty(name)) return name;
            }
            return RegistryEx.GetKeyName(regPath);
        }

        private Image GetItemIcon(string regPath)
        {
            // Logic similar to ShellItem.ItemIcon but returns Image
            var iconLocation = Registry.GetValue(regPath, "Icon", null)?.ToString();
            var hasLUAShield = Registry.GetValue(regPath, "HasLUAShield", null) != null;
            
            // We need ItemFilePath which is derived from Guid or Command
            // ShellItem.ItemFilePath => GuidInfo.GetFilePath(Guid) ?? ObjectPath.ExtractFilePath(ItemCommand);
            // This is complex to replicate exactly without creating ShellItem instance.
            // But we can approximate.
            
            // Replicating Guid logic
            string commandPath = $@"{regPath}\command";
            var keyValues = new Dictionary<string, string>
            {
                { commandPath , "DelegateExecute" },
                { $@"{regPath}\DropTarget" , "CLSID" },
                { regPath , "ExplorerCommandHandler" },
            };
            Guid guid = Guid.Empty;
            foreach (var item in keyValues)
            {
                var val = Registry.GetValue(item.Key, item.Value, null)?.ToString();
                if (GuidEx.TryParse(val, out var g)) { guid = g; break; }
            }
            
            string itemCommand = null;
            if (!isMultiItem(regPath)) // Helper needed
                itemCommand = Registry.GetValue(commandPath, "", null)?.ToString();
                
            string itemFilePath = GuidInfo.GetFilePath(guid) ?? ObjectPath.ExtractFilePath(itemCommand);

            Icon icon;
            string iconPath;
            int iconIndex;
            
            if (iconLocation != null)
            {
                icon = ResourceIcon.GetIcon(iconLocation, out iconPath, out iconIndex);
                if (icon == null && Path.GetExtension(iconPath)?.ToLower() == ".exe")
                    icon = ResourceIcon.GetIcon(iconPath = "imageres.dll", iconIndex = -15);
            }
            else if (hasLUAShield)
                icon = ResourceIcon.GetIcon(iconPath = "imageres.dll", iconIndex = -78);
            else 
                icon = ResourceIcon.GetIcon(iconPath = itemFilePath, iconIndex = 0);
                
            if (icon == null) 
                icon = ResourceIcon.GetExtensionIcon(itemFilePath) ?? ResourceIcon.GetIcon(iconPath = "imageres.dll", iconIndex = -2);
                
            Image image = icon.ToBitmap();
            if (iconLocation == null && !hasLUAShield)
            {
                 // ToTransparent logic if no icon/shield
                 // ShellItem: if (!HasIcon) Image = Image.ToTransparent();
                 // HasIcon = !IconLocation.IsNullOrWhiteSpace() || HasLUAShield
                 image = image.ToTransparent();
            }
            return image;
        }

        private bool isMultiItem(string regPath) // helper
        {
             return GetIsMultiItem(regPath);
        }

        // Original LoadShellItems removed or replaced
        // private void LoadShellItems(string shellPath) { ... } // Deleted

        private void LoadShellExItems(string shellExPath)
        {
            var names = new List<string>();
            using var shellExKey = RegistryEx.GetRegistryKey(shellExPath);
            if (shellExKey == null) return;
            var isDragDrop = Scene == Scenes.DragDrop;
            RegTrustedInstaller.TakeRegTreeOwnerShip(shellExKey.Name);
            var dic = ShellExItem.GetPathAndGuids(shellExPath, isDragDrop);
            FoldGroupItem groupItem = null;
            if (isDragDrop)
            {
                groupItem = GetDragDropGroupItem(shellExPath);
                AddItem(groupItem);
            }
            foreach (var path in dic.Keys)
            {
                var keyName = RegistryEx.GetKeyName(path);
                if (!names.Contains(keyName))
                {
                    var item = new ShellExItem(dic[path], path);
                    if (groupItem != null)
                    {
                        item.FoldGroupItem = groupItem;
                        item.Indent();
                    }
                    AddItem(item);
                    names.Add(keyName);
                }
            }
            groupItem?.SetVisibleWithSubItemCount();
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

        private void LoadStoreItems()
        {
            using var shellKey = RegistryEx.GetRegistryKey(ShellItem.CommandStorePath);
            foreach (var itemName in shellKey.GetSubKeyNames())
            {
                if (AppConfig.HideSysStoreItems && itemName.StartsWith("Windows.", StringComparison.OrdinalIgnoreCase)) continue;
                AddItem(new StoreShellItem($@"{ShellItem.CommandStorePath}\{itemName}", true, false));
            }
        }

        private void LoadUwpModeItem()
        {
            foreach (var doc in XmlDicHelper.UwpModeItemsDic)
            {
                if (doc?.DocumentElement == null) continue;
                foreach (XmlNode sceneXN in doc.DocumentElement.ChildNodes)
                {
                    if (sceneXN.Name == Scene.ToString())
                    {
                        foreach (XmlElement itemXE in sceneXN.ChildNodes)
                        {
                            if (GuidEx.TryParse(itemXE.GetAttribute("Guid"), out var guid))
                            {
                                var isAdded = false;
                                foreach (Control ctr in Controls)
                                {
                                    if (ctr is UwpModeItem item && item.Guid == guid) { isAdded = true; break; }
                                }
                                if (isAdded) continue;
                                if (GuidInfo.GetFilePath(guid) == null) continue;
                                var uwpName = GuidInfo.GetUwpName(guid);
                                var uwpItem = new UwpModeItem(uwpName, guid);
                                AddItem(uwpItem);
                            }
                        }
                    }
                }
            }
        }

        private void LoadAnalysisItems()
        {
            if (CurrentFileObjectPath == null) return;

            void AddFileItems(string filePath)
            {
                var extension = Path.GetExtension(filePath).ToLower();
                if (extension == string.Empty) extension = ".";
                var perceivedType = GetPerceivedType(extension);
                var perceivedTypeName = GetPerceivedTypeName(perceivedType);
                JumpItem.TargetPath = filePath;
                JumpItem.Extension = extension;
                JumpItem.PerceivedType = perceivedType;
                AddItem(new JumpItem(Scenes.File));
                AddItem(new JumpItem(Scenes.AllObjects));
                if (extension == ".exe") AddItem(new JumpItem(Scenes.ExeFile));
                else AddItem(new JumpItem(Scenes.CustomExtension));
                if (GetOpenMode(extension) == null) AddItem(new JumpItem(Scenes.UnknownType));
                if (perceivedType != null) AddItem(new JumpItem(Scenes.PerceivedType));
            }

            void AddDirItems(string dirPath)
            {
                if (!dirPath.EndsWith(":\\"))
                {
                    AddItem(new JumpItem(Scenes.Folder));
                    AddItem(new JumpItem(Scenes.Directory));
                    AddItem(new JumpItem(Scenes.AllObjects));
                    AddItem(new JumpItem(Scenes.DirectoryType));
                }
                else
                {
                    AddItem(new JumpItem(Scenes.Folder));
                    AddItem(new JumpItem(Scenes.Drive));
                }
            }

            if (File.Exists(CurrentFileObjectPath))
            {
                var extension = Path.GetExtension(CurrentFileObjectPath).ToLower();
                if (extension == ".lnk")
                {
                    using (var shellLink = new ShellLink(CurrentFileObjectPath))
                    {
                        var targetPath = shellLink.TargetPath;
                        if (File.Exists(targetPath)) AddFileItems(targetPath);
                        else if (Directory.Exists(targetPath)) AddDirItems(targetPath);
                    }
                    AddItem(new JumpItem(Scenes.LnkFile));
                }
                else AddFileItems(CurrentFileObjectPath);
            }
            else if (Directory.Exists(CurrentFileObjectPath)) AddDirItems(CurrentFileObjectPath);
        }

        public class SelectItem : MyListItem
        {
            public SelectItem(Scenes scene)
            {
                Scene = scene;
                AddCtr(BtnSelect);
                SetTextAndTip();
                SetImage();
                BtnSelect.MouseDown += (sender, e) => ShowSelectDialog();
                MouseDoubleClick += (sender, e) => ShowSelectDialog();
            }

            private readonly PictureButton BtnSelect = new(AppImage.Select);

            public Scenes Scene { get; private set; }
            public string SelectedPath { get; set; }

            private void SetTextAndTip()
            {
                var tip = "";
                var text = "";
                switch (Scene)
                {
                    case Scenes.CustomExtension:
                        tip = AppString.Dialog.SelectExtension;
                        if (CurrentExtension == null) text = tip;
                        else text = AppString.Other.CurrentExtension.Replace("%s", CurrentExtension);
                        break;
                    case Scenes.PerceivedType:
                        tip = AppString.Dialog.SelectPerceivedType;
                        if (CurrentPerceivedType == null) text = tip;
                        else text = AppString.Other.CurrentPerceivedType.Replace("%s", GetPerceivedTypeName(CurrentPerceivedType));
                        break;
                    case Scenes.DirectoryType:
                        tip = AppString.Dialog.SelectDirectoryType;
                        if (CurrentDirectoryType == null) text = tip;
                        else text = AppString.Other.CurrentDirectoryType.Replace("%s", GetDirectoryTypeName(CurrentDirectoryType));
                        break;
                    case Scenes.CustomRegPath:
                        SelectedPath = CurrentCustomRegPath;
                        tip = AppString.Other.SelectRegPath;
                        if (SelectedPath == null) text = tip;
                        else text = AppString.Other.CurrentRegPath + "\n" + SelectedPath;
                        break;
                    case Scenes.MenuAnalysis:
                        SelectedPath = CurrentFileObjectPath;
                        tip = AppString.Tip.DropOrSelectObject;
                        if (SelectedPath == null) text = tip;
                        else text = AppString.Other.CurrentFilePath + "\n" + SelectedPath;
                        break;
                    case Scenes.DragDrop:
                        SelectedPath = GetDropEffectName();
                        tip = AppString.Dialog.SelectDropEffect;
                        text = AppString.Other.SetDefaultDropEffect + " " + SelectedPath;
                        break;
                    case Scenes.CustomExtensionPerceivedType:
                        tip = AppString.Dialog.SelectPerceivedType;
                        text = AppString.Other.SetPerceivedType.Replace("%s", CurrentExtension) + " " + GetPerceivedTypeName(CurrentExtensionPerceivedType);
                        break;
                }
                ToolTipBox.SetToolTip(BtnSelect, tip);
                Text = text;
            }

            private void SetImage()
            {
                switch (Scene)
                {
                    case Scenes.CustomExtensionPerceivedType:
                        using (var icon = ResourceIcon.GetExtensionIcon(CurrentExtension))
                            Image = icon?.ToBitmap();
                        break;
                }
                if (Image == null) Image = AppImage.Custom;
            }

            private void ShowSelectDialog()
            {
                SelectDialog dlg = null;
                switch (Scene)
                {
                    case Scenes.CustomExtension:
                        dlg = new FileExtensionDialog
                        {
                            Selected = CurrentExtension?[1..]
                        };
                        break;
                    case Scenes.PerceivedType:
                        dlg = new SelectDialog
                        {
                            Items = PerceivedTypeNames,
                            Title = AppString.Dialog.SelectPerceivedType,
                            Selected = GetPerceivedTypeName(CurrentPerceivedType)
                        };
                        break;
                    case Scenes.DirectoryType:
                        dlg = new SelectDialog
                        {
                            Items = DirectoryTypeNames,
                            Title = AppString.Dialog.SelectDirectoryType,
                            Selected = GetDirectoryTypeName(CurrentDirectoryType)
                        };
                        break;
                    case Scenes.CustomExtensionPerceivedType:
                        dlg = new SelectDialog
                        {
                            Items = PerceivedTypeNames,
                            Title = AppString.Dialog.SelectPerceivedType,
                            Selected = GetPerceivedTypeName(CurrentExtensionPerceivedType)
                        };
                        break;
                    case Scenes.DragDrop:
                        dlg = new SelectDialog
                        {
                            Items = DropEffectNames,
                            Title = AppString.Dialog.SelectDropEffect,
                            Selected = GetDropEffectName()
                        };
                        break;
                    case Scenes.MenuAnalysis:
                        dlg = new SelectDialog
                        {
                            Items = new[] { AppString.SideBar.File, AppString.SideBar.Directory },
                            Title = AppString.Dialog.SelectObjectType,
                        };
                        break;
                    case Scenes.CustomRegPath:
                        if (AppMessageBox.Show(AppString.Message.SelectRegPath,
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                        var frm = FindForm();
                        frm.Hide();
                        using (var process = Process.Start("regedit.exe", "-m"))
                        {
                            process.WaitForExit();
                        }
                        var path = Registry.GetValue(LASTKEYPATH, "LastKey", "").ToString();
                        var index = path.IndexOf('\\');
                        if (index == -1) return;
                        path = path[(index + 1)..];
                        CurrentCustomRegPath = path;
                        RefreshList();
                        frm.Show();
                        frm.Activate();
                        break;
                }
                switch (Scene)
                {
                    case Scenes.CustomExtension:
                    case Scenes.PerceivedType:
                    case Scenes.DirectoryType:
                    case Scenes.MenuAnalysis:
                    case Scenes.DragDrop:
                    case Scenes.CustomExtensionPerceivedType:
                        if (dlg.ShowDialog() != DialogResult.OK) return;
                        break;
                }
                switch (Scene)
                {
                    case Scenes.CustomExtension:
                        CurrentExtension = dlg.Selected;
                        RefreshList();
                        break;
                    case Scenes.PerceivedType:
                        CurrentPerceivedType = PerceivedTypes[dlg.SelectedIndex];
                        RefreshList();
                        break;
                    case Scenes.DirectoryType:
                        CurrentDirectoryType = DirectoryTypes[dlg.SelectedIndex];
                        RefreshList();
                        break;
                    case Scenes.CustomExtensionPerceivedType:
                        var selected = PerceivedTypes[dlg.SelectedIndex];
                        CurrentExtensionPerceivedType = selected;
                        Text = AppString.Other.SetPerceivedType.Replace("%s", CurrentExtension) + " " + GetPerceivedTypeName(selected);
                        break;
                    case Scenes.DragDrop:
                        switch (dlg.SelectedIndex)
                        {
                            case 0: DefaultDropEffect = DropEffect.Default; break;
                            case 1: DefaultDropEffect = DropEffect.Copy; break;
                            case 2: DefaultDropEffect = DropEffect.Move; break;
                            case 3: DefaultDropEffect = DropEffect.CreateLink; break;
                        }
                        Text = AppString.Other.SetDefaultDropEffect + " " + GetDropEffectName();
                        break;
                    case Scenes.MenuAnalysis:
                        if (dlg.SelectedIndex == 0)
                        {
                            using var dlg1 = new System.Windows.Forms.OpenFileDialog();
                            dlg1.DereferenceLinks = false;
                            if (dlg1.ShowDialog() != DialogResult.OK) return;
                            CurrentFileObjectPath = dlg1.FileName;
                        }
                        else
                        {
                            using var dlg2 = new FolderBrowserDialog();
                            if (dlg2.ShowDialog() != DialogResult.OK) return;
                            CurrentFileObjectPath = dlg2.SelectedPath;
                        }
                        RefreshList();
                        break;
                }
            }

            private void RefreshList()
            {
                var list = (ShellList)Parent;
                list.ClearItems();
                list.LoadItems();
            }
        }

        private sealed class JumpItem : MyListItem
        {
            public JumpItem(Scenes scene)
            {
                AddCtr(btnJump);
                Image image = null;
                var index1 = 0;
                var index2 = 0;
                string[] txts = null;
                switch (scene)
                {
                    case Scenes.File:
                        txts = new[] { AppString.ToolBar.Home, AppString.SideBar.File };
                        image = AppImage.File;
                        break;
                    case Scenes.Folder:
                        txts = new[] { AppString.ToolBar.Home, AppString.SideBar.Folder };
                        image = AppImage.Folder;
                        index2 = 1;
                        break;
                    case Scenes.Directory:
                        txts = new[] { AppString.ToolBar.Home, AppString.SideBar.Directory };
                        image = AppImage.Directory;
                        index2 = 2;
                        break;
                    case Scenes.Drive:
                        txts = new[] { AppString.ToolBar.Home, AppString.SideBar.Drive };
                        image = AppImage.Drive;
                        index2 = 5;
                        break;
                    case Scenes.AllObjects:
                        txts = new[] { AppString.ToolBar.Home, AppString.SideBar.AllObjects };
                        image = AppImage.AllObjects;
                        index2 = 6;
                        break;
                    case Scenes.LnkFile:
                        txts = new[] { AppString.ToolBar.Type, AppString.SideBar.LnkFile };
                        image = AppImage.LnkFile;
                        index1 = 1;
                        index2 = 0; //MainForm.TypeShellScenes
                        break;
                    case Scenes.ExeFile:
                        txts = new[] { AppString.ToolBar.Type, AppString.SideBar.ExeFile };
                        using (var icon = ResourceIcon.GetExtensionIcon(TargetPath)) image = icon.ToBitmap();
                        index1 = 1;
                        index2 = 2; //MainForm.TypeShellScenes
                        break;
                    case Scenes.UnknownType:
                        txts = new[] { AppString.ToolBar.Type, AppString.SideBar.UnknownType };
                        image = AppImage.NotFound;
                        index1 = 1;
                        index2 = 8; //MainForm.TypeShellScenes
                        break;
                    case Scenes.CustomExtension:
                        txts = new[] { AppString.ToolBar.Type, AppString.SideBar.CustomExtension, Extension };
                        using (var icon = ResourceIcon.GetExtensionIcon(Extension)) image = icon.ToBitmap();
                        index1 = 1;
                        index2 = 5; //MainForm.TypeShellScenes
                        break;
                    case Scenes.PerceivedType:
                        txts = new[] { AppString.ToolBar.Type, AppString.SideBar.PerceivedType, GetPerceivedTypeName(PerceivedType) };
                        image = AppImage.File;
                        index1 = 1;
                        index2 = 6; //MainForm.TypeShellScenes
                        break;
                    case Scenes.DirectoryType:
                        txts = new[] { AppString.ToolBar.Type, AppString.SideBar.DirectoryType };
                        image = AppImage.Directory;
                        index1 = 1;
                        index2 = 7; //MainForm.TypeShellScenes
                        break;
                }
                Text = "[ " + string.Join(" ]  ▶  [ ", txts) + " ]";
                Image = image;
                void SwitchTab()
                {
                    switch (scene)
                    {
                        case Scenes.CustomExtension:
                            CurrentExtension = Extension; break;
                        case Scenes.PerceivedType:
                            CurrentPerceivedType = PerceivedType; break;
                    }
                    ((MainForm)FindForm()).JumpItem(index1, index2);//
                }
                ;
                btnJump.MouseDown += (sender, e) => SwitchTab();
                DoubleClick += (sender, e) => SwitchTab();
            }

            private readonly PictureButton btnJump = new(AppImage.Jump);

            public static string Extension = null;
            public static string PerceivedType = null;
            public static string TargetPath = null;
        }

        private void AddNewItem(string scenePath)
        {
            if (scenePath == null) return;
            var shellPath = GetShellPath(scenePath);
            var newItem = new NewItem();
            var btnAddExisting = new PictureButton(AppImage.AddExisting);
            var btnEnhanceMenu = new PictureButton(AppImage.Enhance);
            ToolTipBox.SetToolTip(btnAddExisting, AppString.Tip.AddFromPublic);
            ToolTipBox.SetToolTip(btnEnhanceMenu, AppString.StatusBar.EnhanceMenu);
            if (Scene == Scenes.DragDrop || ShellItem.CommandStorePath.Equals(shellPath,
                StringComparison.OrdinalIgnoreCase)) btnAddExisting.Visible = false;
            else
            {
                using var key = RegistryEx.GetRegistryKey(ShellItem.CommandStorePath);
                var subKeyNames = key.GetSubKeyNames().ToList();
                if (AppConfig.HideSysStoreItems) subKeyNames.RemoveAll(name => name.StartsWith("Windows.", StringComparison.OrdinalIgnoreCase));
                if (subKeyNames.Count == 0) btnAddExisting.Visible = false;
            }
            if (!XmlDicHelper.EnhanceMenuPathDic.ContainsKey(scenePath)) btnEnhanceMenu.Visible = false;
            newItem.AddCtrs(new[] { btnAddExisting, btnEnhanceMenu });
            AddItem(newItem);

            newItem.AddNewItem += () =>
            {
                bool isShell;
                if (Scene == Scenes.PublicReferences) isShell = true;
                else if (Scene == Scenes.DragDrop) isShell = false;
                else
                {
                    using var dlg = new SelectDialog();
                    dlg.Items = new[] { "Shell", "ShellEx" };
                    dlg.Title = AppString.Dialog.SelectNewItemType;
                    if (dlg.ShowDialog() != DialogResult.OK) return;
                    isShell = dlg.SelectedIndex == 0;
                }
                if (isShell) AddNewShellItem(scenePath);
                else AddNewShellExItem(scenePath);
            };

            btnAddExisting.MouseDown += (sender, e) =>
            {
                using var dlg = new ShellStoreDialog();
                dlg.IsReference = false;
                dlg.ShellPath = ShellItem.CommandStorePath;
                dlg.Filter = new Func<string, bool>(itemName => !(AppConfig.HideSysStoreItems
                    && itemName.StartsWith("Windows.", StringComparison.OrdinalIgnoreCase)));
                if (dlg.ShowDialog() != DialogResult.OK) return;
                foreach (var keyName in dlg.SelectedKeyNames)
                {
                    var srcPath = $@"{dlg.ShellPath}\{keyName}";
                    var dstPath = ObjectPath.GetNewPathWithIndex($@"{shellPath}\{keyName}", ObjectPath.PathType.Registry);

                    RegistryEx.CopyTo(srcPath, dstPath);
                    AddItem(new ShellItem(dstPath));
                }
            };

            btnEnhanceMenu.MouseDown += (sender, e) =>
            {
                var tempPath1 = Path.GetTempFileName();
                var tempPath2 = Path.GetTempFileName();
                ExternalProgram.ExportRegistry(scenePath, tempPath1);
                using (var dlg = new EnhanceMenusDialog())
                {
                    dlg.ScenePath = scenePath;
                    dlg.ShowDialog();
                }
                ExternalProgram.ExportRegistry(scenePath, tempPath2);
                var str1 = File.ReadAllText(tempPath1);
                var str2 = File.ReadAllText(tempPath2);
                File.Delete(tempPath1);
                File.Delete(tempPath2);
                if (!str1.Equals(str2))
                {
                    var mainForm = (MainForm)FindForm();
                    mainForm.JumpItem(mainForm.ToolBar.SelectedIndex, mainForm.SideBar.SelectedIndex);
                }
            };
        }

        private void AddNewShellItem(string scenePath)
        {
            var shellPath = GetShellPath(scenePath);
            using var dlg = new NewShellDialog();
            dlg.ScenePath = scenePath;
            dlg.ShellPath = shellPath;
            if (dlg.ShowDialog() != DialogResult.OK) return;
            for (var i = 0; i < Controls.Count; i++)
            {
                if (Controls[i] is NewItem)
                {
                    ShellItem item;
                    if (Scene != Scenes.PublicReferences) item = new ShellItem(dlg.NewItemRegPath);
                    else item = new StoreShellItem(dlg.NewItemRegPath, true, false);
                    InsertItem(item, i + 1);
                    break;
                }
            }
        }

        private void AddNewShellExItem(string scenePath)
        {
            var isDragDrop = Scene == Scenes.DragDrop;
            using var dlg1 = new InputDialog { Title = AppString.Dialog.InputGuid };
            if (GuidEx.TryParse(Clipboard.GetText(), out var guid)) dlg1.Text = guid.ToString();
            if (dlg1.ShowDialog() != DialogResult.OK) return;
            if (GuidEx.TryParse(dlg1.Text, out guid))
            {
                if (isDragDrop)
                {
                    using var dlg2 = new SelectDialog();
                    dlg2.Title = AppString.Dialog.SelectGroup;
                    dlg2.Items = new[] { AppString.SideBar.Folder, AppString.SideBar.Directory,
                                        AppString.SideBar.Drive, AppString.SideBar.AllObjects };
                    if (dlg2.ShowDialog() != DialogResult.OK) return;
                    switch (dlg2.SelectedIndex)
                    {
                        case 0:
                            scenePath = MENUPATH_FOLDER; break;
                        case 1:
                            scenePath = MENUPATH_DIRECTORY; break;
                        case 2:
                            scenePath = MENUPATH_DRIVE; break;
                        case 3:
                            scenePath = MENUPATH_ALLOBJECTS; break;
                    }
                }
                var shellExPath = GetShellExPath(scenePath);
                if (ShellExItem.GetPathAndGuids(shellExPath, isDragDrop).Values.Contains(guid))
                {
                    AppMessageBox.Show(AppString.Message.HasBeenAdded);
                }
                else
                {
                    var part = isDragDrop ? ShellExItem.DdhParts[0] : ShellExItem.CmhParts[0];
                    var regPath = $@"{shellExPath}\{part}\{guid:B}";
                    Registry.SetValue(regPath, "", guid.ToString("B"));
                    var item = new ShellExItem(guid, regPath);
                    for (var i = 0; i < Controls.Count; i++)
                    {
                        if (isDragDrop)
                        {
                            if (Controls[i] is FoldGroupItem groupItem)
                            {
                                if (groupItem.GroupPath.Equals(shellExPath, StringComparison.OrdinalIgnoreCase))
                                {
                                    InsertItem(item, i + 1);
                                    item.FoldGroupItem = groupItem;
                                    groupItem.SetVisibleWithSubItemCount();
                                    item.Visible = !groupItem.IsFold;
                                    item.Indent();
                                    break;
                                }
                            }
                        }
                        else
                        {
                            if (Controls[i] is NewItem)
                            {
                                InsertItem(item, i + 1);
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                AppMessageBox.Show(AppString.Message.MalformedGuid);
            }
        }

        private sealed class SwitchContextMenuStyleItem : MyListItem
        {
            private const string registryKeyPath = @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}";
            private const string registrySubKeyPath = @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32";

            public SwitchContextMenuStyleItem()
            {
                Text = AppString.Menu.SwitchUserContextMenuStyle;
                Image = AppImage.ContextMenuStyle;
                AddCtr(cmbDic);
                cmbDic.AutosizeDropDownWidth();
                cmbDic.Font = new Font(Font.FontFamily, Font.Size + 1F);
                cmbDic.Items.AddRange(new[] { AppString.Menu.Win11DefaultContextMenuStyle, AppString.Menu.Win10ClassicContextMenuStyle });
                cmbDic.SelectionChangeCommitted += (sender, e) =>
                {
                    Focus();
                    UseWin11ContextMenuStyle = cmbDic.SelectedIndex == 0;
                };
                // 判断注册表中是否存在registryKeyPath项：存在则为Win10经典右键菜单，不存在则为Win11默认右键菜单
                var registryKey = Registry.CurrentUser.OpenSubKey(registryKeyPath);
                useWin11ContextMenuStyle = registryKey == null;
                cmbDic.SelectedIndex = useWin11ContextMenuStyle ? 0 : 1;
            }

            private bool useWin11ContextMenuStyle;
            public bool UseWin11ContextMenuStyle
            {
                get => useWin11ContextMenuStyle;
                set
                {
                    if (useWin11ContextMenuStyle == value) return;
                    cmbDic.SelectedIndex = value ? 0 : 1;
                    useWin11ContextMenuStyle = value;
                    if (value)
                    {
                        // 切换Win11默认右键菜单样式
                        // 删除注册表项目：HKEY_CURRENT_USER\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}
                        var registryKey = Registry.CurrentUser.OpenSubKey(registryKeyPath, true);
                        if (registryKey != null)
                        {
                            Registry.CurrentUser.DeleteSubKeyTree(registryKeyPath);
                        }
                    }
                    else
                    {
                        // 切换Win10经典右键菜单样式
                        // 添加注册表项目：HKEY_CURRENT_USER\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32
                        var registryKey = Registry.CurrentUser.CreateSubKey(registrySubKeyPath);
                        registryKey?.SetValue(null, "", RegistryValueKind.String);
                    }
                    ExplorerRestarter.Show();
                }
            }

            private readonly RComboBox cmbDic = new()
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 180.DpiZoom()
            };
        }
    }
}
