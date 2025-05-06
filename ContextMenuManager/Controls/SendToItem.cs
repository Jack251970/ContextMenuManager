using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Controls.Interfaces;
using ContextMenuManager.Methods;
using Microsoft.Win32;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    sealed class SendToItem : MyListItem, IChkVisibleItem, IBtnShowMenuItem, ITsiTextItem, ITsiAdministratorItem,
        ITsiIconItem, ITsiWebSearchItem, ITsiFilePathItem, ITsiDeleteItem, ITsiShortcutCommandItem
    {
        public SendToItem(string filePath)
        {
            InitializeComponents();
            FilePath = filePath;
        }

        private string filePath;
        public string FilePath
        {
            get => filePath;
            set
            {
                filePath = value;
                if(IsShortcut) ShellLink = new ShellLink(value);
                Text = ItemText;
                Image = ItemImage;
            }
        }

        public ShellLink ShellLink { get; private set; }
        private string FileExtension => Path.GetExtension(FilePath);
        private bool IsShortcut => FileExtension.ToLower() == ".lnk";
        public string SearchText => $"{AppString.SideBar.SendTo} {Text}";
        private Image ItemImage => ItemIcon?.ToBitmap() ?? AppImage.NotFound;
        public string ItemFileName => Path.GetFileName(ItemFilePath);

        public string ItemFilePath
        {
            get
            {
                string path = null;
                if(IsShortcut) path = ShellLink.TargetPath;
                else
                {
                    using(RegistryKey root = Registry.ClassesRoot)
                    using(RegistryKey extKey = root.OpenSubKey(FileExtension))
                    {
                        string guidPath = extKey?.GetValue("")?.ToString();
                        if(!string.IsNullOrEmpty(guidPath))
                        {
                            using(RegistryKey ipsKey = root.OpenSubKey($@"{guidPath}\InProcServer32"))
                            {
                                path = ipsKey?.GetValue("")?.ToString();
                            }
                        }
                    }
                }
                if(!File.Exists(path) && !Directory.Exists(path)) path = FilePath;
                return path;
            }
        }

        public bool ItemVisible
        {
            get => (File.GetAttributes(FilePath) & FileAttributes.Hidden) != FileAttributes.Hidden;
            set
            {
                FileAttributes attributes = File.GetAttributes(FilePath);
                if(value) attributes &= ~FileAttributes.Hidden;
                else attributes |= FileAttributes.Hidden;
                File.SetAttributes(FilePath, attributes);
            }
        }

        public string ItemText
        {
            get
            {
                string name = DesktopIni.GetLocalizedFileNames(FilePath, true);
                if(name == string.Empty) name = Path.GetFileNameWithoutExtension(FilePath);
                if(name == string.Empty) name = FileExtension;
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
                Icon icon = ResourceIcon.GetIcon(IconLocation, out string iconPath, out int iconIndex);
                IconPath = iconPath; IconIndex = iconIndex;
                if(icon != null) return icon;
                if(IsShortcut)
                {
                    string path = ItemFilePath;
                    if(File.Exists(path)) icon = ResourceIcon.GetExtensionIcon(path);
                    else if(Directory.Exists(path)) icon = ResourceIcon.GetFolderIcon(path);
                }
                if(icon == null) icon = ResourceIcon.GetExtensionIcon(FileExtension);
                return icon;
            }
        }

        public string IconLocation
        {
            get
            {
                string location = null;
                if(IsShortcut)
                {
                    ShellLink.ICONLOCATION iconLocation = ShellLink.IconLocation;
                    string iconPath = iconLocation.IconPath;
                    int iconIndex = iconLocation.IconIndex;
                    if(string.IsNullOrEmpty(iconPath)) iconPath = ShellLink.TargetPath;
                    location = $@"{iconPath},{iconIndex}";
                }
                else
                {
                    using(RegistryKey root = Registry.ClassesRoot)
                    using(RegistryKey extensionKey = root.OpenSubKey(FileExtension))
                    {
                        // 检查extensionKey是否为null
                        if (extensionKey != null)
                        {
                            string guidPath = extensionKey.GetValue("")?.ToString();
                            if (!string.IsNullOrEmpty(guidPath))
                            {
                                using(RegistryKey guidKey = root.OpenSubKey($@"{guidPath}\DefaultIcon"))
                                {
                                    // 检查guidKey是否为null
                                    if (guidKey != null)
                                    {
                                        location = guidKey.GetValue("")?.ToString();
                                    }
                                }
                            }
                        }
                    }
                }
                return location;
            }
            set
            {
                if(IsShortcut)
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
                    using(RegistryKey root = Registry.ClassesRoot)
                    using(RegistryKey extensionKey = root.OpenSubKey(FileExtension))
                    {
                        string guidPath = extensionKey.GetValue("")?.ToString();
                        if(guidPath != null)
                        {
                            string regPath = $@"{root.Name}\{guidPath}\DefaultIcon";
                            RegTrustedInstaller.TakeRegTreeOwnerShip(regPath);
                            Registry.SetValue(regPath, "", value);
                            ExplorerRestarter.Show();
                        }
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

        readonly RToolStripMenuItem TsiDetails = new RToolStripMenuItem(AppString.Menu.Details);

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

            ContextMenuStrip.Items.AddRange(new ToolStripItem[] { TsiChangeText, new RToolStripSeparator(),
                TsiChangeIcon, new RToolStripSeparator(), TsiAdministrator, new RToolStripSeparator(),
                TsiDetails, new RToolStripSeparator(), TsiDeleteMe });

            TsiDetails.DropDownItems.AddRange(new ToolStripItem[] { TsiSearch, new RToolStripSeparator(),
                TsiChangeCommand, TsiFileProperties, TsiFileLocation });

            ContextMenuStrip.Opening += (sender, e) => TsiChangeCommand.Visible = IsShortcut;

            TsiChangeCommand.Click += (sender, e) =>
            {
                if(TsiChangeCommand.ChangeCommand(ShellLink))
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