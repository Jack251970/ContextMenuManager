using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ContextMenuManager.Views
{
    public partial class AppSettingView : UserControl
    {
        private bool isLoading;

        public Window OwnerWindow { get; set; }

        public AppSettingView()
        {
            InitializeComponent();
            LoadStaticOptions();
            LoadLabels();
            RefreshFromConfig();
        }

        public void RefreshFromConfig()
        {
            isLoading = true;

            LoadLabels();

            ConfigDirComboBox.SelectedIndex = AppConfig.SaveToAppDir ? 1 : 0;
            UpdateComboBox.SelectedIndex = GetUpdateSelectIndex();
            RepoComboBox.SelectedIndex = AppConfig.RequestUseGithub ? 0 : 1;
            EngineComboBox.SelectedIndex = GetEngineSelectIndex();

            AutoBackupCheckBox.IsChecked = AppConfig.AutoBackup;
            TopMostCheckBox.IsChecked = AppConfig.TopMost;
            ProtectOpenItemCheckBox.IsChecked = AppConfig.ProtectOpenItem;
            ShowFilePathCheckBox.IsChecked = AppConfig.ShowFilePath;
            OpenMoreRegeditCheckBox.IsChecked = AppConfig.OpenMoreRegedit;
            OpenMoreExplorerCheckBox.IsChecked = AppConfig.OpenMoreExplorer;
            HideDisabledItemsCheckBox.IsChecked = AppConfig.HideDisabledItems;
            HideSysStoreItemsCheckBox.IsChecked = AppConfig.HideSysStoreItems;

            var showHideSysStore = WinOsVersion.Current >= WinOsVersion.Win7;
            HideSysStoreRow.Visibility = showHideSysStore ? Visibility.Visible : Visibility.Collapsed;
            HideSysStoreSeparator.Visibility = showHideSysStore ? Visibility.Visible : Visibility.Collapsed;

            isLoading = false;
        }

        private void LoadStaticOptions()
        {
            RepoComboBox.Items.Clear();
            RepoComboBox.Items.Add("Github");
            RepoComboBox.Items.Add("Gitee");

            LoadDynamicOptions();
        }

        private void LoadDynamicOptions()
        {
            ConfigDirComboBox.Items.Clear();
            ConfigDirComboBox.Items.Add(AppString.Other.AppDataDir);
            ConfigDirComboBox.Items.Add(AppString.Other.AppDir);

            UpdateComboBox.Items.Clear();
            UpdateComboBox.Items.Add(AppString.Other.OnceAWeek);
            UpdateComboBox.Items.Add(AppString.Other.OnceAMonth);
            UpdateComboBox.Items.Add(AppString.Other.OnceASeason);
            UpdateComboBox.Items.Add(AppString.Other.NeverCheck);

            EngineComboBox.Items.Clear();
            foreach (var engine in AppConfig.EngineUrlsDic.Keys)
            {
                EngineComboBox.Items.Add(engine);
            }
            EngineComboBox.Items.Add(AppString.Other.CustomEngine ?? "Custom");
        }

        private void LoadLabels()
        {
            PageTitleText.Text = AppString.SideBar.AppSetting ?? "Settings";
            PageDescriptionText.Text = "Manage update behavior, backup handling, and app-wide defaults.";

            ConfigPathLabel.Text = AppString.Other.ConfigPath;
            ConfigPathHint.Text = AppConfig.ConfigDir;
            OpenConfigDirButton.Content = AppString.Menu.FileLocation ?? "Open";

            UpdateFrequencyLabel.Text = AppString.Other.SetUpdateFrequency;
            UpdateFrequencyHint.Text = "Choose how often the app checks for a new release.";
            CheckUpdateButton.Content = AppString.About.CheckUpdate ?? "Check Update";

            RepoLabel.Text = AppString.Other.SetRequestRepo;
            RepoHint.Text = "Select the preferred source for update and download requests.";

            EngineLabel.Text = AppString.Other.WebSearchEngine;
            EngineHint.Text = "Pick a preset engine or provide a custom URL template.";

            AutoBackupLabel.Text = AppString.Other.AutoBackup;
            AutoBackupHint.Text = AppConfig.RegBackupDir;
            OpenBackupDirButton.Content = AppString.Menu.FileLocation ?? "Open";

            TopMostLabel.Text = AppString.Other.TopMost;
            TopMostHint.Text = "Keep the main window above other windows.";

            ProtectOpenItemLabel.Text = AppString.Other.ProtectOpenItem;
            ProtectOpenItemHint.Text = "Prevent accidental edits to protected Open items.";

            ShowFilePathLabel.Text = AppString.Other.ShowFilePath;
            ShowFilePathHint.Text = "Display backing file or registry paths while browsing items.";

            OpenMoreRegeditLabel.Text = AppString.Other.OpenMoreRegedit;
            OpenMoreRegeditHint.Text = "Expose the extended registry editor entry when available.";

            OpenMoreExplorerLabel.Text = AppString.Other.OpenMoreExplorer;
            OpenMoreExplorerHint.Text = "Expose the extended Explorer entry when available.";

            HideDisabledItemsLabel.Text = AppString.Other.HideDisabledItems;
            HideDisabledItemsHint.Text = "Filter disabled items out of the visible lists.";

            HideSysStoreItemsLabel.Text = AppString.Other.HideSysStoreItems;
            HideSysStoreItemsHint.Text = "Hide system store items when the current Windows version supports it.";

            LoadDynamicOptions();
        }

        private void OpenConfigDirButton_OnClick(object sender, RoutedEventArgs e)
        {
            ExternalProgram.OpenDirectory(AppConfig.ConfigDir);
        }

        private void OpenBackupDirButton_OnClick(object sender, RoutedEventArgs e)
        {
            ExternalProgram.OpenDirectory(AppConfig.RegBackupDir);
        }

        private void CheckUpdateButton_OnClick(object sender, RoutedEventArgs e)
        {
            Updater.Update(true);
        }

        private void ConfigDirComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isLoading || ConfigDirComboBox.SelectedIndex < 0)
            {
                return;
            }

            var newPath = ConfigDirComboBox.SelectedIndex == 0 ? AppConfig.AppDataConfigDir : AppConfig.AppConfigDir;
            if (newPath == AppConfig.ConfigDir)
            {
                return;
            }

            var result = System.Windows.MessageBox.Show(
                AppString.Message.RestartApp,
                AppString.General.AppName,
                MessageBoxButton.OKCancel,
                System.Windows.MessageBoxImage.Question);
            if (result != MessageBoxResult.OK)
            {
                RefreshFromConfig();
                return;
            }

            DirectoryEx.CopyTo(AppConfig.ConfigDir, newPath);
            Directory.Delete(AppConfig.ConfigDir, true);
            SingleInstance.Restart();
        }

        private void UpdateComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isLoading || UpdateComboBox.SelectedIndex < 0)
            {
                return;
            }

            AppConfig.UpdateFrequency = UpdateComboBox.SelectedIndex switch
            {
                0 => 7,
                2 => 90,
                3 => -1,
                _ => 30
            };
        }

        private void RepoComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isLoading && RepoComboBox.SelectedIndex >= 0)
            {
                AppConfig.RequestUseGithub = RepoComboBox.SelectedIndex == 0;
            }
        }

        private async void EngineComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isLoading || EngineComboBox.SelectedIndex < 0)
            {
                return;
            }

            if (EngineComboBox.SelectedIndex < EngineComboBox.Items.Count - 1)
            {
                AppConfig.EngineUrl = AppConfig.EngineUrlsDic[EngineComboBox.SelectedItem.ToString()];
                return;
            }

            var dialog = ContentDialogHost.CreateDialog(AppString.Other.SetCustomEngine);
            dialog.PrimaryButtonText = ResourceString.OK;
            dialog.CloseButtonText = ResourceString.Cancel;

            var inputBox = new TextBox
            {
                Text = AppConfig.EngineUrl ?? string.Empty,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                MinWidth = 360,
                MinHeight = 120
            };

            dialog.Content = inputBox;
            var result = await dialog.ShowAsync(OwnerWindow ?? Window.GetWindow(this));
            if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(inputBox.Text))
            {
                AppConfig.EngineUrl = inputBox.Text;
            }

            RefreshFromConfig();
        }

        private void AutoBackupCheckBox_OnChanged(object sender, RoutedEventArgs e)
        {
            if (!isLoading)
            {
                AppConfig.AutoBackup = AutoBackupCheckBox.IsChecked == true;
            }
        }

        private void TopMostCheckBox_OnChanged(object sender, RoutedEventArgs e)
        {
            if (!isLoading)
            {
                AppConfig.TopMost = TopMostCheckBox.IsChecked == true;
                if (OwnerWindow != null)
                {
                    OwnerWindow.Topmost = AppConfig.TopMost;
                }
            }
        }

        private void ProtectOpenItemCheckBox_OnChanged(object sender, RoutedEventArgs e)
        {
            if (!isLoading)
            {
                AppConfig.ProtectOpenItem = ProtectOpenItemCheckBox.IsChecked == true;
            }
        }

        private void ShowFilePathCheckBox_OnChanged(object sender, RoutedEventArgs e)
        {
            if (!isLoading)
            {
                AppConfig.ShowFilePath = ShowFilePathCheckBox.IsChecked == true;
            }
        }

        private void OpenMoreRegeditCheckBox_OnChanged(object sender, RoutedEventArgs e)
        {
            if (!isLoading)
            {
                AppConfig.OpenMoreRegedit = OpenMoreRegeditCheckBox.IsChecked == true;
            }
        }

        private void OpenMoreExplorerCheckBox_OnChanged(object sender, RoutedEventArgs e)
        {
            if (!isLoading)
            {
                AppConfig.OpenMoreExplorer = OpenMoreExplorerCheckBox.IsChecked == true;
            }
        }

        private void HideDisabledItemsCheckBox_OnChanged(object sender, RoutedEventArgs e)
        {
            if (!isLoading)
            {
                AppConfig.HideDisabledItems = HideDisabledItemsCheckBox.IsChecked == true;
            }
        }

        private void HideSysStoreItemsCheckBox_OnChanged(object sender, RoutedEventArgs e)
        {
            if (!isLoading)
            {
                AppConfig.HideSysStoreItems = HideSysStoreItemsCheckBox.IsChecked == true;
            }
        }

        private static int GetUpdateSelectIndex()
        {
            return AppConfig.UpdateFrequency switch
            {
                7 => 0,
                90 => 2,
                -1 => 3,
                _ => 1
            };
        }

        private int GetEngineSelectIndex()
        {
            var urls = AppConfig.EngineUrlsDic.Values.ToArray();
            for (var i = 0; i < urls.Length; i++)
            {
                if (AppConfig.EngineUrl == urls[i])
                {
                    return i;
                }
            }

            return EngineComboBox.Items.Count - 1;
        }
    }
}
