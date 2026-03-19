using ContextMenuManager.Controls;
using ContextMenuManager.Methods;
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
            HideSysStoreBorder.Visibility = WinOsVersion.Current >= WinOsVersion.Win7 ? Visibility.Visible : Visibility.Collapsed;

            isLoading = false;
        }

        private void LoadStaticOptions()
        {
            RepoComboBox.Items.Clear();
            RepoComboBox.Items.Add("Github");
            RepoComboBox.Items.Add("Gitee");

            ConfigDirComboBox.Items.Clear();
            UpdateComboBox.Items.Clear();
            EngineComboBox.Items.Clear();

            foreach (var engine in AppConfig.EngineUrlsDic.Keys)
            {
                EngineComboBox.Items.Add(engine);
            }
            EngineComboBox.Items.Add(AppString.Other.CustomEngine ?? "Custom");
        }

        private void LoadLabels()
        {
            ConfigPathLabel.Text = AppString.Other.ConfigPath;
            UpdateFrequencyLabel.Text = AppString.Other.SetUpdateFrequency;
            RepoLabel.Text = AppString.Other.SetRequestRepo;
            EngineLabel.Text = AppString.Other.WebSearchEngine;
            AutoBackupLabel.Text = AppString.Other.AutoBackup;
            TopMostLabel.Text = AppString.Other.TopMost;
            ProtectOpenItemLabel.Text = AppString.Other.ProtectOpenItem;
            ShowFilePathLabel.Text = AppString.Other.ShowFilePath;
            OpenMoreRegeditLabel.Text = AppString.Other.OpenMoreRegedit;
            OpenMoreExplorerLabel.Text = AppString.Other.OpenMoreExplorer;
            HideDisabledItemsLabel.Text = AppString.Other.HideDisabledItems;
            HideSysStoreItemsLabel.Text = AppString.Other.HideSysStoreItems;

            ConfigDirComboBox.Items.Clear();
            ConfigDirComboBox.Items.Add(AppString.Other.AppDataDir);
            ConfigDirComboBox.Items.Add(AppString.Other.AppDir);

            UpdateComboBox.Items.Clear();
            UpdateComboBox.Items.Add(AppString.Other.OnceAWeek);
            UpdateComboBox.Items.Add(AppString.Other.OnceAMonth);
            UpdateComboBox.Items.Add(AppString.Other.OnceASeason);
            UpdateComboBox.Items.Add(AppString.Other.NeverCheck);

            var engines = AppConfig.EngineUrlsDic.Keys.ToArray();
            EngineComboBox.Items.Clear();
            foreach (var engine in engines)
            {
                EngineComboBox.Items.Add(engine);
            }
            EngineComboBox.Items.Add(AppString.Other.CustomEngine);
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

            var result = MessageBox.Show(
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

        private void EngineComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
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

            var dialog = new InputDialog
            {
                Title = AppString.Other.SetCustomEngine,
                Text = AppConfig.EngineUrl
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.Text))
            {
                AppConfig.EngineUrl = dialog.Text;
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
