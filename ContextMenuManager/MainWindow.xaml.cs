using ContextMenuManager.Controls;
using ContextMenuManager.Methods;
using ContextMenuManager.Views;
using iNKORE.UI.WPF.Modern;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using DrawingSize = System.Drawing.Size;
using WinForms = System.Windows.Forms;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;

namespace ContextMenuManager
{
    public partial class MainWindow : Window
    {
        // TODO
        public static readonly string DefaultText = "Ver: Debug";/*$"Ver: {Application.ProductVersion}    {Application.CompanyName}";*/

        internal static MainWindow Instance { get; private set; }

        // WinForms content controls hosted in the WindowsFormsHost
        private readonly ShellList shellList = new();
        private readonly ShellNewList shellNewList = new();
        private readonly SendToList sendToList = new();
        private readonly OpenWithList openWithList = new();
        private readonly WinXList winXList = new();
        private readonly EnhanceMenuList enhanceMenusList = new();
        private readonly DetailedEditList detailedEditList = new();
        private readonly GuidBlockedList guidBlockedList = new();
        private readonly IEList iEList = new();
        private readonly AppSettingView appSettingView = new();
        private readonly LanguagesView languagesView = new();
        private readonly BackupView backupView = new();
        private readonly DictionariesView dictionariesView = new();
        private readonly AboutAppView aboutAppView = new();
        private readonly DonateView donateView = new();

        // WinForms container panel (single child for WindowsFormsHost)
        private readonly WinForms.Panel contentPanel = new();

        private WinForms.Control currentListControl;
        private string currentTag;

        // Saved items for search restore (mirrors MainForm logic)
        private readonly List<WinForms.Control> originalListItems = new();

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;

            Title = AppString.General.AppName ?? "ContextMenuManager";
            RefreshButton.Content = AppString.ToolBar.Refresh ?? "Refresh";
            SearchBox.SetValue(iNKORE.UI.WPF.Modern.Controls.Helpers.ControlHelper.PlaceholderTextProperty,
                AppString.General.Search ?? "Search...");
            appSettingView.OwnerWindow = this;
            languagesView.OwnerWindow = this;
            backupView.OwnerWindow = this;

            // Restore saved window size
            var savedSize = AppConfig.MainFormSize;
            if (savedSize.Width >= 680 && savedSize.Height >= 450)
            {
                Width = savedSize.Width;
                Height = savedSize.Height;
            }
            Topmost = AppConfig.TopMost;

            // Set up the WinForms container panel
            contentPanel.Dock = WinForms.DockStyle.Fill;
            contentPanel.BackColor = System.Drawing.Color.Transparent;

            // Add all main content controls (hidden by default)
            foreach (var ctrl in AllContentControls())
            {
                ctrl.Dock = WinForms.DockStyle.Fill;
                ctrl.Visible = false;
                contentPanel.Controls.Add(ctrl);
            }

            // Host the WinForms panel inside WPF
            ContentHost.Child = contentPanel;

            // Populate navigation items from AppString
            BuildNavigation();

            // Show first page
            NavigateTo("shell_file");

            // First-run language download prompt
            Loaded += (_, _) => FirstRunDownloadLanguage();
            Closed += (_, _) =>
            {
                if (ReferenceEquals(Instance, this))
                {
                    Instance = null;
                }
            };

            DarkModeHelper.ThemeChanged += DarkModeHelper_ThemeChanged;
            ThemeManager.SetRequestedTheme(this, DarkModeHelper.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light);
        }

        private void DarkModeHelper_ThemeChanged(object sender, EventArgs e)
        {
            ThemeManager.SetRequestedTheme(this, DarkModeHelper.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light);
        }

        private WinForms.Control[] AllContentControls() =>
        [
            shellList, shellNewList, sendToList, openWithList, winXList,
            enhanceMenusList, detailedEditList, guidBlockedList, iEList,
        ];

        // Navigation building

