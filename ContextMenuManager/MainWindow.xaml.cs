using ContextMenuManager.Controls;
using ContextMenuManager.Methods;
using ContextMenuManager.Properties;
using ContextMenuManager.Views;
using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Controls.Helpers;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DrawingSize = System.Drawing.Size;

namespace ContextMenuManager
{
    public partial class MainWindow : Window
    {
        public static readonly string DefaultText = $"Ver: {InfoHelper.ProductVersion}    {InfoHelper.CompanyName}";

        private ShellList ShellList { get => field ??= new(); }
        private ShellNewList ShellNewList { get => field ??= new(); }
        private SendToList SendToList { get => field ??= new(); }
        private OpenWithList OpenWithList { get => field ??= new(); }
        private WinXList WinXList { get => field ??= new(); }
        private EnhanceMenuList EnhanceMenusList { get => field ??= new(); }
        private DetailedEditList DetailedEditList { get => field ??= new(); }
        private GuidBlockedList GuidBlockedList { get => field ??= new(); }
        private IEList IEList { get => field ??= new(); }
        private AppSettingView AppSettingView { get => field ??= new(); }
        private LanguagesView LanguagesView { get => field ??= new(); }
        private BackupView BackupView { get => field ??= new(); }
        private DictionariesView DictionariesView { get => field ??= new(); }
        private AboutAppView AboutAppView { get => field ??= new(); }
        private DonateView DonateView { get => field ??= new(); }

        private TextBox SearchBox { get; set; }

        private UIElement currentListControl;
        private string currentTag;
        private int selectedToolBarIndex = 0;

        private readonly SearchService searchService = new();

        // Toolbar button data: (label, icon glyph, tag prefix)
        private readonly (string label, string glyph, int tagIndex)[] toolbarItems = new[]
        {
            ("", "\uE80F", 0),   // Home
            ("", "\uE8A9", 1),   // Type
            ("", "\uE90F", 2),   // Rule
            ("", "\uE72C", 3),   // Refresh (not selectable)
            ("", "\uE946", 4),   // About
        };

        // Sidebar items per toolbar tab (null = separator)
        private static readonly string[] GeneralItems =
        {
            null, // placeholder for index alignment
            null, // placeholder
            null, // placeholder
            null, // placeholder
            null, // placeholder
            null, // placeholder
            null, // placeholder
            null, // placeholder
            null, // placeholder
            null, // placeholder
            null, // separator
            null, // New
            null, // SendTo
            null, // OpenWith
            null, // separator
            null, // WinX
        };

        private static readonly string[] TypeItems =
        {
            null, // LnkFile
            null, // UwpLnk
            null, // ExeFile
            null, // UnknownType
            null, // separator
            null, // CustomExtension
            null, // PerceivedType
            null, // DirectoryType
            null, // separator
            null, // MenuAnalysis
        };

        private static readonly string[] OtherRuleItems =
        {
            null, // EnhanceMenu
            null, // DetailedEdit
            null, // separator
            null, // DragDrop
            null, // PublicReferences
            null, // IEMenu
            null, // separator
            null, // GuidBlocked
            null, // CustomRegPath
        };

        private static readonly string[] AboutItems =
        {
            null, // AppSetting
            null, // AppLanguage
            null, // BackupRestore
            null, // Dictionaries
            null, // AboutApp
            null, // Donate
        };

        private readonly int[] lastItemIndex = new int[5];

        public MainWindow()
        {
            InitializeComponent();

            // 初始化搜索框
            SearchBox = new TextBox
            {
                Width = 200,
                Height = 28,
                Tag = AppString.Other.SearchContent ?? "Search..."
            };

            Title = AppString.General.AppName ?? "ContextMenuManager";
            Icon = AppResources.Logo.ToBitmapSource();
            AppSettingView.OwnerWindow = this;
            LanguagesView.OwnerWindow = this;
            BackupView.OwnerWindow = this;

            // Restore saved window size
            var savedSize = AppConfig.MainWindowSize;
            if (savedSize.Width >= 680 && savedSize.Height >= 450)
            {
                Width = savedSize.Width;
                Height = savedSize.Height;
            }
            Topmost = AppConfig.TopMost;

            BuildToolBar();
            SwitchTab();

            // First-run language download prompt
            Loaded += (_, _) => FirstRunDownloadLanguage();
        }

