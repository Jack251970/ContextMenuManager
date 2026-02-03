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
    public sealed class ShellList : MyList
    {
        public const string MENUPATH_FILE = @"HKEY_CLASSES_ROOT\*";
        public const string MENUPATH_FOLDER = @"HKEY_CLASSES_ROOT\Folder";
        public const string MENUPATH_DIRECTORY = @"HKEY_CLASSES_ROOT\Directory";
        public const string MENUPATH_BACKGROUND = @"HKEY_CLASSES_ROOT\Directory\Background";
        public const string MENUPATH_DESKTOP = @"HKEY_CLASSES_ROOT\DesktopBackground";
        public const string MENUPATH_DRIVE = @"HKEY_CLASSES_ROOT\Drive";
        public const string MENUPATH_ALLOBJECTS = @"HKEY_CLASSES_ROOT\AllFilesystemObjects";
        public const string MENUPATH_COMPUTER = @"HKEY_CLASSES_ROOT\CLSID\{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
        public const string MENUPATH_RECYCLEBIN = @"HKEY_CLASSES_ROOT\CLSID\{645FF040-5081-101B-9F08-00AA002F954E}";
        public const string MENUPATH_LIBRARY = @"HKEY_CLASSES_ROOT\LibraryFolder";
        public const string MENUPATH_LIBRARY_BACKGROUND = @"HKEY_CLASSES_ROOT\LibraryFolder\Background";
        public const string MENUPATH_LIBRARY_USER = @"HKEY_CLASSES_ROOT\UserLibraryFolder";
        public const string MENUPATH_UWPLNK = @"HKEY_CLASSES_ROOT\Launcher.ImmersiveApplication";
        public const string MENUPATH_UNKNOWN = @"HKEY_CLASSES_ROOT\Unknown";
        public const string SYSFILEASSPATH = @"HKEY_CLASSES_ROOT\SystemFileAssociations";
        private const string LASTKEYPATH = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Applets\Regedit";

        public event EventHandler ItemsLoaded;
        private CancellationTokenSource cts;

        private struct ShellItemData
        {
            public string RegPath;
            public string Text;
            public Image Image;
            public bool IsMultiItem;
        }

        private static readonly Dictionary<string, int> DefaultNameIndexs = new(StringComparer.OrdinalIgnoreCase)
        {
            { "open", 8496 }, { "edit", 8516 }, { "print", 8497 }, { "find", 8503 },
            { "play", 8498 }, { "runas", 8505 }, { "explore", 8502 }, { "preview", 8499 }
        };

        public static readonly List<string> DirectoryTypes = new() { "Document", "Image", "Video", "Audio" };
        public static readonly List<string> PerceivedTypes = new() { null, "Text", "Document", "Image", "Video", "Audio", "Compressed", "System" };
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

        private static string GetDirectoryTypeName(string directoryType) =>
            directoryType == null ? null : DirectoryTypes.FindIndex(type => directoryType.Equals(type, StringComparison.OrdinalIgnoreCase)) is var idx && idx >= 0 ? DirectoryTypeNames[idx] : null;

        private static string GetPerceivedTypeName(string perceivedType)
        {
            var index = perceivedType != null ? PerceivedTypes.FindIndex(type => perceivedType.Equals(type, StringComparison.OrdinalIgnoreCase)) : 0;
            return PerceivedTypeNames[index == -1 ? 0 : index];
        }

        private static readonly string[] DropEffectPaths = { MENUPATH_FILE, MENUPATH_ALLOBJECTS, MENUPATH_FOLDER, MENUPATH_DIRECTORY };
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
                    if (value is int i && i is >= 1 and <= 4)
                        return (DropEffect)i;
                }
                return DropEffect.Default;
            }
            set
            {
                var data = value switch { DropEffect.Copy => 1, DropEffect.Move => 2, DropEffect.CreateLink => 4, _ => 0 };
                foreach (var path in DropEffectPaths)
                    Registry.SetValue(path, "DefaultDropEffect", data, RegistryValueKind.DWord);
            }
        }

        private static string GetDropEffectName() => DropEffectNames[(int)DefaultDropEffect];

        private static string CurrentExtension, CurrentDirectoryType, CurrentPerceivedType;
        public static string CurrentCustomRegPath, CurrentFileObjectPath;

        private static string CurrentExtensionPerceivedType
        {
            get => GetPerceivedType(CurrentExtension);
            set
            {
                var path = $"{RegistryEx.CLASSES_ROOT}\\{CurrentExtension}";
                if (value == null) RegistryEx.DeleteValue(path, "PerceivedType");
                else Registry.SetValue(path, "PerceivedType", value, RegistryValueKind.String);
            }
        }

        public static string GetShellPath(string scenePath) => $"{scenePath}\\shell";
        public static string GetShellExPath(string scenePath) => $"{scenePath}\\ShellEx";
        public static string GetSysAssExtPath(string typeName) => typeName != null ? $"{SYSFILEASSPATH}\\{typeName}" : null;
        private static string GetOpenMode(string extension) => FileExtension.GetOpenMode(extension);
        public static string GetOpenModePath(string extension) => extension != null ? $"{RegistryEx.CLASSES_ROOT}\\{GetOpenMode(extension)}" : null;
        private static string GetPerceivedType(string extension) => Registry.GetValue($"{RegistryEx.CLASSES_ROOT}\\{extension}", "PerceivedType", null)?.ToString();

        public Scenes Scene { get; set; }

        public void LoadItems()
        {
            var scenePath = GetScenePath(Scene);
            if (scenePath == string.Empty) return;
            if (scenePath == "SPECIAL")
            {
                HandleSpecialScenes();
                return;
            }

            if (WinOsVersion.Current >= WinOsVersion.Win11 && Scene is Scenes.File or Scenes.Folder or Scenes.Directory or Scenes.Background or Scenes.Desktop or Scenes.Drive or Scenes.AllObjects or Scenes.Computer or Scenes.RecycleBin or Scenes.Library)
                AddItem(new SwitchContextMenuStyleItem());

            AddNewItem(scenePath);
            LoadItems(scenePath);

            if (WinOsVersion.Current >= WinOsVersion.Win10) LoadUwpModeItem();

            AddSceneSpecificItems(scenePath);
        }

        private string GetScenePath(Scenes scene) => scene switch
        {
            Scenes.File => MENUPATH_FILE,
            Scenes.Folder => MENUPATH_FOLDER,
            Scenes.Directory => MENUPATH_DIRECTORY,
            Scenes.Background => MENUPATH_BACKGROUND,
            Scenes.Desktop => WinOsVersion.Current == WinOsVersion.Vista ? string.Empty : MENUPATH_DESKTOP,
            Scenes.Drive => MENUPATH_DRIVE,
            Scenes.AllObjects => MENUPATH_ALLOBJECTS,
            Scenes.Computer => MENUPATH_COMPUTER,
            Scenes.RecycleBin => MENUPATH_RECYCLEBIN,
            Scenes.Library => WinOsVersion.Current == WinOsVersion.Vista ? string.Empty : MENUPATH_LIBRARY,
            Scenes.LnkFile => GetOpenModePath(".lnk"),
            Scenes.UwpLnk => WinOsVersion.Current < WinOsVersion.Win8 ? string.Empty : MENUPATH_UWPLNK,
            Scenes.ExeFile => GetSysAssExtPath(".exe"),
            Scenes.UnknownType => MENUPATH_UNKNOWN,
            Scenes.CustomExtension => CurrentExtension?.ToLower() == ".lnk" ? GetOpenModePath(".lnk") : GetSysAssExtPath(CurrentExtension),
            Scenes.PerceivedType => GetSysAssExtPath(CurrentPerceivedType),
            Scenes.DirectoryType => CurrentDirectoryType == null ? null : GetSysAssExtPath($"Directory.{CurrentDirectoryType}"),
            Scenes.MenuAnalysis or Scenes.DragDrop or Scenes.PublicReferences or Scenes.CustomRegPath => "SPECIAL",
            _ => null
        };

        private void HandleSpecialScenes()
        {
            switch (Scene)
            {
                case Scenes.MenuAnalysis:
                    AddItem(new SelectItem(Scene));
                    LoadAnalysisItems();
                    break;
                case Scenes.DragDrop:
                    AddItem(new SelectItem(Scene));
                    AddNewItem(MENUPATH_FOLDER);
                    LoadShellExItems(GetShellExPath(MENUPATH_FOLDER));
                    LoadShellExItems(GetShellExPath(MENUPATH_DIRECTORY));
                    LoadShellExItems(GetShellExPath(MENUPATH_DRIVE));
                    LoadShellExItems(GetShellExPath(MENUPATH_ALLOBJECTS));
                    break;
                case Scenes.PublicReferences:
                    if (WinOsVersion.Current == WinOsVersion.Vista) return;
                    AddNewItem(RegistryEx.GetParentPath(ShellItem.CommandStorePath));
                    LoadStoreItems();
                    break;
                case Scenes.CustomRegPath:
                    AddNewItem(CurrentCustomRegPath);
                    LoadItems(CurrentCustomRegPath);
                    break;
            }
        }

        private void AddSceneSpecificItems(string scenePath)
        {
            switch (Scene)
            {
                case Scenes.Background: AddItem(new VisibleRegRuleItem(VisibleRegRuleItem.CustomFolder)); break;
                case Scenes.Computer: AddItem(new VisibleRegRuleItem(VisibleRegRuleItem.NetworkDrive)); break;
                case Scenes.RecycleBin: AddItem(new VisibleRegRuleItem(VisibleRegRuleItem.RecycleBinProperties)); break;
                case Scenes.Library:
                    LoadItems(MENUPATH_LIBRARY_BACKGROUND);
                    LoadItems(MENUPATH_LIBRARY_USER);
                    break;
                case Scenes.ExeFile: LoadItems(GetOpenModePath(".exe")); break;
                case Scenes.CustomExtension or Scenes.PerceivedType or Scenes.DirectoryType or Scenes.CustomRegPath:
                    InsertItem(new SelectItem(Scene), 0);
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
            cts?.Cancel();
            cts = new CancellationTokenSource();
            var token = cts.Token;

            Task.Run(() =>
            {
                if (token.IsCancellationRequested) return;
                RegTrustedInstaller.TakeRegKeyOwnerShip(scenePath);
                var shellItemsData = GetShellItemsData(GetShellPath(scenePath));
                if (token.IsCancellationRequested) return;

                Invoke(new Action(() =>
                {
                    if (token.IsCancellationRequested) return;
                    foreach (var data in shellItemsData)
                        AddItem(new ShellItem(data.RegPath, data.Text, data.Image, data.IsMultiItem));
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

            foreach (var keyName in shellKey.GetSubKeyNames())
            {
                var regPath = $"{shellPath}\\{keyName}";
                list.Add(new ShellItemData
                {
                    RegPath = regPath,
                    Text = GetItemText(regPath, keyName),
                    Image = GetItemIcon(regPath),
                    IsMultiItem = GetIsMultiItem(regPath)
                });
            }
            return list;
        }

        private bool GetIsMultiItem(string regPath) =>
            Registry.GetValue(regPath, "SubCommands", null) != null ||
            !string.IsNullOrEmpty(Registry.GetValue(regPath, "ExtendedSubCommandsKey", null)?.ToString());

        private string GetItemText(string regPath, string keyName)
        {
            foreach (var valueName in new[] { "MUIVerb", "" })
            {
                var name = ResourceString.GetDirectString(Registry.GetValue(regPath, valueName, null)?.ToString());
                if (!string.IsNullOrEmpty(name)) return name;
            }
            if (DefaultNameIndexs.TryGetValue(RegistryEx.GetKeyName(regPath), out var index))
            {
                var name = ResourceString.GetDirectString($"@windows.storage.dll,-{index}");
                if (!string.IsNullOrEmpty(name)) return name;
            }
            return RegistryEx.GetKeyName(regPath);
        }

        private Image GetItemIcon(string regPath)
        {
            var iconLocation = Registry.GetValue(regPath, "Icon", null)?.ToString();
            var hasLUAShield = Registry.GetValue(regPath, "HasLUAShield", null) != null;
            var commandPath = $"{regPath}\\command";

            Guid guid = Guid.Empty;
            foreach (var item in new Dictionary<string, string> { { commandPath, "DelegateExecute" }, { $"{regPath}\\DropTarget", "CLSID" }, { regPath, "ExplorerCommandHandler" } })
                if (GuidEx.TryParse(Registry.GetValue(item.Key, item.Value, null)?.ToString(), out var g)) { guid = g; break; }

            var itemCommand = !GetIsMultiItem(regPath) ? Registry.GetValue(commandPath, "", null)?.ToString() : null;
            var itemFilePath = GuidInfo.GetFilePath(guid) ?? ObjectPath.ExtractFilePath(itemCommand);

            Icon icon = iconLocation != null ? ResourceIcon.GetIcon(iconLocation, out var iconPath, out _) ?? (Path.GetExtension(iconPath)?.ToLower() == ".exe" ? ResourceIcon.GetIcon("imageres.dll", -15) : null)
                : hasLUAShield ? ResourceIcon.GetIcon("imageres.dll", -78)
                : ResourceIcon.GetIcon(itemFilePath, 0);

            icon ??= ResourceIcon.GetExtensionIcon(itemFilePath) ?? ResourceIcon.GetIcon("imageres.dll", -2);
            var image = icon.ToBitmap();
            return iconLocation == null && !hasLUAShield ? image.ToTransparent() : image;
        }

        private void LoadShellExItems(string shellExPath)
        {
            using var shellExKey = RegistryEx.GetRegistryKey(shellExPath);
            if (shellExKey == null) return;

            var isDragDrop = Scene == Scenes.DragDrop;
            RegTrustedInstaller.TakeRegTreeOwnerShip(shellExKey.Name);
            var dic = ShellExItem.GetPathAndGuids(shellExPath, isDragDrop);
            var names = new List<string>();
            FoldGroupItem groupItem = null;

            if (isDragDrop)
            {
                groupItem = GetDragDropGroupItem(shellExPath);
                AddItem(groupItem);
            }

            foreach (var path in dic.Keys)
            {
                var keyName = RegistryEx.GetKeyName(path);
                if (names.Contains(keyName)) continue;
                var item = new ShellExItem(dic[path], path);
                if (groupItem != null)
                {
                    item.FoldGroupItem = groupItem;
                    item.Indent();
                }
                AddItem(item);
                names.Add(keyName);
            }
            groupItem?.SetVisibleWithSubItemCount();
        }

        private FoldGroupItem GetDragDropGroupItem(string shellExPath)
        {
            var path = shellExPath[..shellExPath.LastIndexOf('\\')];
            var (text, image) = path switch
            {
                MENUPATH_FOLDER => (AppString.SideBar.Folder, AppImage.Folder),
                MENUPATH_DIRECTORY => (AppString.SideBar.Directory, AppImage.Directory),
                MENUPATH_DRIVE => (AppString.SideBar.Drive, AppImage.Drive),
                MENUPATH_ALLOBJECTS => (AppString.SideBar.AllObjects, AppImage.AllObjects),
                _ => (null, null)
            };
            return new FoldGroupItem(shellExPath, ObjectPath.PathType.Registry) { Text = text, Image = image };
        }

        private void LoadStoreItems()
        {
            using var shellKey = RegistryEx.GetRegistryKey(ShellItem.CommandStorePath);
            foreach (var itemName in shellKey.GetSubKeyNames())
            {
                if (AppConfig.HideSysStoreItems && itemName.StartsWith("Windows.", StringComparison.OrdinalIgnoreCase)) continue;
                AddItem(new StoreShellItem($"{ShellItem.CommandStorePath}\\{itemName}", true, false));
            }
        }

        private void LoadUwpModeItem()
        {
            foreach (var doc in XmlDicHelper.UwpModeItemsDic)
            {
                if (doc?.DocumentElement == null) continue;
                foreach (XmlNode sceneXN in doc.DocumentElement.ChildNodes)
                {
                    if (sceneXN.Name != Scene.ToString()) continue;
                    foreach (XmlElement itemXE in sceneXN.ChildNodes)
                    {
                        if (!GuidEx.TryParse(itemXE.GetAttribute("Guid"), out var guid)) continue;
                        if (Controls.Cast<Control>().Any(ctr => ctr is UwpModeItem item && item.Guid == guid)) continue;
                        if (GuidInfo.GetFilePath(guid) == null) continue;
                        AddItem(new UwpModeItem(GuidInfo.GetUwpName(guid), guid));
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
                JumpItem.TargetPath = filePath;
                JumpItem.Extension = extension;
                JumpItem.PerceivedType = perceivedType;
                AddItem(new JumpItem(Scenes.File));
                AddItem(new JumpItem(Scenes.AllObjects));
                AddItem(new JumpItem(extension == ".exe" ? Scenes.ExeFile : Scenes.CustomExtension));
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
                    using var shellLink = new ShellLink(CurrentFileObjectPath);
                    var targetPath = shellLink.TargetPath;
                    if (File.Exists(targetPath)) AddFileItems(targetPath);
                    else if (Directory.Exists(targetPath)) AddDirItems(targetPath);
                    AddItem(new JumpItem(Scenes.LnkFile));
                }
                else AddFileItems(CurrentFileObjectPath);
            }
            else if (Directory.Exists(CurrentFileObjectPath)) AddDirItems(CurrentFileObjectPath);
        }

        public class SelectItem : MyListItem
        {
            public Scenes Scene { get; private set; }
            public string SelectedPath { get; set; }
            private readonly PictureButton BtnSelect = new(AppImage.Select);

            public SelectItem(Scenes scene)
            {
                Scene = scene;
                AddCtr(BtnSelect);
                SetTextAndTip();
                SetImage();
                BtnSelect.MouseDown += (sender, e) => ShowSelectDialog();
                MouseDoubleClick += (sender, e) => ShowSelectDialog();
            }

            private void SetTextAndTip()
            {
                string tip, text;
                switch (Scene)
                {
                    case Scenes.CustomExtension:
                        tip = AppString.Dialog.SelectExtension;
                        text = CurrentExtension == null ? AppString.Dialog.SelectExtension : AppString.Other.CurrentExtension.Replace("%s", CurrentExtension);
                        break;
                    case Scenes.PerceivedType:
                        tip = AppString.Dialog.SelectPerceivedType;
                        text = CurrentPerceivedType == null ? AppString.Dialog.SelectPerceivedType : AppString.Other.CurrentPerceivedType.Replace("%s", GetPerceivedTypeName(CurrentPerceivedType));
                        break;
                    case Scenes.DirectoryType:
                        tip = AppString.Dialog.SelectDirectoryType;
                        text = CurrentDirectoryType == null ? AppString.Dialog.SelectDirectoryType : AppString.Other.CurrentDirectoryType.Replace("%s", GetDirectoryTypeName(CurrentDirectoryType));
                        break;
                    case Scenes.CustomRegPath:
                        tip = AppString.Other.SelectRegPath;
                        SelectedPath = CurrentCustomRegPath;
                        text = SelectedPath == null ? AppString.Other.SelectRegPath : AppString.Other.CurrentRegPath + "\n" + SelectedPath;
                        break;
                    case Scenes.MenuAnalysis:
                        tip = AppString.Tip.DropOrSelectObject;
                        SelectedPath = CurrentFileObjectPath;
                        text = SelectedPath == null ? AppString.Tip.DropOrSelectObject : AppString.Other.CurrentFilePath + "\n" + SelectedPath;
                        break;
                    case Scenes.DragDrop:
                        tip = AppString.Dialog.SelectDropEffect;
                        SelectedPath = GetDropEffectName();
                        text = AppString.Other.SetDefaultDropEffect + " " + SelectedPath;
                        break;
                    case Scenes.CustomExtensionPerceivedType:
                        tip = AppString.Dialog.SelectPerceivedType;
                        text = AppString.Other.SetPerceivedType.Replace("%s", CurrentExtension) + " " + GetPerceivedTypeName(CurrentExtensionPerceivedType);
                        break;
                    default:
                        tip = "";
                        text = "";
                        break;
                }
                ToolTipBox.SetToolTip(BtnSelect, tip);
                Text = text;
            }

            private void SetImage()
            {
                if (Scene == Scenes.CustomExtensionPerceivedType)
                    using (var icon = ResourceIcon.GetExtensionIcon(CurrentExtension))
                        Image = icon?.ToBitmap();
                Image ??= AppImage.Custom;
            }

            private void ShowSelectDialog()
            {
                SelectDialog dlg = null;
                switch (Scene)
                {
                    case Scenes.CustomExtension:
                        dlg = new FileExtensionDialog { Selected = CurrentExtension?[1..] };
                        break;
                    case Scenes.PerceivedType:
                        dlg = new SelectDialog { Items = PerceivedTypeNames, Title = AppString.Dialog.SelectPerceivedType, Selected = GetPerceivedTypeName(CurrentPerceivedType) };
                        break;
                    case Scenes.DirectoryType:
                        dlg = new SelectDialog { Items = DirectoryTypeNames, Title = AppString.Dialog.SelectDirectoryType, Selected = GetDirectoryTypeName(CurrentDirectoryType) };
                        break;
                    case Scenes.CustomExtensionPerceivedType:
                        dlg = new SelectDialog { Items = PerceivedTypeNames, Title = AppString.Dialog.SelectPerceivedType, Selected = GetPerceivedTypeName(CurrentExtensionPerceivedType) };
                        break;
                    case Scenes.DragDrop:
                        dlg = new SelectDialog { Items = DropEffectNames, Title = AppString.Dialog.SelectDropEffect, Selected = GetDropEffectName() };
                        break;
                    case Scenes.MenuAnalysis:
                        dlg = new SelectDialog { Items = new[] { AppString.SideBar.File, AppString.SideBar.Directory }, Title = AppString.Dialog.SelectObjectType };
                        break;
                    case Scenes.CustomRegPath:
                        if (AppMessageBox.Show(AppString.Message.SelectRegPath, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                        var frm = FindForm();
                        frm.Hide();
                        using (var process = Process.Start("regedit.exe", "-m")) process.WaitForExit();
                        var path = Registry.GetValue(LASTKEYPATH, "LastKey", "").ToString();
                        var index = path.IndexOf('\\');
                        if (index == -1) return;
                        CurrentCustomRegPath = path[(index + 1)..];
                        RefreshList();
                        frm.Show();
                        frm.Activate();
                        return;
                }

                if (dlg?.ShowDialog() != DialogResult.OK) return;

                switch (Scene)
                {
                    case Scenes.CustomExtension: CurrentExtension = dlg.Selected; RefreshList(); break;
                    case Scenes.PerceivedType: CurrentPerceivedType = PerceivedTypes[dlg.SelectedIndex]; RefreshList(); break;
                    case Scenes.DirectoryType: CurrentDirectoryType = DirectoryTypes[dlg.SelectedIndex]; RefreshList(); break;
                    case Scenes.CustomExtensionPerceivedType:
                        CurrentExtensionPerceivedType = PerceivedTypes[dlg.SelectedIndex];
                        Text = AppString.Other.SetPerceivedType.Replace("%s", CurrentExtension) + " " + GetPerceivedTypeName(CurrentExtensionPerceivedType);
                        break;
                    case Scenes.DragDrop:
                        DefaultDropEffect = (DropEffect)(dlg.SelectedIndex is >= 0 and <= 3 ? dlg.SelectedIndex : 0);
                        Text = AppString.Other.SetDefaultDropEffect + " " + GetDropEffectName();
                        break;
                    case Scenes.MenuAnalysis:
                        if (dlg.SelectedIndex == 0)
                        {
                            using var dlg1 = new System.Windows.Forms.OpenFileDialog { DereferenceLinks = false };
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
            public static string Extension, PerceivedType, TargetPath;
            private readonly PictureButton btnJump = new(AppImage.Jump);

            public JumpItem(Scenes scene)
            {
                AddCtr(btnJump);
                var (txts, image, index1, index2) = scene switch
                {
                    Scenes.File => (new[] { AppString.ToolBar.Home, AppString.SideBar.File }, AppImage.File, 0, 0),
                    Scenes.Folder => (new[] { AppString.ToolBar.Home, AppString.SideBar.Folder }, AppImage.Folder, 0, 1),
                    Scenes.Directory => (new[] { AppString.ToolBar.Home, AppString.SideBar.Directory }, AppImage.Directory, 0, 2),
                    Scenes.Drive => (new[] { AppString.ToolBar.Home, AppString.SideBar.Drive }, AppImage.Drive, 0, 5),
                    Scenes.AllObjects => (new[] { AppString.ToolBar.Home, AppString.SideBar.AllObjects }, AppImage.AllObjects, 0, 6),
                    Scenes.LnkFile => (new[] { AppString.ToolBar.Type, AppString.SideBar.LnkFile }, AppImage.LnkFile, 1, 0),
                    Scenes.ExeFile => (new[] { AppString.ToolBar.Type, AppString.SideBar.ExeFile }, GetIcon(), 1, 2),
                    Scenes.UnknownType => (new[] { AppString.ToolBar.Type, AppString.SideBar.UnknownType }, AppImage.NotFound, 1, 8),
                    Scenes.CustomExtension => (new[] { AppString.ToolBar.Type, AppString.SideBar.CustomExtension, Extension }, GetIcon(Extension), 1, 5),
                    Scenes.PerceivedType => (new[] { AppString.ToolBar.Type, AppString.SideBar.PerceivedType, GetPerceivedTypeName(PerceivedType) }, AppImage.File, 1, 6),
                    Scenes.DirectoryType => (new[] { AppString.ToolBar.Type, AppString.SideBar.DirectoryType }, AppImage.Directory, 1, 7),
                    _ => (null, null, 0, 0)
                };
                Text = "[ " + string.Join(" ]  ▶  [ ", txts) + " ]";
                Image = image;
                void SwitchTab()
                {
                    if (scene == Scenes.CustomExtension) CurrentExtension = Extension;
                    else if (scene == Scenes.PerceivedType) CurrentPerceivedType = PerceivedType;
                    ((MainForm)FindForm()).JumpItem(index1, index2);
                }
                btnJump.MouseDown += (sender, e) => SwitchTab();
                DoubleClick += (sender, e) => SwitchTab();

                static Image GetIcon(string ext = null)
                {
                    using var icon = ResourceIcon.GetExtensionIcon(ext ?? TargetPath);
                    return icon?.ToBitmap();
                }
            }
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

            if (Scene == Scenes.DragDrop || ShellItem.CommandStorePath.Equals(shellPath, StringComparison.OrdinalIgnoreCase))
                btnAddExisting.Visible = false;
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
                    using var dlg = new SelectDialog { Items = new[] { "Shell", "ShellEx" }, Title = AppString.Dialog.SelectNewItemType };
                    if (dlg.ShowDialog() != DialogResult.OK) return;
                    isShell = dlg.SelectedIndex == 0;
                }
                if (isShell) AddNewShellItem(scenePath);
                else AddNewShellExItem(scenePath);
            };

            btnAddExisting.MouseDown += (sender, e) =>
            {
                using var dlg = new ShellStoreDialog { IsReference = false, ShellPath = ShellItem.CommandStorePath, Filter = itemName => !(AppConfig.HideSysStoreItems && itemName.StartsWith("Windows.", StringComparison.OrdinalIgnoreCase)) };
                if (dlg.ShowDialog() != DialogResult.OK) return;
                foreach (var keyName in dlg.SelectedKeyNames)
                {
                    var srcPath = $"{dlg.ShellPath}\\{keyName}";
                    var dstPath = ObjectPath.GetNewPathWithIndex($"{shellPath}\\{keyName}", ObjectPath.PathType.Registry);
                    RegistryEx.CopyTo(srcPath, dstPath);
                    AddItem(new ShellItem(dstPath));
                }
            };

            btnEnhanceMenu.MouseDown += (sender, e) =>
            {
                var tempPath1 = Path.GetTempFileName();
                var tempPath2 = Path.GetTempFileName();
                ExternalProgram.ExportRegistry(scenePath, tempPath1);
                using (var dlg = new EnhanceMenusDialog { ScenePath = scenePath }) dlg.ShowDialog();
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
            using var dlg = new NewShellDialog { ScenePath = scenePath, ShellPath = shellPath };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            for (var i = 0; i < Controls.Count; i++)
            {
                if (Controls[i] is not NewItem) continue;
                var item = Scene != Scenes.PublicReferences ? new ShellItem(dlg.NewItemRegPath) : new StoreShellItem(dlg.NewItemRegPath, true, false);
                InsertItem(item, i + 1);
                break;
            }
        }

        private void AddNewShellExItem(string scenePath)
        {
            var isDragDrop = Scene == Scenes.DragDrop;
            using var dlg1 = new InputDialog { Title = AppString.Dialog.InputGuid };
            if (GuidEx.TryParse(Clipboard.GetText(), out var guid)) dlg1.Text = guid.ToString();
            if (dlg1.ShowDialog() != DialogResult.OK) return;
            if (!GuidEx.TryParse(dlg1.Text, out guid))
            {
                AppMessageBox.Show(AppString.Message.MalformedGuid);
                return;
            }

            if (isDragDrop)
            {
                using var dlg2 = new SelectDialog { Title = AppString.Dialog.SelectGroup, Items = new[] { AppString.SideBar.Folder, AppString.SideBar.Directory, AppString.SideBar.Drive, AppString.SideBar.AllObjects } };
                if (dlg2.ShowDialog() != DialogResult.OK) return;
                scenePath = dlg2.SelectedIndex switch { 0 => MENUPATH_FOLDER, 1 => MENUPATH_DIRECTORY, 2 => MENUPATH_DRIVE, 3 => MENUPATH_ALLOBJECTS, _ => scenePath };
            }

            var shellExPath = GetShellExPath(scenePath);
            if (ShellExItem.GetPathAndGuids(shellExPath, isDragDrop).Values.Contains(guid))
            {
                AppMessageBox.Show(AppString.Message.HasBeenAdded);
                return;
            }

            var part = isDragDrop ? ShellExItem.DdhParts[0] : ShellExItem.CmhParts[0];
            var regPath = $"{shellExPath}\\{part}\\{guid:B}";
            Registry.SetValue(regPath, "", guid.ToString("B"));
            var item = new ShellExItem(guid, regPath);

            for (var i = 0; i < Controls.Count; i++)
            {
                if (isDragDrop && Controls[i] is FoldGroupItem groupItem && groupItem.GroupPath.Equals(shellExPath, StringComparison.OrdinalIgnoreCase))
                {
                    InsertItem(item, i + 1);
                    item.FoldGroupItem = groupItem;
                    groupItem.SetVisibleWithSubItemCount();
                    item.Visible = !groupItem.IsFold;
                    item.Indent();
                    break;
                }
                else if (!isDragDrop && Controls[i] is NewItem)
                {
                    InsertItem(item, i + 1);
                    break;
                }
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
                cmbDic.SelectionChangeCommitted += (sender, e) => { Focus(); UseWin11ContextMenuStyle = cmbDic.SelectedIndex == 0; };
                useWin11ContextMenuStyle = Registry.CurrentUser.OpenSubKey(registryKeyPath) == null;
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
                        var registryKey = Registry.CurrentUser.OpenSubKey(registryKeyPath, true);
                        if (registryKey != null) Registry.CurrentUser.DeleteSubKeyTree(registryKeyPath);
                    }
                    else
                    {
                        Registry.CurrentUser.CreateSubKey(registrySubKeyPath)?.SetValue(null, "", RegistryValueKind.String);
                    }
                    ExplorerRestarter.Show();
                }
            }

            private readonly RComboBox cmbDic = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180.DpiZoom() };
        }
    }
}