        private void BuildNavigation()
        {
            var homeItem = MakeSectionItem(AppString.ToolBar.Home ?? "Home", "\uE80F");
            AddSubItems(homeItem, new[]
            {
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
            });
            homeItem.MenuItems.Add(new NavigationViewItemSeparator());
            homeItem.MenuItems.Add(MakeItem(AppString.SideBar.New ?? "New", "shell_new"));
            homeItem.MenuItems.Add(MakeItem(AppString.SideBar.SendTo ?? "Send To", "shell_sendto"));
            homeItem.MenuItems.Add(MakeItem(AppString.SideBar.OpenWith ?? "Open With", "shell_openwith"));
            homeItem.MenuItems.Add(new NavigationViewItemSeparator());
            homeItem.MenuItems.Add(MakeItem(AppString.SideBar.WinX ?? "WinX", "shell_winx"));
            homeItem.IsExpanded = true;

            var typeItem = MakeSectionItem(AppString.ToolBar.Type ?? "Type", "\uE8A9");
            AddSubItems(typeItem, new[]
            {
                (AppString.SideBar.LnkFile ?? "Lnk File", "type_lnk"),
                (AppString.SideBar.UwpLnk ?? "UWP Lnk", "type_uwplnk"),
                (AppString.SideBar.ExeFile ?? "Exe File", "type_exe"),
                (AppString.SideBar.UnknownType ?? "Unknown Type", "type_unknown"),
            });
            typeItem.MenuItems.Add(new NavigationViewItemSeparator());
            AddSubItems(typeItem, new[]
            {
                (AppString.SideBar.CustomExtension ?? "Custom Extension", "type_custom"),
                (AppString.SideBar.PerceivedType ?? "Perceived Type", "type_perceived"),
                (AppString.SideBar.DirectoryType ?? "Directory Type", "type_directory"),
            });
            typeItem.MenuItems.Add(new NavigationViewItemSeparator());
            typeItem.MenuItems.Add(MakeItem(AppString.SideBar.MenuAnalysis ?? "Menu Analysis", "type_menuanalysis"));

            var ruleItem = MakeSectionItem(AppString.ToolBar.Rule ?? "Rule", "\uE90F");
            AddSubItems(ruleItem, new[]
            {
                (AppString.SideBar.EnhanceMenu ?? "Enhance Menu", "rule_enhance"),
                (AppString.SideBar.DetailedEdit ?? "Detailed Edit", "rule_detailed"),
            });
            ruleItem.MenuItems.Add(new NavigationViewItemSeparator());
            AddSubItems(ruleItem, new[]
            {
                (AppString.SideBar.DragDrop ?? "Drag Drop", "rule_dragdrop"),
                (AppString.SideBar.PublicReferences ?? "Public References", "rule_public"),
                (AppString.SideBar.IEMenu ?? "IE Menu", "rule_ie"),
            });
            ruleItem.MenuItems.Add(new NavigationViewItemSeparator());
            AddSubItems(ruleItem, new[]
            {
                (AppString.SideBar.GuidBlocked ?? "GUID Blocked", "rule_guid"),
                (AppString.SideBar.CustomRegPath ?? "Custom Reg Path", "rule_customreg"),
            });

            NavView.MenuItems.Add(homeItem);
            NavView.MenuItems.Add(typeItem);
            NavView.MenuItems.Add(ruleItem);

            NavView.FooterMenuItems.Add(MakeItem(AppString.SideBar.AppSetting ?? "Settings", "about_settings"));
            NavView.FooterMenuItems.Add(MakeItem(AppString.SideBar.AppLanguage ?? "Language", "about_language"));
            NavView.FooterMenuItems.Add(MakeItem(AppString.SideBar.BackupRestore ?? "Backup", "about_backup"));
            NavView.FooterMenuItems.Add(MakeItem(AppString.SideBar.Dictionaries ?? "Dictionaries", "about_dict"));
            NavView.FooterMenuItems.Add(MakeItem(AppString.SideBar.AboutApp ?? "About", "about_app"));
            NavView.FooterMenuItems.Add(MakeItem(AppString.SideBar.Donate ?? "Donate", "about_donate"));
        }

        private static NavigationViewItem MakeSectionItem(string content, string glyph) =>
            new NavigationViewItem { Content = content, Icon = new FontIcon { Glyph = glyph } };

        private static NavigationViewItem MakeItem(string content, string tag) =>
            new NavigationViewItem { Content = content, Tag = tag };