        private void BuildToolBar()
        {
            var items = new (string label, string glyph)[]
            {
                (AppString.ToolBar.Home ?? "Home", "\uE80F"),
                (AppString.ToolBar.Type ?? "Type", "\uE8A9"),
                (AppString.ToolBar.Rule ?? "Rule", "\uE90F"),
                (AppString.ToolBar.Refresh ?? "Refresh", "\uE72C"),
                (AppString.ToolBar.About ?? "About", "\uE946"),
            };

            var buttons = new AppBarToggleButton[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                var (label, glyph) = items[i];

                var button = new AppBarToggleButton
                {
                    Icon = new FontIcon
                    {
                        Glyph = glyph,
                        
                        FontSize = 30
                    },
                    Label = label,
                    Tag = i
                };

                buttons[i] = button;
            }

            for (int i = 0; i < buttons.Length; i++)
            {
                ToolBar.AddButton(buttons[i], i != 3);
            }

            // 刷新按钮不可选中
            buttons[3].MouseDown += (_, e) =>
            {
                if (e.ChangedButton == MouseButton.Left)
                    RefreshApp();
            };

            // 搜索框
            ToolBar.AddSearchBox(SearchBox);

            // 切换标签时显示/隐藏搜索框 + 切换侧边栏
            ToolBar.SelectedButtonChanged += (sender, e) =>
            {
                ControlHelper.SetPlaceholderText(SearchBox, AppString.Other.SearchContent ?? "Search...");
                SearchBox.Text = string.Empty;
                SearchBox.Visibility = ToolBar.SelectedIndex == 4
                    ? Visibility.Collapsed
                    : Visibility.Visible;
                OnToolBarSelectionChanged(sender, e);
            };

            SearchBox.TextChanged += SearchBox_TextChanged;
            SearchBox.Visibility = Visibility.Visible;

            ToolBar.SelectedIndex = selectedToolBarIndex;
        }

        private void OnToolBarSelectionChanged(object sender, EventArgs e)
        {
            var idx = ToolBar.SelectedIndex;
            if (idx >= 0 && idx != selectedToolBarIndex)
            {
                selectedToolBarIndex = idx;
                SwitchTab();
            }
        }

        private void SwitchTab()
        {
            SideBar.Items.Clear();
            string[] items;
            switch (selectedToolBarIndex)
            {
                case 0: items = GetGeneralItems(); break;
                case 1: items = GetTypeItems(); break;
                case 2: items = GetOtherRuleItems(); break;
                case 4: items = GetAboutItems(); break;
                default: return;
            }

            foreach (var item in items)
            {
                if (item == null)
                {
                    // Separator
                    var sep = new Separator();
                    SideBar.Items.Add(sep);
                }
                else
                {
                    SideBar.Items.Add(item);
                }
            }

            SideBar.SelectedIndex = Math.Min(lastItemIndex[selectedToolBarIndex], SideBar.Items.Count - 1);
        }

        private string[] GetGeneralItems()
        {
            return new[]
            {
                AppString.SideBar.File ?? "File",
                AppString.SideBar.Folder ?? "Folder",
                AppString.SideBar.Directory ?? "Directory",
                AppString.SideBar.Background ?? "Background",
                AppString.SideBar.Desktop ?? "Desktop",
                AppString.SideBar.Drive ?? "Drive",
                AppString.SideBar.AllObjects ?? "All Objects",
                AppString.SideBar.Computer ?? "Computer",
                AppString.SideBar.RecycleBin ?? "Recycle Bin",
                AppString.SideBar.Library ?? "Library",
                null,
                AppString.SideBar.New ?? "New",
                AppString.SideBar.SendTo ?? "Send To",
                AppString.SideBar.OpenWith ?? "Open With",
                null,
                AppString.SideBar.WinX ?? "WinX",
            };
        }

        private string[] GetTypeItems()
        {
            return new[]
            {
                AppString.SideBar.LnkFile ?? "Lnk File",
                AppString.SideBar.UwpLnk ?? "UWP Lnk",
                AppString.SideBar.ExeFile ?? "Exe File",
                AppString.SideBar.UnknownType ?? "Unknown Type",
                null,
                AppString.SideBar.CustomExtension ?? "Custom Extension",
                AppString.SideBar.PerceivedType ?? "Perceived Type",
                AppString.SideBar.DirectoryType ?? "Directory Type",
                null,
                AppString.SideBar.MenuAnalysis ?? "Menu Analysis",
            };
        }

