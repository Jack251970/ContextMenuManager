using ContextMenuManager.Methods;
using ContextMenuManager.Controls.Interfaces;
using Microsoft.Win32;
using System.Drawing;
using System.IO;
using System.Windows.Controls;
using System;

namespace ContextMenuManager.Controls
{
    internal sealed class SendToItem : MyListItem, IChkVisibleItem, IBtnShowMenuItem, ITsiTextItem, ITsiAdministratorItem,
        ITsiIconItem, ITsiWebSearchItem, ITsiFilePathItem, ITsiDeleteItem, ITsiShortcutCommandItem
    {
        public SendToItem(MyList list, string filePath) : base(list)
        {
            if (list != null) InitializeComponents();
            FilePath = filePath;
        }

        private string filePath;
        public string FilePath
        {
            get => filePath;
            set
            {
                filePath = value;
                if (IsShortcut) ShellLink = new ShellLink(value);
                Text = ItemText;
                if (List != null) Image = ItemImage;
            }
        }

        public ContextMenu ContextMenu
        {
            get => Control.ContextMenu;
            set => Control.ContextMenu = value;
        }

        public ShellLink ShellLink { get; private set; }
        private string FileExtension => Path.GetExtension(FilePath);
        private bool IsShortcut => FileExtension.Equals(".lnk", StringComparison.CurrentCultureIgnoreCase);
        public string SearchText => $"{AppString.SideBar.SendTo} {Text}";
        private System.Drawing.Image ItemImage => ItemIcon?.ToBitmap() ?? AppImage.NotFound;
        public string ItemFileName => Path.GetFileName(ItemFilePath);

        public string ItemFilePath
        {
            get
            {
                string path = null;
                if (IsShortcut) path = ShellLink.TargetPath;
                else
                {
                    using var root = Registry.ClassesRoot;
                    using var extKey = root.OpenSubKey(FileExtension);
                    var guidPath = extKey?.GetValue("")?.ToString();
                    if (!string.IsNullOrEmpty(guidPath))
                    {
                        using var ipsKey = root.OpenSubKey($@"{guidPath}\InProcServer32");
                        path = ipsKey?.GetValue("")?.ToString();
                    }
                }
                if (!File.Exists(path) && !Directory.Exists(path)) path = FilePath;
                return path;
            }
        }

        public bool ItemVisible
        {
            get => (File.GetAttributes(FilePath) & FileAttributes.Hidden) != FileAttributes.Hidden;
            set
            {
                var attributes = File.GetAttributes(FilePath);
                if (value) attributes &= ~FileAttributes.Hidden;
                else attributes |= FileAttributes.Hidden;
                File.SetAttributes(FilePath, attributes);
            }
        }

        public string ItemText
        {
            get
            {
                var name = DesktopIni.GetLocalizedFileNames(FilePath, true);
                if (name == string.Empty) name = Path.GetFileNameWithoutExtension(FilePath);
                if (name == string.Empty) name = FileExtension;
                return name;
            }
            set
            {
                DesktopIni.SetLocalizedFileNames(FilePath, value);
                Text = ResourceString.GetDirectString(value);
                ExplorerRestarter.Show();
            }
        }

        public Icon ItemIcon
        {
            get
            {
                var icon = ResourceIcon.GetIcon(IconLocation, out var iconPath, out var iconIndex);
                IconPath = iconPath; IconIndex = iconIndex;
                if (icon != null) return icon;
                if (IsShortcut)
                {
                    var path = ItemFilePath;
                    if (File.Exists(path)) icon = ResourceIcon.GetExtensionIcon(path);
                    else if (Directory.Exists(path)) icon = ResourceIcon.GetFolderIcon(path);
                }
                if (icon == null) icon = ResourceIcon.GetExtensionIcon(FileExtension);
                return icon;
            }
        }

        public string IconLocation
        {
            get
            {
                string location = null;
                if (IsShortcut)
                {
                    var iconLocation = ShellLink.IconLocation;
                    var iconPath = iconLocation.IconPath;
                    var iconIndex = iconLocation.IconIndex;
                    if (string.IsNullOrEmpty(iconPath)) iconPath = ShellLink.TargetPath;
                    location = $@"{iconPath},{iconIndex}";
                }
                else
                {
                    using var root = Registry.ClassesRoot;
                    using var extensionKey = root.OpenSubKey(FileExtension);
                    // 检查extensionKey是否为null
                    if (extensionKey != null)
                    {
                        var guidPath = extensionKey.GetValue("")?.ToString();
                        if (!string.IsNullOrEmpty(guidPath))
                        {
                            using var guidKey = root.OpenSubKey($@"{guidPath}\DefaultIcon");
                            // 检查guidKey是否为null
                            if (guidKey != null)
                            {
                                location = guidKey.GetValue("")?.ToString();
                            }
                        }
                    }
                }
                return location;
            }
            set
            {
                if (IsShortcut)
                {
                    ShellLink.IconLocation = new ShellLink.ICONLOCATION
                    {
                        IconPath = IconPath,
                        IconIndex = IconIndex
                    };
                    ShellLink.Save();
                }
                else
                {
                    using var root = Registry.ClassesRoot;
                    using var extensionKey = root.OpenSubKey(FileExtension);
                    var guidPath = extensionKey.GetValue("")?.ToString();
                    if (guidPath != null)
                    {
                        var regPath = $@"{root.Name}\{guidPath}\DefaultIcon";
                        RegTrustedInstaller.TakeRegTreeOwnerShip(regPath);
                        Registry.SetValue(regPath, "", value);
                        ExplorerRestarter.Show();
                    }
                }
            }
        }

        public string IconPath { get; set; }
        public int IconIndex { get; set; }

        public VisibleCheckBox ChkVisible { get; set; }
        public MenuButton BtnShowMenu { get; set; }
        public ChangeTextMenuItem TsiChangeText { get; set; }
        public ChangeIconMenuItem TsiChangeIcon { get; set; }
        public WebSearchMenuItem TsiSearch { get; set; }
        public FilePropertiesMenuItem TsiFileProperties { get; set; }
        public FileLocationMenuItem TsiFileLocation { get; set; }
        public DeleteMeMenuItem TsiDeleteMe { get; set; }
        public ShortcutCommandMenuItem TsiChangeCommand { get; set; }
        public RunAsAdministratorItem TsiAdministrator { get; set; }

        private readonly RToolStripMenuItem TsiDetails = new(AppString.Menu.Details);

        private void InitializeComponents()
        {
            BtnShowMenu = new MenuButton(this);
            ChkVisible = new VisibleCheckBox(this);
            TsiChangeText = new ChangeTextMenuItem(this);
            TsiChangeIcon = new ChangeIconMenuItem(this);
            TsiChangeCommand = new ShortcutCommandMenuItem(this);
            TsiAdministrator = new RunAsAdministratorItem(this);
            TsiSearch = new WebSearchMenuItem(this);
            TsiFileLocation = new FileLocationMenuItem(this);
            TsiFileProperties = new FilePropertiesMenuItem(this);
            TsiDeleteMe = new DeleteMeMenuItem(this);

            foreach (var item in new Control[] { TsiChangeText, new RToolStripSeparator(),
                TsiChangeIcon, new RToolStripSeparator(), TsiAdministrator, new RToolStripSeparator(),
                TsiDetails, new RToolStripSeparator(), TsiDeleteMe })
            {
                Control.ContextMenu.Items.Add(item);
            }

            foreach (var item in new Control[] { TsiSearch, new RToolStripSeparator(),
                TsiChangeCommand, TsiFileProperties, TsiFileLocation })
            {
                TsiDetails.Items.Add(item);
            }

            Control.ContextMenu.Opened += (sender, e) => TsiChangeCommand.Visible = IsShortcut;

            TsiChangeCommand.Click += (sender, e) =>
            {
                if (TsiChangeCommand.ChangeCommand(ShellLink))
                {
                    Image = ItemImage;
                }
            };
        }

        public void DeleteMe()
        {
            File.Delete(FilePath);
            DesktopIni.DeleteLocalizedFileNames(FilePath);
            ShellLink?.Dispose();
        }
    }
}