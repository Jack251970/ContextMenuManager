using ContextMenuManager.Controls;
using ContextMenuManager.Methods;
using ContextMenuManager.Properties;
using ContextMenuManager.Views;
using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Controls.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
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

        private UIElement currentListControl;
        private string currentTag;

        private readonly SearchService searchService = new();

        public MainWindow()
        {
            InitializeComponent();

            Title = AppString.General.AppName ?? "ContextMenuManager";
            Icon = AppResources.Logo.ToBitmapSource();
            RefreshButton.Content = AppString.ToolBar.Refresh ?? "Refresh";
            ControlHelper.SetPlaceholderText(SearchBox, AppString.Other.SearchContent ?? "Search...");
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

            // Populate navigation items from AppString
            BuildNavigation();

            // First-run language download prompt
            Loaded += (_, _) => FirstRunDownloadLanguage();
        }

        // Navigation building

        private void BuildNavigation()
        {
            var homeItem = MakeSectionItem(AppString.ToolBar.Home ?? "Home", "\uE80F");
            AddSubItems(homeItem,
            [
                (AppString.SideBar.File ?? "File", "shell_file"),
                (AppString.SideBar.Folder ?? "Folder", "shell_folder"),
                (AppString.SideBar.Directory ?? "Directory", "shell_directory"),
                (AppString.SideBar.Background ?? "Background", "shell_background"),
                (AppString.SideBar.Desktop ?? "Desktop", "shell_desktop"),
                (AppString.SideBar.Drive ?? "Drive", "shell_drive"),
                (AppString.SideBar.AllObjects ?? "All Objects", "shell_allobjects"),
                (AppString.SideBar.Computer ?? "Computer", "shell_computer"),
                (AppString.SideBar.RecycleBin ?? "Recycle Bin", "shell_recyclebin"),
                (AppString.SideBar.Library ?? "Library", "shell_library"),
            ]);
            homeItem.MenuItems.Add(new NavigationViewItemSeparator());
            homeItem.MenuItems.Add(MakeItem(AppString.SideBar.New ?? "New", "shell_new"));
            homeItem.MenuItems.Add(MakeItem(AppString.SideBar.SendTo ?? "Send To", "shell_sendto"));
            homeItem.MenuItems.Add(MakeItem(AppString.SideBar.OpenWith ?? "Open With", "shell_openwith"));
            homeItem.MenuItems.Add(new NavigationViewItemSeparator());
            homeItem.MenuItems.Add(MakeItem(AppString.SideBar.WinX ?? "WinX", "shell_winx"));
            homeItem.IsExpanded = true;

            var typeItem = MakeSectionItem(AppString.ToolBar.Type ?? "Type", "\uE8A9");
            AddSubItems(typeItem,
            [
                (AppString.SideBar.LnkFile ?? "Lnk File", "type_lnk"),
                (AppString.SideBar.UwpLnk ?? "UWP Lnk", "type_uwplnk"),
                (AppString.SideBar.ExeFile ?? "Exe File", "type_exe"),
                (AppString.SideBar.UnknownType ?? "Unknown Type", "type_unknown"),
            ]);
            typeItem.MenuItems.Add(new NavigationViewItemSeparator());
            AddSubItems(typeItem,
            [
                (AppString.SideBar.CustomExtension ?? "Custom Extension", "type_custom"),
                (AppString.SideBar.PerceivedType ?? "Perceived Type", "type_perceived"),
                (AppString.SideBar.DirectoryType ?? "Directory Type", "type_directory"),
            ]);
            typeItem.MenuItems.Add(new NavigationViewItemSeparator());
            typeItem.MenuItems.Add(MakeItem(AppString.SideBar.MenuAnalysis ?? "Menu Analysis", "type_menuanalysis"));

            var ruleItem = MakeSectionItem(AppString.ToolBar.Rule ?? "Rule", "\uE90F");
            AddSubItems(ruleItem,
            [
                (AppString.SideBar.EnhanceMenu ?? "Enhance Menu", "rule_enhance"),
                (AppString.SideBar.DetailedEdit ?? "Detailed Edit", "rule_detailed"),
            ]);
            ruleItem.MenuItems.Add(new NavigationViewItemSeparator());
            AddSubItems(ruleItem,
            [
                (AppString.SideBar.DragDrop ?? "Drag Drop", "rule_dragdrop"),
                (AppString.SideBar.PublicReferences ?? "Public References", "rule_public"),
                (AppString.SideBar.IEMenu ?? "IE Menu", "rule_ie"),
            ]);
            ruleItem.MenuItems.Add(new NavigationViewItemSeparator());
            AddSubItems(ruleItem,
            [
                (AppString.SideBar.GuidBlocked ?? "GUID Blocked", "rule_guid"),
                (AppString.SideBar.CustomRegPath ?? "Custom Reg Path", "rule_customreg"),
            ]);

            NavView.MenuItems.Add(homeItem);
            NavView.MenuItems.Add(typeItem);
            NavView.MenuItems.Add(ruleItem);

            NavView.FooterMenuItems.Add(MakeItem(AppString.SideBar.AppSetting ?? "Settings", "about_settings", "\uE713"));
            NavView.FooterMenuItems.Add(MakeItem(AppString.SideBar.AppLanguage ?? "Language", "about_language", "\uE775"));
            NavView.FooterMenuItems.Add(MakeItem(AppString.SideBar.BackupRestore ?? "Backup", "about_backup", "\uE777"));
            NavView.FooterMenuItems.Add(MakeItem(AppString.SideBar.Dictionaries ?? "Dictionaries", "about_dict", "\uE82D"));
            NavView.FooterMenuItems.Add(MakeItem(AppString.SideBar.AboutApp ?? "About", "about_app", "\uE946"));
            NavView.FooterMenuItems.Add(MakeItem(AppString.SideBar.Donate ?? "Donate", "about_donate", "\uEB51"));

            if (NavView.MenuItems[0] is NavigationViewItem parent && parent.MenuItems[0] is NavigationViewItem item)
            {
                NavView.SelectedItem = item;
            }
        }

        private static NavigationViewItem MakeSectionItem(string content, string glyph)
        {
            var item = new NavigationViewItem() { Content = content, Icon = new FontIcon { Glyph = glyph } };
            return item;
        }

        private static NavigationViewItem MakeItem(string content, string tag, string glyph = null)
        {
            var item = new NavigationViewItem { Content = content, Tag = tag };
            if (!string.IsNullOrEmpty(glyph))
            {
                item.Icon = new FontIcon { Glyph = glyph };
            }
            return item;
        }

        private void AddSubItems(NavigationViewItem parent, (string content, string tag)[] items)
        {
            foreach (var (content, tag) in items)
            {
                parent.MenuItems.Add(MakeItem(content, tag));
            }
            foreach (var item in parent.MenuItems)
            {
                if (item is NavigationViewItem navItem)
                {
                    navItem.MouseEnter += NavItem_MouseEnter;
                    navItem.MouseLeave += NavItem_MouseLeave;
                }
            }
        }

        private void NavItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is NavigationViewItem item && item.Tag is string tag)
            {
                UpdateStatusText(GetStatusText(tag));
            }
        }

        private void NavItem_MouseLeave(object sender, MouseEventArgs e)
        {
            UpdateStatusText(GetStatusText(currentTag));
        }

        // Navigation / content switching

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
            {
                NavView.Header = item.Content;
                SearchBox.Text = string.Empty;
                searchService.Clear();
                NavigateTo(tag);
            }
        }

        private void NavigateTo(string tag)
        {
            ArgumentNullException.ThrowIfNull(tag);

            if (currentTag == tag)
            {
                return;
            }

            currentTag = tag;

            if (currentListControl is MyList myList)
            {
                myList.ClearItems();
            }
            currentListControl = null;

            switch (tag)
            {
                case "shell_file": LoadShell(Scenes.File); break;
                case "shell_folder": LoadShell(Scenes.Folder); break;
                case "shell_directory": LoadShell(Scenes.Directory); break;
                case "shell_background": LoadShell(Scenes.Background); break;
                case "shell_desktop": LoadShell(Scenes.Desktop); break;
                case "shell_drive": LoadShell(Scenes.Drive); break;
                case "shell_allobjects": LoadShell(Scenes.AllObjects); break;
                case "shell_computer": LoadShell(Scenes.Computer); break;
                case "shell_recyclebin": LoadShell(Scenes.RecycleBin); break;
                case "shell_library": LoadShell(Scenes.Library); break;
                case "shell_new": LoadShell(Scenes.New); break;
                case "shell_sendto": LoadShell(Scenes.SendTo); break;
                case "shell_openwith": LoadShell(Scenes.OpenWith); break;
                case "shell_winx": LoadShell(Scenes.WinX); break;

                case "type_lnk": LoadShell(Scenes.LnkFile); break;
                case "type_uwplnk": LoadShell(Scenes.UwpLnk); break;
                case "type_exe": LoadShell(Scenes.ExeFile); break;
                case "type_unknown": LoadShell(Scenes.UnknownType); break;
                case "type_custom": LoadShell(Scenes.CustomExtension); break;
                case "type_perceived": LoadShell(Scenes.PerceivedType); break;
                case "type_directory": LoadShell(Scenes.DirectoryType); break;
                case "type_menuanalysis": LoadShell(Scenes.MenuAnalysis); break;

                case "rule_enhance": LoadShell(Scenes.EnhanceMenu); break;
                case "rule_detailed": LoadShell(Scenes.DetailedEdit); break;
                case "rule_dragdrop": LoadShell(Scenes.DragDrop); break;
                case "rule_public": LoadShell(Scenes.PublicReferences); break;
                case "rule_ie": LoadShell(Scenes.InternetExplorer); break;
                case "rule_guid": LoadShell(Scenes.GuidBlocked); break;
                case "rule_customreg": LoadShell(Scenes.CustomRegPath); break;

                case "about_settings":
                    AppSettingView.RefreshFromConfig();
                    ShowControl(AppSettingView);
                    break;
                case "about_language":
                    LanguagesView.LoadLanguages();
                    ShowControl(LanguagesView);
                    break;
                case "about_backup":
                    BackupView.LoadItems();
                    ShowControl(BackupView);
                    break;
                case "about_dict":
                    DictionariesView.LoadText();
                    ShowControl(DictionariesView);
                    break;
                case "about_app":
                    AboutAppView.RefreshContent();
                    ShowControl(AboutAppView);
                    break;
                case "about_donate":
                    DonateView.RefreshContent();
                    ShowControl(DonateView);
                    break;
                default:
                    throw new NotImplementedException();
            }

            UpdateStatusText(GetStatusText(currentTag));
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
            SelectNavigationItem(tag);
        }

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
                _ => null
            } ?? throw new ArgumentException("Unsupported tag for GetStatusText", nameof(tag));
        }

        internal void RefreshCurrentView()
        {
            NavigateTo(currentTag);
        }

        private void SelectNavigationItem(string tag)
        {
            foreach (var item in EnumerateNavigationItems())
            {
                if (string.Equals(item.Tag as string, tag, StringComparison.Ordinal))
                {
                    NavView.SelectedItem = item;
                    return;
                }
            }

            NavigateTo(tag);
        }

        private IEnumerable<NavigationViewItem> EnumerateNavigationItems()
        {
            foreach (var item in NavView.MenuItems.OfType<NavigationViewItem>())
            {
                foreach (var nested in EnumerateNavigationItems(item))
                {
                    yield return nested;
                }
            }

            foreach (var item in NavView.FooterMenuItems.OfType<NavigationViewItem>())
            {
                yield return item;
            }
        }

        private static IEnumerable<NavigationViewItem> EnumerateNavigationItems(NavigationViewItem item)
        {
            yield return item;

            foreach (var nested in item.MenuItems.OfType<NavigationViewItem>())
            {
                foreach (var child in EnumerateNavigationItems(nested))
                {
                    yield return child;
                }
            }
        }

        private void LoadShell(Scenes scene)
        {
            switch (scene)
            {
                case Scenes.New: ShellNewList.LoadItems(); ShowControl(ShellNewList); break;
                case Scenes.SendTo: SendToList.LoadItems(); ShowControl(SendToList); break;
                case Scenes.OpenWith: OpenWithList.LoadItems(); ShowControl(OpenWithList); break;
                case Scenes.WinX: WinXList.LoadItems(); ShowControl(WinXList); break;
                case Scenes.InternetExplorer: IEList.LoadItems(); ShowControl(IEList); break;
                case Scenes.GuidBlocked: GuidBlockedList.LoadItems(); ShowControl(GuidBlockedList); break;
                case Scenes.EnhanceMenu:
                    EnhanceMenusList.ScenePath = null;
                    EnhanceMenusList.LoadItems();
                    ShowControl(EnhanceMenusList);
                    break;
                case Scenes.DetailedEdit:
                    DetailedEditList.GroupGuid = Guid.Empty;
                    DetailedEditList.LoadItems();
                    ShowControl(DetailedEditList);
                    break;
                default:
                    ShellList.Scene = scene; ShellList.LoadItems();
                    ShowControl(ShellList);
                    break;
            }
        }

        private void ShowControl(UIElement ctrl)
        {
            WpfContentHost.Content = ctrl;
            WpfContentHost.Visibility = Visibility.Visible;
            currentListControl = ctrl;
            SetSearchEnabled(ctrl is MyList);
            if (ctrl is not MyList) UpdateStatusText(DefaultText);
        }

        private void SetSearchEnabled(bool enabled)
        {
            SearchBox.Text = string.Empty;
            SearchBoxPanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        }

        // Refresh

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            ObjectPath.ClearFilePathDic();
            AppConfig.ReloadConfig();
            GuidInfo.ReloadDics();
            XmlDicHelper.ReloadDics();
            NavigateTo(currentTag);
        }

        // Search

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
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

        // Status bar helper

        private void UpdateStatusText(string text)
        {
            StatusText.Text = text;
        }

        // Window events

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (ExplorerRestarterBanner.IsPendingRestart)
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
                NavigateTo("about_language");
                LanguagesView.ShowLanguageDialog();
            }
        }
    }
}