        private string[] GetOtherRuleItems()
        {
            return new[]
            {
                AppString.SideBar.EnhanceMenu ?? "Enhance Menu",
                AppString.SideBar.DetailedEdit ?? "Detailed Edit",
                null,
                AppString.SideBar.DragDrop ?? "Drag Drop",
                AppString.SideBar.PublicReferences ?? "Public References",
                AppString.SideBar.IEMenu ?? "IE Menu",
                null,
                AppString.SideBar.GuidBlocked ?? "GUID Blocked",
                AppString.SideBar.CustomRegPath ?? "Custom Reg Path",
            };
        }

        private string[] GetAboutItems()
        {
            return new[]
            {
                AppString.SideBar.AppSetting ?? "Settings",
                AppString.SideBar.AppLanguage ?? "Language",
                AppString.SideBar.BackupRestore ?? "Backup",
                AppString.SideBar.Dictionaries ?? "Dictionaries",
                AppString.SideBar.AboutApp ?? "About",
                AppString.SideBar.Donate ?? "Donate",
            };
        }

        private void SideBar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SideBar.SelectedIndex < 0) return;
            SwitchItem();
        }

        private void SwitchItem()
        {
            if (currentListControl is MyList myList)
            {
                myList.ClearItems();
            }
            currentListControl = null;
            WpfContentHost.Content = null;
            WpfContentHost.Visibility = Visibility.Collapsed;

            lastItemIndex[selectedToolBarIndex] = SideBar.SelectedIndex;

            switch (selectedToolBarIndex)
            {
                case 0: SwitchGeneralItem(); break;
                case 1: SwitchTypeItem(); break;
                case 2: SwitchOtherRuleItem(); break;
                case 4: SwitchAboutItem(); break;
            }

            UpdateStatusText(GetStatusText(currentTag));
        }

        private void SwitchGeneralItem()
        {
            var idx = SideBar.SelectedIndex;
            var scenes = new[]
            {
                Scenes.File, Scenes.Folder, Scenes.Directory, Scenes.Background,
                Scenes.Desktop, Scenes.Drive, Scenes.AllObjects, Scenes.Computer,
                Scenes.RecycleBin, Scenes.Library,
            };

            if (idx >= 0 && idx < scenes.Length)
            {
                ShellList.Scene = scenes[idx];
                ShellList.LoadItems();
                ShowControl(ShellList);
                currentTag = $"shell_{scenes[idx]}";
                return;
            }

            // New=11, SendTo=12, OpenWith=13, WinX=15
            switch (idx)
            {
                case 11: ShellNewList.LoadItems(); ShowControl(ShellNewList); currentTag = "shell_new"; break;
                case 12: SendToList.LoadItems(); ShowControl(SendToList); currentTag = "shell_sendto"; break;
                case 13: OpenWithList.LoadItems(); ShowControl(OpenWithList); currentTag = "shell_openwith"; break;
                case 15: WinXList.LoadItems(); ShowControl(WinXList); currentTag = "shell_winx"; break;
            }
        }

        private void SwitchTypeItem()
        {
            var typeScenes = new Scenes?[]
            {
                Scenes.LnkFile, Scenes.UwpLnk, Scenes.ExeFile, Scenes.UnknownType,
                null,
                Scenes.CustomExtension, Scenes.PerceivedType, Scenes.DirectoryType,
                null,
                Scenes.MenuAnalysis,
            };

            var idx = SideBar.SelectedIndex;
            if (idx >= 0 && idx < typeScenes.Length && typeScenes[idx].HasValue)
            {
                ShellList.Scene = typeScenes[idx].Value;
                ShellList.LoadItems();
                ShowControl(ShellList);
                currentTag = $"type_{typeScenes[idx].Value}";
            }
        }

        private void SwitchOtherRuleItem()
        {
            switch (SideBar.SelectedIndex)
            {
                case 0:
                    EnhanceMenusList.ScenePath = null;
                    EnhanceMenusList.LoadItems();
                    ShowControl(EnhanceMenusList);
                    currentTag = "rule_enhance";
                    break;
                case 1:
                    DetailedEditList.GroupGuid = Guid.Empty;
                    DetailedEditList.LoadItems();
                    ShowControl(DetailedEditList);
                    currentTag = "rule_detailed";
                    break;
                case 3:
                    ShellList.Scene = Scenes.DragDrop;
                    ShellList.LoadItems();
                    ShowControl(ShellList);
                    currentTag = "rule_dragdrop";
                    break;
                case 4:
                    ShellList.Scene = Scenes.PublicReferences;
                    ShellList.LoadItems();
                    ShowControl(ShellList);
                    currentTag = "rule_public";
                    break;
                case 5:
                    IEList.LoadItems();
                    ShowControl(IEList);
                    currentTag = "rule_ie";
                    break;
                case 7:
                    GuidBlockedList.LoadItems();
                    ShowControl(GuidBlockedList);
                    currentTag = "rule_guid";
                    break;
                case 8:
                    ShellList.Scene = Scenes.CustomRegPath;
                    ShellList.LoadItems();
                    ShowControl(ShellList);
                    currentTag = "rule_customreg";
                    break;
            }
        }

        private void SwitchAboutItem()
        {
            switch (SideBar.SelectedIndex)
            {
                case 0:
                    AppSettingView.RefreshFromConfig();
                    ShowControl(AppSettingView);
                    currentTag = "about_settings";
                    break;
                case 1:
                    LanguagesView.LoadLanguages();
                    ShowControl(LanguagesView);
                    currentTag = "about_language";
                    break;
                case 2:
                    BackupView.LoadItems();
                    ShowControl(BackupView);
                    currentTag = "about_backup";
                    break;
                case 3:
                    DictionariesView.LoadText();
                    ShowControl(DictionariesView);
                    currentTag = "about_dict";
                    break;
                case 4:
                    AboutAppView.RefreshContent();
                    ShowControl(AboutAppView);
                    currentTag = "about_app";
                    break;
                case 5:
                    DonateView.RefreshContent();
                    ShowControl(DonateView);
                    currentTag = "about_donate";
                    break;
            }
        }

        private void ShowControl(UIElement ctrl)
        {
            WpfContentHost.Content = ctrl;
            WpfContentHost.Visibility = Visibility.Visible;
            currentListControl = ctrl;
            if (ctrl is not MyList) UpdateStatusText(DefaultText);
        }

        private void RefreshApp()
        {
            ObjectPath.ClearFilePathDic();
            AppConfig.ReloadConfig();
            GuidInfo.ReloadDics();
            XmlDicHelper.ReloadDics();
            SwitchItem();
        }

        // Search

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterItems(SearchBox.Text);
        }

        private void FilterItems(string filterText)
        {
            if (currentListControl is not MyList myList) return;

            if (string.IsNullOrWhiteSpace(filterText))
            {
                RestoreOriginalListItems();
                UpdateStatusText(GetStatusText(currentTag));
                return;
            }

            if (!searchService.HasOriginalItems)
            {
                var items = myList.Controls.Select(c => c.Item).ToList();
                searchService.Initialize(items);
            }

            var searchResults = searchService.Search(filterText);

            myList.ClearItems();
            foreach (var result in searchResults)
            {
                myList.AddItem(result.Item);
            }

            UpdateSearchStatus(searchResults.Count);
        }

        private void RestoreOriginalListItems()
        {
            if (currentListControl is MyList myList && searchService.HasOriginalItems)
            {
                myList.ClearItems();
                foreach (var item in searchService.GetOriginalItems())
                {
                    myList.AddItem(item);
                }
                searchService.Clear();
            }
        }

        private void UpdateSearchStatus(int visibleCount)
        {
            if (string.IsNullOrEmpty(SearchBox.Text))
            {
                UpdateStatusText(GetStatusText(currentTag));
            }
            else
            {
                var statusMsg = AppString.Other.StatusSearch ?? "Search '%searchText' - Find %visibleCount items (%totalCount items in total)";
                statusMsg = statusMsg.Replace("%searchText", SearchBox.Text)
                    .Replace("%visibleCount", visibleCount.ToString())
                    .Replace("%totalCount", searchService.TotalItemCount.ToString());
                UpdateStatusText(statusMsg);
            }
        }

        // Status bar

        internal static string GetStatusText(Scenes scene)
        {
            return scene switch
            {
                Scenes.File => AppString.StatusBar.File,
                Scenes.Folder => AppString.StatusBar.Folder,
                Scenes.Directory => AppString.StatusBar.Directory,
                Scenes.Background => AppString.StatusBar.Background,
                Scenes.Desktop => AppString.StatusBar.Desktop,
                Scenes.Drive => AppString.StatusBar.Drive,
                Scenes.AllObjects => AppString.StatusBar.AllObjects,
                Scenes.Computer => AppString.StatusBar.Computer,
                Scenes.RecycleBin => AppString.StatusBar.RecycleBin,
                Scenes.Library => AppString.StatusBar.Library,
                Scenes.New => AppString.StatusBar.New,
                Scenes.SendTo => AppString.StatusBar.SendTo,
                Scenes.OpenWith => AppString.StatusBar.OpenWith,
                Scenes.WinX => AppString.StatusBar.WinX,
                Scenes.LnkFile => AppString.StatusBar.LnkFile,
                Scenes.UwpLnk => AppString.StatusBar.UwpLnk,
                Scenes.ExeFile => AppString.StatusBar.ExeFile,
                Scenes.UnknownType => AppString.StatusBar.UnknownType,
                Scenes.CustomExtension => AppString.StatusBar.CustomExtension,
                Scenes.PerceivedType => AppString.StatusBar.PerceivedType,
                Scenes.DirectoryType => AppString.StatusBar.DirectoryType,
                Scenes.MenuAnalysis => AppString.StatusBar.MenuAnalysis,
                Scenes.EnhanceMenu => AppString.StatusBar.EnhanceMenu,
                Scenes.DetailedEdit => AppString.StatusBar.DetailedEdit,
                Scenes.DragDrop => AppString.StatusBar.DragDrop,
                Scenes.PublicReferences => AppString.StatusBar.PublicReferences,
                Scenes.InternetExplorer => AppString.StatusBar.IEMenu,
                Scenes.GuidBlocked => AppString.StatusBar.GuidBlocked,
                Scenes.CustomRegPath => AppString.StatusBar.CustomRegPath,
                _ => null
            } ?? throw new ArgumentException("Unsupported scene for GetStatusText", nameof(scene));
        }

        internal static string GetStatusText(string tag)
        {
            if (tag == null) return DefaultText;
            return tag switch
            {
                "shell_file" => GetStatusText(Scenes.File),
                "shell_folder" => GetStatusText(Scenes.Folder),
                "shell_directory" => GetStatusText(Scenes.Directory),
                "shell_background" => GetStatusText(Scenes.Background),
                "shell_desktop" => GetStatusText(Scenes.Desktop),
                "shell_drive" => GetStatusText(Scenes.Drive),
                "shell_allobjects" => GetStatusText(Scenes.AllObjects),
                "shell_computer" => GetStatusText(Scenes.Computer),
                "shell_recyclebin" => GetStatusText(Scenes.RecycleBin),
                "shell_library" => GetStatusText(Scenes.Library),
                "shell_new" => GetStatusText(Scenes.New),
                "shell_sendto" => GetStatusText(Scenes.SendTo),
                "shell_openwith" => GetStatusText(Scenes.OpenWith),
                "shell_winx" => GetStatusText(Scenes.WinX),
                "type_lnk" => GetStatusText(Scenes.LnkFile),
                "type_uwplnk" => GetStatusText(Scenes.UwpLnk),
                "type_exe" => GetStatusText(Scenes.ExeFile),
                "type_unknown" => GetStatusText(Scenes.UnknownType),
                "type_custom" => GetStatusText(Scenes.CustomExtension),
                "type_perceived" => GetStatusText(Scenes.PerceivedType),
                "type_directory" => GetStatusText(Scenes.DirectoryType),
                "type_menuanalysis" => GetStatusText(Scenes.MenuAnalysis),
                "rule_enhance" => GetStatusText(Scenes.EnhanceMenu),
                "rule_detailed" => GetStatusText(Scenes.DetailedEdit),
                "rule_dragdrop" => GetStatusText(Scenes.DragDrop),
                "rule_public" => GetStatusText(Scenes.PublicReferences),
                "rule_ie" => GetStatusText(Scenes.InternetExplorer),
                "rule_guid" => GetStatusText(Scenes.GuidBlocked),
                "rule_customreg" => GetStatusText(Scenes.CustomRegPath),
                "about_settings" => DefaultText,
                "about_language" => DefaultText,
                "about_backup" => DefaultText,
                "about_dict" => DefaultText,
                "about_app" => DefaultText,
                "about_donate" => DefaultText,
                _ => DefaultText
            };
        }

        private void UpdateStatusText(string text)
        {
            StatusText.Text = text;
        }

        // Window events

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (ExplorerRestarter.IsPendingRestart)
            {
                var result = AppMessageBox.Show(
                    AppString.Other.RestartExplorer,
                    AppString.General.AppName,
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Question);
                if (result == MessageBoxResult.OK)
                {
                    ExternalProgram.RestartExplorer();
                    ExplorerRestarter.Hide();
                }
            }

            AppConfig.MainWindowSize = new DrawingSize((int)Width, (int)Height);
            Opacity = 0;
        }

        internal void JumpToScene(Scenes scene)
        {
            var tag = scene switch
            {
                Scenes.File => "shell_file",
                Scenes.Folder => "shell_folder",
                Scenes.Directory => "shell_directory",
                Scenes.Background => "shell_background",
                Scenes.Desktop => "shell_desktop",
                Scenes.Drive => "shell_drive",
                Scenes.AllObjects => "shell_allobjects",
                Scenes.Computer => "shell_computer",
                Scenes.RecycleBin => "shell_recyclebin",
                Scenes.Library => "shell_library",
                Scenes.New => "shell_new",
                Scenes.SendTo => "shell_sendto",
                Scenes.OpenWith => "shell_openwith",
                Scenes.WinX => "shell_winx",
                Scenes.LnkFile => "type_lnk",
                Scenes.UwpLnk => "type_uwplnk",
                Scenes.ExeFile => "type_exe",
                Scenes.UnknownType => "type_unknown",
                Scenes.CustomExtension => "type_custom",
                Scenes.PerceivedType => "type_perceived",
                Scenes.DirectoryType => "type_directory",
                Scenes.MenuAnalysis => "type_menuanalysis",
                Scenes.EnhanceMenu => "rule_enhance",
                Scenes.DetailedEdit => "rule_detailed",
                Scenes.DragDrop => "rule_dragdrop",
                Scenes.PublicReferences => "rule_public",
                Scenes.CustomRegPath => "rule_customreg",
                _ => null
            } ?? throw new ArgumentException("Unsupported scene for JumpToScene", nameof(scene));

            // Map scene to toolbar index and sidebar index
            int toolbarIdx = scene switch
            {
                Scenes.File or Scenes.Folder or Scenes.Directory or Scenes.Background
                    or Scenes.Desktop or Scenes.Drive or Scenes.AllObjects or Scenes.Computer
                    or Scenes.RecycleBin or Scenes.Library or Scenes.New or Scenes.SendTo
                    or Scenes.OpenWith or Scenes.WinX => 0,
                Scenes.LnkFile or Scenes.UwpLnk or Scenes.ExeFile or Scenes.UnknownType
                    or Scenes.CustomExtension or Scenes.PerceivedType or Scenes.DirectoryType
                    or Scenes.MenuAnalysis => 1,
                Scenes.EnhanceMenu or Scenes.DetailedEdit or Scenes.DragDrop
                    or Scenes.PublicReferences or Scenes.InternetExplorer or Scenes.GuidBlocked
                    or Scenes.CustomRegPath => 2,
                _ => 0
            };

            int sidebarIdx = scene switch
            {
                Scenes.File => 0, Scenes.Folder => 1, Scenes.Directory => 2,
                Scenes.Background => 3, Scenes.Desktop => 4, Scenes.Drive => 5,
                Scenes.AllObjects => 6, Scenes.Computer => 7, Scenes.RecycleBin => 8,
                Scenes.Library => 9, Scenes.New => 11, Scenes.SendTo => 12,
                Scenes.OpenWith => 13, Scenes.WinX => 15,
                Scenes.LnkFile => 0, Scenes.UwpLnk => 1, Scenes.ExeFile => 2,
                Scenes.UnknownType => 3, Scenes.CustomExtension => 5,
                Scenes.PerceivedType => 6, Scenes.DirectoryType => 7,
                Scenes.MenuAnalysis => 9,
                Scenes.EnhanceMenu => 0, Scenes.DetailedEdit => 1,
                Scenes.DragDrop => 3, Scenes.PublicReferences => 4,
                Scenes.InternetExplorer => 5, Scenes.GuidBlocked => 7,
                Scenes.CustomRegPath => 8,
                _ => 0
            };

            selectedToolBarIndex = toolbarIdx;
            ToolBar.SelectedIndex = toolbarIdx;
            SwitchTab();
            SideBar.SelectedIndex = sidebarIdx;
        }

        private void FirstRunDownloadLanguage()
        {
            if (!AppConfig.IsFirstRun)
            {
                return;
            }

            if (CultureInfo.CurrentUICulture.Name == "zh-CN")
            {
                return;
            }

            var result = AppMessageBox.Show(
                "It is detected that you may be running this program for the first time,\n" +
                "and your system display language is not Simplified Chinese (zh-CN).\n" +
                "Do you need to download another language?",
                AppString.General.AppName,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                selectedToolBarIndex = 4;
                ToolBar.SelectedIndex = 4;
                SideBar.SelectedIndex = 1;
                LanguagesView.ShowLanguageDialog();
            }
        }
    }
}