        private static void AddSubItems(NavigationViewItem parent, (string content, string tag)[] items)
        {
            foreach (var (content, tag) in items)
            {
                parent.MenuItems.Add(MakeItem(content, tag));
            }
        }

        // Navigation / content switching

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
            {
                NavView.Header = item.Content;
                SearchBox.Text = string.Empty;
                originalListItems.Clear();
                NavigateTo(tag);
            }
        }

        private void NavigateTo(string tag)
        {
            if (tag == null)
            {
                return;
            }

            currentTag = tag;

            foreach (WinForms.Control ctrl in contentPanel.Controls)
            {
                ctrl.Visible = false;
            }

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
                case "shell_new": shellNewList.LoadItems(); ShowControl(shellNewList); break;
                case "shell_sendto": sendToList.LoadItems(); ShowControl(sendToList); break;
                case "shell_openwith": openWithList.LoadItems(); ShowControl(openWithList); break;
                case "shell_winx": winXList.LoadItems(); ShowControl(winXList); break;

                case "type_lnk": LoadShell(Scenes.LnkFile); break;
                case "type_uwplnk": LoadShell(Scenes.UwpLnk); break;
                case "type_exe": LoadShell(Scenes.ExeFile); break;
                case "type_unknown": LoadShell(Scenes.UnknownType); break;
                case "type_custom": LoadShell(Scenes.CustomExtension); break;
                case "type_perceived": LoadShell(Scenes.PerceivedType); break;
                case "type_directory": LoadShell(Scenes.DirectoryType); break;
                case "type_menuanalysis": LoadShell(Scenes.MenuAnalysis); break;

                case "rule_enhance":
                    enhanceMenusList.ScenePath = null;
                    enhanceMenusList.LoadItems();
                    ShowControl(enhanceMenusList);
                    break;
                case "rule_detailed":
                    detailedEditList.GroupGuid = Guid.Empty;
                    detailedEditList.LoadItems();
                    ShowControl(detailedEditList);
                    break;
                case "rule_dragdrop": LoadShell(Scenes.DragDrop); break;
                case "rule_public": LoadShell(Scenes.PublicReferences); break;
                case "rule_ie": iEList.LoadItems(); ShowControl(iEList); break;
                case "rule_guid": guidBlockedList.LoadItems(); ShowControl(guidBlockedList); break;
                case "rule_customreg": LoadShell(Scenes.CustomRegPath); break;

                case "about_settings":
                    appSettingView.RefreshFromConfig();
                    ShowWpfControl(appSettingView);
                    break;
                case "about_language":
                    languagesView.LoadLanguages();
                    ShowWpfControl(languagesView);
                    break;
                case "about_backup":
                    backupView.LoadItems();
                    ShowWpfControl(backupView);
                    break;
                case "about_dict":
                    dictionariesView.LoadText();
                    ShowWpfControl(dictionariesView);
                    break;
                case "about_app":
                    aboutAppView.RefreshContent();
                    ShowWpfControl(aboutAppView);
                    break;
                case "about_donate":
                    donateView.RefreshContent();
                    ShowWpfControl(donateView);
                    break;
            }
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
                Scenes.LnkFile => "type_lnk",
                Scenes.UwpLnk => "type_uwplnk",
                Scenes.ExeFile => "type_exe",
                Scenes.UnknownType => "type_unknown",
                Scenes.CustomExtension => "type_custom",
                Scenes.PerceivedType => "type_perceived",
                Scenes.DirectoryType => "type_directory",
                Scenes.MenuAnalysis => "type_menuanalysis",
                Scenes.DragDrop => "rule_dragdrop",
                Scenes.PublicReferences => "rule_public",
                Scenes.CustomRegPath => "rule_customreg",
                _ => null
            };

            if (tag == null)
            {
                return;
            }

            SelectNavigationItem(tag);
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
            shellList.Scene = scene;
            shellList.LoadItems();
            ShowControl(shellList);
        }

        private void ShowControl(WinForms.Control ctrl)
        {
            WpfContentHost.Content = null;
            WpfContentHost.Visibility = Visibility.Collapsed;
            ContentHost.Visibility = Visibility.Visible;
            ctrl.Visible = true;
            currentListControl = ctrl;
            SetSearchEnabled(ctrl is MyList);
        }

        private void ShowWpfControl(System.Windows.Controls.Control ctrl)
        {
            ContentHost.Visibility = Visibility.Collapsed;
            WpfContentHost.Content = ctrl;
            WpfContentHost.Visibility = Visibility.Visible;
            currentListControl = null;
            SetSearchEnabled(false);
            UpdateStatusText(string.Empty);
        }

        private void SetSearchEnabled(bool enabled)
        {
            SearchBox.Text = string.Empty;
            SearchBox.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        }

        // Refresh

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            WinForms.Cursor.Current = WinForms.Cursors.WaitCursor;
            ObjectPath.FilePathDic.Clear();
            AppConfig.ReloadConfig();
            GuidInfo.ReloadDics();
            XmlDicHelper.ReloadDics();
            NavigateTo(currentTag);
            WinForms.Cursor.Current = WinForms.Cursors.Default;
        }

        // Search

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            FilterItems(SearchBox.Text);
        }

        private void FilterItems(string filterText)
        {
            if (string.IsNullOrWhiteSpace(filterText))
            {
                RestoreOriginalListItems();
                UpdateStatusText(DefaultText);
                return;
            }

            var searchText = filterText.ToLower();

            if (currentListControl is MyList myList)
            {
                if (originalListItems.Count == 0)
                {
                    foreach (WinForms.Control ctrl in myList.Controls)
                    {
                        originalListItems.Add(ctrl);
                    }
                }

                FilterListItems(myList, searchText);
            }
        }

        private void RestoreOriginalListItems()
        {
            if (currentListControl is MyList myList && originalListItems.Count > 0)
            {
                myList.Controls.Clear();
                foreach (var item in originalListItems)
                {
                    myList.Controls.Add(item);
                }
                originalListItems.Clear();
            }
        }

        private void FilterListItems(MyList listControl, string searchText)
        {
            var itemsToShow = new List<WinForms.Control>();

            foreach (WinForms.Control control in listControl.Controls)
            {
                if (control is not MyListItem item)
                {
                    continue;
                }

                var matches = item.Text?.ToLower().Contains(searchText) == true
                    || item.SubText?.ToLower().Contains(searchText) == true;

                if (!matches)
                {
                    foreach (var prop in item.GetType().GetProperties())
                    {
                        if (prop.PropertyType == typeof(string) && prop.Name is not "Text" and not "SubText")
                        {
                            if (prop.GetValue(item) is string val && val.ToLower().Contains(searchText))
                            {
                                matches = true;
                                break;
                            }
                        }
                    }
                }

                if (matches)
                {
                    itemsToShow.Add(item);
                }
            }

            listControl.Controls.Clear();
            foreach (var item in itemsToShow)
            {
                listControl.Controls.Add(item);
            }

            var statusMsg = itemsToShow.Count == 0
                ? $"{AppString.General.NoResultsFor ?? "No results for"} \"{searchText}\""
                : $"{AppString.General.Search ?? "Found"}: {itemsToShow.Count}";
            UpdateStatusText(statusMsg);
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
                var result = System.Windows.MessageBox.Show(
                    ExplorerRestarterBanner.MessageText,
                    AppString.General.AppName,
                    MessageBoxButton.OKCancel,
                    WpfMessageBoxImage.Question);
                if (result == MessageBoxResult.OK)
                {
                    ExternalProgram.RestartExplorer();
                    ExplorerRestarter.Hide();
                }
            }

            AppConfig.MainFormSize = new DrawingSize((int)Width, (int)Height);
            Opacity = 0;
        }

        // First-run helper

        private void FirstRunDownloadLanguage()
        {
            if (!AppConfig.IsFirstRun)
            {
                return;
            }

            if (System.Globalization.CultureInfo.CurrentUICulture.Name == "zh-CN")
            {
                return;
            }

            var result = System.Windows.MessageBox.Show(
                "It is detected that you may be running this program for the first time,\n" +
                "and your system display language is not Simplified Chinese (zh-CN).\n" +
                "Do you need to download another language?",
                AppString.General.AppName,
                MessageBoxButton.YesNo,
                WpfMessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                NavigateTo("about_language");
                languagesView.ShowLanguageDialog();
            }
        }
    }
}
