using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
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

        private static readonly List<string> DirectoryTypes = new List<string>
        {
            "Document", "Image", "Video", "Audio"
        };
        private static readonly List<string> PerceivedTypes = new List<string>
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
            if(directoryType == null) return null;
            int index = DirectoryTypes.FindIndex(type => directoryType.Equals(type, StringComparison.OrdinalIgnoreCase));
            if(index >= 0) return DirectoryTypeNames[index];
            else return null;
        }

        private static string GetPerceivedTypeName(string perceivedType)
        {
            int index = 0;
            if(perceivedType != null) index = PerceivedTypes.FindIndex(type => perceivedType.Equals(type, StringComparison.OrdinalIgnoreCase));
            if(index == -1) index = 0;
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
                foreach(string path in DropEffectPaths)
                {
                    object value = Registry.GetValue(path, "DefaultDropEffect", null);
                    if(value != null)
                    {
                        switch(value)
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
                object data;
                switch(value)
                {
                    case DropEffect.Copy:
                        data = 1; break;
                    case DropEffect.Move:
                        data = 2; break;
                    case DropEffect.CreateLink:
                        data = 4; break;
                    default:
                        data = 0; break;
                }
                foreach(string path in DropEffectPaths)
                {
                    Registry.SetValue(path, "DefaultDropEffect", data, RegistryValueKind.DWord);
                }
            }
        }

        private static string GetDropEffectName()
        {
            switch(DefaultDropEffect)
            {
                case DropEffect.Copy:
                    return DropEffectNames[1];
                case DropEffect.Move:
                    return DropEffectNames[2];
                case DropEffect.CreateLink:
                    return DropEffectNames[3];
                default:
                    return DropEffectNames[0];
            }
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
                string path = $@"{RegistryEx.CLASSES_ROOT}\{CurrentExtension}";
                if(value == null) RegistryEx.DeleteValue(path, "PerceivedType");
                else Registry.SetValue(path, "PerceivedType", value, RegistryValueKind.String);
            }
        }

        public static string GetShellPath(string scenePath) => $@"{scenePath}\shell";
        public static string GetShellExPath(string scenePath) => $@"{scenePath}\ShellEx";
        public static string GetSysAssExtPath(string typeName) => typeName != null ? $@"{SYSFILEASSPATH}\{typeName}" : null;
        private static string GetOpenMode(string extension) => FileExtension.GetOpenMode(extension);
        public static string GetOpenModePath(string extension) => extension != null ? $@"{RegistryEx.CLASSES_ROOT}\{GetOpenMode(extension)}" : null;
        private static string GetPerceivedType(string extension) => Registry.GetValue($@"{RegistryEx.CLASSES_ROOT}\{extension}", "PerceivedType", null)?.ToString();

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
                    bool isLnk = CurrentExtension?.ToLower() == ".lnk";
                    if(isLnk) scenePath = GetOpenModePath(".lnk");
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
            if(WinOsVersion.Current >= WinOsVersion.Win10)
            {
                LoadUwpModeItem();
            }
            switch(Scene)
            {
                case Scenes.Background:
                    VisibleRegRuleItem item = new VisibleRegRuleItem(VisibleRegRuleItem.CustomFolder);
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
            if(scenePath == null) return;
            RegTrustedInstaller.TakeRegKeyOwnerShip(scenePath);
            LoadShellItems(GetShellPath(scenePath));
            LoadShellExItems(GetShellExPath(scenePath));
        }
        
        private void LoadShellItems(string shellPath)
        {
            using (RegistryKey shellKey = RegistryEx.GetRegistryKey(shellPath))
            {
                if (shellKey == null) return;
                RegTrustedInstaller.TakeRegTreeOwnerShip(shellKey.Name);
                foreach(string keyName in shellKey.GetSubKeyNames())
                {
                    ShellItem item = new ShellItem($@"{shellPath}\{keyName}");
                    AddItem(item);
                }
            }
        }
        
        private void LoadShellExItems(string shellExPath)
        {
            List<string> names = new List<string>();
            using(RegistryKey shellExKey = RegistryEx.GetRegistryKey(shellExPath))
            {
                if(shellExKey == null) return;
                bool isDragDrop = Scene == Scenes.DragDrop;
                RegTrustedInstaller.TakeRegTreeOwnerShip(shellExKey.Name);
                Dictionary<string, Guid> dic = ShellExItem.GetPathAndGuids(shellExPath, isDragDrop);
                FoldGroupItem groupItem = null;
                if(isDragDrop)
                {
                    groupItem = GetDragDropGroupItem(shellExPath);
                    // TODO:什么是groupItem？
                    AddItem(groupItem);
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
                    if(!names.Contains(keyName))
                    {
                        ShellExItem item = new ShellExItem(dic[path], path);
                        if(groupItem != null)
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
        }

        private FoldGroupItem GetDragDropGroupItem(string shellExPath)
        {
            string text = null;
            Image image = null;
            string path = shellExPath.Substring(0, shellExPath.LastIndexOf('\\'));
            switch(path)
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
            using (RegistryKey shellKey = RegistryEx.GetRegistryKey(ShellItem.CommandStorePath))
            {
                foreach(string itemName in shellKey.GetSubKeyNames())
                {
                    if(AppConfig.HideSysStoreItems && itemName.StartsWith("Windows.", StringComparison.OrdinalIgnoreCase)) continue;
                    AddItem(new StoreShellItem($@"{ShellItem.CommandStorePath}\{itemName}", true, false));
                }
            }
        }

        private void LoadUwpModeItem()
        {
            foreach (XmlDocument doc in XmlDicHelper.UwpModeItemsDic)
            {
                if(doc?.DocumentElement == null) continue;
                foreach(XmlNode sceneXN in doc.DocumentElement.ChildNodes)
                {
                    if(sceneXN.Name == Scene.ToString())
                    {
                        foreach(XmlElement itemXE in sceneXN.ChildNodes)
                        {
                            if(GuidEx.TryParse(itemXE.GetAttribute("Guid"), out Guid guid))
                            {
                                bool isAdded = false;
                                foreach(Control ctr in Controls)
                                {
                                    if(ctr is UwpModeItem item && item.Guid == guid) { isAdded = true; break; }
                                }
                                if(isAdded) continue;
                                if(GuidInfo.GetFilePath(guid) == null) continue;
                                string uwpName = GuidInfo.GetUwpName(guid);
                                UwpModeItem uwpItem = new UwpModeItem(uwpName, guid);
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
                string extension = Path.GetExtension(filePath).ToLower();
                if(extension == string.Empty) extension = ".";
                string perceivedType = GetPerceivedType(extension);
                string perceivedTypeName = GetPerceivedTypeName(perceivedType);
                JumpItem.TargetPath = filePath;
                JumpItem.Extension = extension;
                JumpItem.PerceivedType = perceivedType;
                AddItem(new JumpItem(Scenes.File));
                AddItem(new JumpItem(Scenes.AllObjects));
                if(extension == ".exe") AddItem(new JumpItem(Scenes.ExeFile));
                else AddItem(new JumpItem(Scenes.CustomExtension));
                if(GetOpenMode(extension) == null) AddItem(new JumpItem(Scenes.UnknownType));
                if(perceivedType != null) AddItem(new JumpItem(Scenes.PerceivedType));
            }

            void AddDirItems(string dirPath)
            {
                if(!dirPath.EndsWith(":\\"))
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

            if(File.Exists(CurrentFileObjectPath))
            {
                string extension = Path.GetExtension(CurrentFileObjectPath).ToLower();
                if(extension == ".lnk")
                {
                    using(ShellLink shellLink = new ShellLink(CurrentFileObjectPath))
                    {
                        string targetPath = shellLink.TargetPath;
                        if(File.Exists(targetPath)) AddFileItems(targetPath);
                        else if(Directory.Exists(targetPath)) AddDirItems(targetPath);
                    }
                    AddItem(new JumpItem(Scenes.LnkFile));
                }
                else AddFileItems(CurrentFileObjectPath);
            }
            else if(Directory.Exists(CurrentFileObjectPath)) AddDirItems(CurrentFileObjectPath);
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

            readonly PictureButton BtnSelect = new PictureButton(AppImage.Select);

            public Scenes Scene { get; private set; }
            public string SelectedPath { get; set; }

            private void SetTextAndTip()
            {
                string tip = "";
                string text = "";
                switch(Scene)
                {
                    case Scenes.CustomExtension:
                        tip = AppString.Dialog.SelectExtension;
                        if(CurrentExtension == null) text = tip;
                        else text = AppString.Other.CurrentExtension.Replace("%s", CurrentExtension);
                        break;
                    case Scenes.PerceivedType:
                        tip = AppString.Dialog.SelectPerceivedType;
                        if(CurrentPerceivedType == null) text = tip;
                        else text = AppString.Other.CurrentPerceivedType.Replace("%s", GetPerceivedTypeName(CurrentPerceivedType));
                        break;
                    case Scenes.DirectoryType:
                        tip = AppString.Dialog.SelectDirectoryType;
                        if(CurrentDirectoryType == null) text = tip;
                        else text = AppString.Other.CurrentDirectoryType.Replace("%s", GetDirectoryTypeName(CurrentDirectoryType));
                        break;
                    case Scenes.CustomRegPath:
                        SelectedPath = CurrentCustomRegPath;
                        tip = AppString.Other.SelectRegPath;
                        if(SelectedPath == null) text = tip;
                        else text = AppString.Other.CurrentRegPath + "\n" + SelectedPath;
                        break;
                    case Scenes.MenuAnalysis:
                        SelectedPath = CurrentFileObjectPath;
                        tip = AppString.Tip.DropOrSelectObject;
                        if(SelectedPath == null) text = tip;
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
                switch(Scene)
                {
                    case Scenes.CustomExtensionPerceivedType:
                        using(Icon icon = ResourceIcon.GetExtensionIcon(CurrentExtension))
                            Image = icon?.ToBitmap();
                        break;
                }
                if(Image == null) Image = AppImage.Custom;
            }

            private void ShowSelectDialog()
            {
                SelectDialog dlg = null;
                switch(Scene)
                {
                    case Scenes.CustomExtension:
                        dlg = new FileExtensionDialog
                        {
                            Selected = CurrentExtension?.Substring(1)
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
                        if(AppMessageBox.Show(AppString.Message.SelectRegPath,
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                        Form frm = FindForm();
                        frm.Hide();
                        using(Process process = Process.Start("regedit.exe", "-m"))
                        {
                            process.WaitForExit();
                        }
                        string path = Registry.GetValue(LASTKEYPATH, "LastKey", "").ToString();
                        int index = path.IndexOf('\\');
                        if(index == -1) return;
                        path = path.Substring(index + 1);
                        CurrentCustomRegPath = path;
                        RefreshList();
                        frm.Show();
                        frm.Activate();
                        break;
                }
                switch(Scene)
                {
                    case Scenes.CustomExtension:
                    case Scenes.PerceivedType:
                    case Scenes.DirectoryType:
                    case Scenes.MenuAnalysis:
                    case Scenes.DragDrop:
                    case Scenes.CustomExtensionPerceivedType:
                        if(dlg.ShowDialog() != DialogResult.OK) return;
                        break;
                }
                switch(Scene)
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
                        string selected = PerceivedTypes[dlg.SelectedIndex];
                        CurrentExtensionPerceivedType = selected;
                        Text = AppString.Other.SetPerceivedType.Replace("%s", CurrentExtension) + " " + GetPerceivedTypeName(selected);
                        break;
                    case Scenes.DragDrop:
                        switch(dlg.SelectedIndex)
                        {
                            case 0: DefaultDropEffect = DropEffect.Default; break;
                            case 1: DefaultDropEffect = DropEffect.Copy; break;
                            case 2: DefaultDropEffect = DropEffect.Move; break;
                            case 3: DefaultDropEffect = DropEffect.CreateLink; break;
                        }
                        Text = AppString.Other.SetDefaultDropEffect + " " + GetDropEffectName();
                        break;
                    case Scenes.MenuAnalysis:
                        if(dlg.SelectedIndex == 0)
                        {
                            using(var dlg1 = new System.Windows.Forms.OpenFileDialog())
                            {
                                dlg1.DereferenceLinks = false;
                                if(dlg1.ShowDialog() != DialogResult.OK) return;
                                CurrentFileObjectPath = dlg1.FileName;
                            }
                        }
                        else
                        {
                            using(var dlg2 = new FolderBrowserDialog())
                            {
                                if(dlg2.ShowDialog() != DialogResult.OK) return;
                                CurrentFileObjectPath = dlg2.SelectedPath;
                            }
                        }
                        RefreshList();
                        break;
                }
            }

            private void RefreshList()
            {
                ShellList list = (ShellList)Parent;
                list.ClearItems();
                list.LoadItems();
            }
        }

        sealed class JumpItem : MyListItem
        {
            public JumpItem(Scenes scene)
            {
                AddCtr(btnJump);
                Image image = null;
                int index1 = 0;
                int index2 = 0;
                string[] txts = null;
                switch(scene)
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
                        break;
                    case Scenes.ExeFile:
                        txts = new[] { AppString.ToolBar.Type, AppString.SideBar.ExeFile };
                        using(Icon icon = ResourceIcon.GetExtensionIcon(TargetPath)) image = icon.ToBitmap();
                        index1 = 1;
                        index2 = 2;
                        break;
                    case Scenes.UnknownType:
                        txts = new[] { AppString.ToolBar.Type, AppString.SideBar.UnknownType };
                        image = AppImage.NotFound;
                        index1 = 1;
                        index2 = 8;
                        break;
                    case Scenes.CustomExtension:
                        txts = new[] { AppString.ToolBar.Type, AppString.SideBar.CustomExtension, Extension };
                        using(Icon icon = ResourceIcon.GetExtensionIcon(Extension)) image = icon.ToBitmap();
                        index1 = 1;
                        index2 = 4;
                        break;
                    case Scenes.PerceivedType:
                        txts = new[] { AppString.ToolBar.Type, AppString.SideBar.PerceivedType, GetPerceivedTypeName(PerceivedType) };
                        image = AppImage.File;
                        index1 = 1;
                        index2 = 5;
                        break;
                    case Scenes.DirectoryType:
                        txts = new[] { AppString.ToolBar.Type, AppString.SideBar.DirectoryType };
                        image = AppImage.Directory;
                        index1 = 1;
                        index2 = 6;
                        break;
                }
                Text = "[ " + string.Join(" ]  ▶  [ ", txts) + " ]";
                Image = image;
                void SwitchTab()
                {
                    switch(scene)
                    {
                        case Scenes.CustomExtension:
                            CurrentExtension = Extension; break;
                        case Scenes.PerceivedType:
                            CurrentPerceivedType = PerceivedType; break;
                    }
                    ((MainForm)FindForm()).JumpItem(index1, index2);
                };
                btnJump.MouseDown += (sender, e) => SwitchTab();
                DoubleClick += (sender, e) => SwitchTab();
            }

            readonly PictureButton btnJump = new PictureButton(AppImage.Jump);

            public static string Extension = null;
            public static string PerceivedType = null;
            public static string TargetPath = null;
        }

        private void AddNewItem(string scenePath)
        {
            if (scenePath == null) return;
            string shellPath = GetShellPath(scenePath);
            NewItem newItem = new NewItem();
            PictureButton btnAddExisting = new PictureButton(AppImage.AddExisting);
            PictureButton btnEnhanceMenu = new PictureButton(AppImage.Enhance);
            ToolTipBox.SetToolTip(btnAddExisting, AppString.Tip.AddFromPublic);
            ToolTipBox.SetToolTip(btnEnhanceMenu, AppString.StatusBar.EnhanceMenu);
            if (Scene == Scenes.DragDrop || ShellItem.CommandStorePath.Equals(shellPath,
                StringComparison.OrdinalIgnoreCase)) btnAddExisting.Visible = false;
            else
            {
                using (RegistryKey key = RegistryEx.GetRegistryKey(ShellItem.CommandStorePath))
                {
                    List<string> subKeyNames = key.GetSubKeyNames().ToList();
                    if (AppConfig.HideSysStoreItems) subKeyNames.RemoveAll(name => name.StartsWith("Windows.", StringComparison.OrdinalIgnoreCase));
                    if (subKeyNames.Count == 0) btnAddExisting.Visible = false;
                }
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
                    using (SelectDialog dlg = new SelectDialog())
                    {
                        dlg.Items = new[] { "Shell", "ShellEx" };
                        dlg.Title = AppString.Dialog.SelectNewItemType;
                        if (dlg.ShowDialog() != DialogResult.OK) return;
                        isShell = dlg.SelectedIndex == 0;
                    }
                }
                if (isShell) AddNewShellItem(scenePath);
                else AddNewShellExItem(scenePath);
            };

            btnAddExisting.MouseDown += (sender, e) =>
            {
                using (ShellStoreDialog dlg = new ShellStoreDialog())
                {
                    dlg.IsReference = false;
                    dlg.ShellPath = ShellItem.CommandStorePath;
                    dlg.Filter = new Func<string, bool>(itemName => !(AppConfig.HideSysStoreItems
                        && itemName.StartsWith("Windows.", StringComparison.OrdinalIgnoreCase)));
                    if (dlg.ShowDialog() != DialogResult.OK) return;
                    foreach (string keyName in dlg.SelectedKeyNames)
                    {
                        string srcPath = $@"{dlg.ShellPath}\{keyName}";
                        string dstPath = ObjectPath.GetNewPathWithIndex($@"{shellPath}\{keyName}", ObjectPath.PathType.Registry);

                        RegistryEx.CopyTo(srcPath, dstPath);
                        AddItem(new ShellItem(dstPath));
                    }
                }
            };

            btnEnhanceMenu.MouseDown += (sender, e) =>
            {
                string tempPath1 = Path.GetTempFileName();
                string tempPath2 = Path.GetTempFileName();
                ExternalProgram.ExportRegistry(scenePath, tempPath1);
                using (EnhanceMenusDialog dlg = new EnhanceMenusDialog())
                {
                    dlg.ScenePath = scenePath;
                    dlg.ShowDialog();
                }
                ExternalProgram.ExportRegistry(scenePath, tempPath2);
                string str1 = File.ReadAllText(tempPath1);
                string str2 = File.ReadAllText(tempPath2);
                File.Delete(tempPath1);
                File.Delete(tempPath2);
                if (!str1.Equals(str2))
                {
                    MainForm mainForm = (MainForm)FindForm();
                    mainForm.JumpItem(mainForm.ToolBar.SelectedIndex, mainForm.SideBar.SelectedIndex);
                }
            };
        }

        private void AddNewShellItem(string scenePath)
        {
            string shellPath = GetShellPath(scenePath);
            using (NewShellDialog dlg = new NewShellDialog())
            {
                dlg.ScenePath = scenePath;
                dlg.ShellPath = shellPath;
                if (dlg.ShowDialog() != DialogResult.OK) return;
                for (int i = 0; i < Controls.Count; i++)
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
        }

        private void AddNewShellExItem(string scenePath)
        {
            bool isDragDrop = Scene == Scenes.DragDrop;
            using (InputDialog dlg1 = new InputDialog { Title = AppString.Dialog.InputGuid })
            {
                if (GuidEx.TryParse(Clipboard.GetText(), out Guid guid)) dlg1.Text = guid.ToString();
                if (dlg1.ShowDialog() != DialogResult.OK) return;
                if (GuidEx.TryParse(dlg1.Text, out guid))
                {
                    if (isDragDrop)
                    {
                        using (SelectDialog dlg2 = new SelectDialog())
                        {
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
                    }
                    string shellExPath = GetShellExPath(scenePath);
                    if (ShellExItem.GetPathAndGuids(shellExPath, isDragDrop).Values.Contains(guid))
                    {
                        AppMessageBox.Show(AppString.Message.HasBeenAdded);
                    }
                    else
                    {
                        string part = isDragDrop ? ShellExItem.DdhParts[0] : ShellExItem.CmhParts[0];
                        string regPath = $@"{shellExPath}\{part}\{guid:B}";
                        Registry.SetValue(regPath, "", guid.ToString("B"));
                        ShellExItem item = new ShellExItem(guid, regPath);
                        for (int i = 0; i < Controls.Count; i++)
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
        }

        sealed class SwitchContextMenuStyleItem : MyListItem
        {
            const string registryKeyPath = @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}";
            const string registrySubKeyPath = @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32";

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
                RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(registryKeyPath);
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
                        RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(registryKeyPath, true);
                        if (registryKey != null)
                        {
                            Registry.CurrentUser.DeleteSubKeyTree(registryKeyPath);
                        }
                    }
                    else
                    {
                        // 切换Win10经典右键菜单样式
                        // 添加注册表项目：HKEY_CURRENT_USER\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32
                        RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(registrySubKeyPath);
                        if (registryKey != null)
                        {
                            registryKey.SetValue(null, "", RegistryValueKind.String);
                        }
                    }
                    ExplorerRestarter.Show();
                }
            }

            readonly RComboBox cmbDic = new RComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 180.DpiZoom()
            };
        }
    }
}