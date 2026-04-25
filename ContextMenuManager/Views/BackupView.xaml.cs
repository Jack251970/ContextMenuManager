using ContextMenuManager.Controls;
using ContextMenuManager.Methods;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ContextMenuManager.Views
{
    public partial class BackupView : UserControl
    {
        private readonly BackupHelper helper = new();
        private bool isLogonRestoreLoading;

        public ObservableCollection<BackupEntry> BackupEntries { get; } = [];

        public string Restore => AppString.Menu.RestoreBackup ?? "Restore backup";
        public string Delete => AppString.Menu.DeleteBackup ?? "Delete backup";

        public Window OwnerWindow { get; set; }

        public BackupView()
        {
            InitializeComponent();
            DataContext = this;
            LoadLabels();
            LoadItems();
        }

        public void LoadItems()
        {
            LoadLabels();
            BackupEntries.Clear();

            var rootPath = AppConfig.MenuBackupRootDir;
            if (Directory.Exists(rootPath))
            {
                foreach (var deviceDir in Directory.GetDirectories(rootPath))
                {
                    foreach (var xmlFile in Directory.GetFiles(deviceDir, "*.xml"))
                    {
                        try
                        {
                            BackupList.LoadBackupDataMetaData(xmlFile);
                            var deviceName = BackupList.metaData?.Device ?? AppString.Other.Unknown;
                            var createTime = BackupList.metaData?.CreateTime ?? File.GetCreationTime(xmlFile);
                            BackupEntries.Add(new BackupEntry(xmlFile, deviceName, createTime));
                        }
                        catch
                        {
                        }
                    }
                }
            }

            foreach (var entry in BackupEntries.OrderByDescending(x => x.CreateTime).ToArray())
            {
                BackupEntries.Remove(entry);
                BackupEntries.Add(entry);
            }

            RefreshLogonRestoreCard();
        }

        private void LoadLabels()
        {
            PageTitleText.Text = AppString.SideBar.BackupRestore ?? "Backup";
            SummaryLabel.Text = AppString.Dialog.BackupContent ?? "Backup";
            SummaryHintText.Text = AppConfig.MenuBackupRootDir;
            NewBackupButton.Content = AppString.Dialog.NewBackupItem ?? "New Backup";
            OpenBackupFolderButton.Content = AppString.Menu.FileLocation ?? "Open";
            RefreshButton.Content = AppString.ToolBar.Refresh ?? "Refresh";
            BackupsHeaderText.Text = AppString.SideBar.BackupRestore ?? "Backups";

            LogonRestoreLabel.Text = AppString.Other.LogonRestore ?? "Auto-restore on startup";
            LogonRestoreConfigButton.Content = AppString.Other.LogonRestoreSettings ?? "Configure";
        }

        // ── Backup folder / refresh / new backup ─────────────────────────────

        private void OpenBackupFolderButton_OnClick(object sender, RoutedEventArgs e)
        {
            ExternalProgram.OpenDirectory(AppConfig.MenuBackupRootDir);
        }

        private void RefreshButton_OnClick(object sender, RoutedEventArgs e)
        {
            LoadItems();
        }

        private async void NewBackupButton_OnClick(object sender, RoutedEventArgs e)
        {
            await CreateBackupAsync();
        }

        private async Task CreateBackupAsync()
        {
            var dlg = new BackupDialog
            {
                Title = AppString.Dialog.NewBackupItem,
                TvTitle = AppString.Dialog.BackupContent,
                TvItems = BackupHelper.BackupScenesText,
                CmbTitle = AppString.Dialog.BackupMode,
                CmbItems = [AppString.Dialog.BackupMode1, AppString.Dialog.BackupMode2, AppString.Dialog.BackupMode3]
            };

            if (dlg.ShowDialog() != true)
            {
                return;
            }

            var backupScenes = dlg.TvSelectedItems;
            if (backupScenes.Count == 0)
            {
                AppMessageBox.Show(AppString.Message.NotChooseAnyBackup, AppString.General.AppName);
                return;
            }

            var backupMode = dlg.CmbSelectedIndex switch
            {
                1 => BackupMode.OnlyVisible,
                2 => BackupMode.OnlyInvisible,
                _ => BackupMode.All
            };

            var success = LoadingDialog.ShowDialog(AppString.SideBar.BackupRestore,
                dialogInterface => helper.BackupItems(backupScenes, backupMode, dialogInterface));

            if (!success)
            {
                return;
            }

            LoadItems();
            AppMessageBox.Show(
                AppString.Message.BackupSucceeded.Replace("%s", helper.backupCount.ToString()),
                AppString.General.AppName);
        }

        // ── Restore / delete individual backup ───────────────────────────────

        private async void RestoreBackupButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button { CommandParameter: BackupEntry entry })
            {
                await RestoreBackupAsync(entry);
            }
        }

        private async Task RestoreBackupAsync(BackupEntry entry)
        {
            BackupList.LoadBackupDataMetaData(entry.FilePath);
            if (BackupList.metaData.Version <= BackupHelper.DeprecatedBackupVersion)
            {
                AppMessageBox.Show(AppString.Message.DeprecatedBackupVersion, AppString.General.AppName);
                return;
            }
            if (BackupList.metaData.Version < BackupHelper.BackupVersion)
            {
                AppMessageBox.Show(AppString.Message.OldBackupVersion, AppString.General.AppName);
            }

            var dlg = new BackupDialog
            {
                Title = AppString.Dialog.RestoreBackupItem,
                TvTitle = AppString.Dialog.RestoreContent,
                TvItems = BackupHelper.GetBackupRestoreScenesText(BackupList.metaData.BackupScenes),
                CmbTitle = AppString.Dialog.RestoreMode,
                CmbItems = [AppString.Dialog.RestoreMode1, AppString.Dialog.RestoreMode2, AppString.Dialog.RestoreMode3]
            };

            if (dlg.ShowDialog() != true)
            {
                return;
            }

            var restoreScenes = dlg.TvSelectedItems;
            if (restoreScenes.Count == 0)
            {
                AppMessageBox.Show(AppString.Message.NotChooseAnyRestore, AppString.General.AppName);
                return;
            }

            var restoreMode = dlg.CmbSelectedIndex switch
            {
                1 => RestoreMode.DisableNotOnList,
                2 => RestoreMode.EnableNotOnList,
                _ => RestoreMode.NotHandleNotOnList
            };

            var success = LoadingDialog.ShowDialog(AppString.SideBar.BackupRestore,
                dialogInterface => helper.RestoreItems(entry.FilePath, restoreScenes, restoreMode, dialogInterface));

            if (!success)
            {
                return;
            }

            await ShowRestoreResultsAsync(helper.restoreList);
        }

        private async Task ShowRestoreResultsAsync(List<RestoreChangedItem> restoreList)
        {
            if (restoreList == null || restoreList.Count == 0)
            {
                AppMessageBox.Show(AppString.Message.NoNeedRestore, AppString.General.AppName);
                return;
            }

            var dialog = ContentDialogHost.CreateDialog(AppString.Dialog.RestoreDetails);

            var items = restoreList.Select(item =>
            {
                var sceneText = BackupHelper.BackupScenesText[(int)item.BackupScene];
                var changedValue = item.ItemData switch
                {
                    "False" => AppString.Dialog.Disabled,
                    "True" => AppString.Dialog.Enabled,
                    _ => item.ItemData
                };

                var section = AppString.ToolBar.Home;
                if (BackupHelper.TypeBackupScenesText.Contains(sceneText))
                {
                    section = AppString.ToolBar.Type;
                }
                else if (BackupHelper.RuleBackupScenesText.Contains(sceneText))
                {
                    section = AppString.ToolBar.Rule;
                }

                return new RestoreResultEntry($"{section} -> {sceneText} -> {item.KeyName}", changedValue);
            }).ToList();

            dialog.Content = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = AppString.Message.RestoreSucceeded.Replace("%s", restoreList.Count.ToString()),
                        Margin = new Thickness(0, 0, 0, 12),
                        TextWrapping = TextWrapping.Wrap
                    },
                    new DataGrid
                    {
                        AutoGenerateColumns = false,
                        CanUserAddRows = false,
                        CanUserDeleteRows = false,
                        IsReadOnly = true,
                        ItemsSource = items,
                        Columns =
                        {
                            new DataGridTextColumn { Header = AppString.Dialog.ItemLocation, Binding = new System.Windows.Data.Binding(nameof(RestoreResultEntry.Location)), Width = new DataGridLength(1, DataGridLengthUnitType.Star) },
                            new DataGridTextColumn { Header = AppString.Dialog.RestoredValue, Binding = new System.Windows.Data.Binding(nameof(RestoreResultEntry.Value)), Width = new DataGridLength(220) }
                        }
                    }
                }
            };

            await dialog.ShowAsync(OwnerWindow ?? Window.GetWindow(this));
        }

        private void DeleteBackupButton_OnClick(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.CommandParameter is not BackupEntry entry)
            {
                return;
            }

            var result = AppMessageBox.Show(
                AppString.Message.ConfirmDeleteBackupPermanently,
                AppString.General.AppName,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                // If the deleted backup was the configured logon-restore backup, clean up config
                if (AppConfig.LogonRestoreFilePath == entry.FilePath)
                {
                    DisableLogonRestoreTask();
                    RefreshLogonRestoreCard();
                }

                File.Delete(entry.FilePath);
                BackupEntries.Remove(entry);
            }
            catch (Exception ex)
            {
                AppMessageBox.Show(ex.Message, AppString.General.AppName);
            }
        }

        // ── Logon restore section ─────────────────────────────────────────────

        /// <summary>Updates the logon-restore card to reflect the current configuration.</summary>
        private void RefreshLogonRestoreCard()
        {
            isLogonRestoreLoading = true;
            try
            {
                var enabled = LogonTaskHelper.IsTaskEnabled();
                LogonRestoreToggle.IsOn = enabled;
                LogonRestoreConfigButton.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
                UpdateLogonRestoreHintText(enabled);
            }
            finally
            {
                isLogonRestoreLoading = false;
            }
        }

        private void UpdateLogonRestoreHintText(bool enabled)
        {
            if (!enabled)
            {
                LogonRestoreHintText.Text = null;
                return;
            }

            var filePath = AppConfig.LogonRestoreFilePath;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                LogonRestoreHintText.Text = null;
                return;
            }

            try
            {
                BackupList.LoadBackupDataMetaData(filePath);
                var device = BackupList.metaData?.Device ?? AppString.Other.Unknown;
                var time = (BackupList.metaData?.CreateTime ?? File.GetCreationTime(filePath)).ToString("G");
                var backupText = AppString.Other.RestoreItemText
                    ?.Replace("%device", device)
                    .Replace("%time", time) ?? $"{device}  {time}";

                var modeText = AppConfig.LogonRestoreMode switch
                {
                    1 => AppString.Dialog.RestoreMode2,
                    2 => AppString.Dialog.RestoreMode3,
                    _ => AppString.Dialog.RestoreMode1
                };

                LogonRestoreHintText.Text = $"{AppString.Other.LogonRestoreBackup ?? "Backup"}: {backupText}";
            }
            catch
            {
                LogonRestoreHintText.Text = null;
            }
        }

        private void LogonRestoreToggle_OnToggled(object sender, RoutedEventArgs e)
        {
            if (isLogonRestoreLoading) return;

            if (LogonRestoreToggle.IsOn)
            {
                // Turning ON: open configuration dialog
                if (!OpenLogonRestoreDialog(isNew: true))
                {
                    // User cancelled – revert toggle
                    isLogonRestoreLoading = true;
                    LogonRestoreToggle.IsOn = false;
                    isLogonRestoreLoading = false;
                    LogonRestoreConfigButton.Visibility = Visibility.Collapsed;
                }
                else
                {
                    LogonRestoreConfigButton.Visibility = Visibility.Visible;
                }
            }
            else
            {
                // Turning OFF: confirm and disable
                var result = AppMessageBox.Show(
                    AppString.Message.ConfirmDisableLogonRestore ?? "Are you sure you want to disable logon restore?",
                    AppString.General.AppName,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    // Revert toggle
                    isLogonRestoreLoading = true;
                    LogonRestoreToggle.IsOn = true;
                    isLogonRestoreLoading = false;
                    return;
                }

                DisableLogonRestoreTask();
                LogonRestoreConfigButton.Visibility = Visibility.Collapsed;
                UpdateLogonRestoreHintText(false);
            }
        }

        private void LogonRestoreConfigButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenLogonRestoreDialog(isNew: false);
        }

        /// <summary>Opens the logon-restore configuration dialog.
        /// Returns true if the user confirmed a valid configuration.</summary>
        private bool OpenLogonRestoreDialog(bool isNew)
        {
            if (BackupEntries.Count == 0)
            {
                AppMessageBox.Show(
                    AppString.Message.LogonRestoreNoBackup ?? "No backups available. Create a backup first.",
                    AppString.General.AppName);
                return false;
            }

            // Build the entries list for the dialog
            var dialogEntries = BackupEntries
                .Select(e => new LogonRestoreDialog.BackupEntry
                {
                    FilePath = e.FilePath,
                    DisplayText = e.DisplayText
                })
                .ToList();

            // Resolve pre-selected scenes from stored indices
            List<string> preSelectedScenes = null;
            if (!isNew)
            {
                var storedScenes = LogonTaskHelper.ParseSceneTexts(AppConfig.LogonRestoreScenes);
                if (storedScenes.Count > 0)
                    preSelectedScenes = storedScenes;
            }

            var dlg = new LogonRestoreDialog
            {
                Title = AppString.Dialog.SetLogonRestore ?? "Configure logon restore",
                BackupEntries = dialogEntries,
                SelectedFilePath = isNew ? null : AppConfig.LogonRestoreFilePath,
                SelectedScenes = preSelectedScenes,
                SelectedModeIndex = isNew ? 0 : AppConfig.LogonRestoreMode
            };

            if (!dlg.ShowDialog())
                return false;

            if (string.IsNullOrEmpty(dlg.ResultFilePath))
            {
                AppMessageBox.Show(
                    AppString.Message.LogonRestoreNoBackup ?? "No backup selected.",
                    AppString.General.AppName);
                return false;
            }

            if (dlg.ResultScenes == null || dlg.ResultScenes.Count == 0)
            {
                AppMessageBox.Show(AppString.Message.NotChooseAnyRestore, AppString.General.AppName);
                return false;
            }

            // Save config
            AppConfig.LogonRestoreFilePath = dlg.ResultFilePath;
            AppConfig.LogonRestoreScenes = LogonTaskHelper.BuildScenesString(dlg.ResultScenes);
            AppConfig.LogonRestoreMode = dlg.ResultModeIndex;

            // Create / update the scheduled task
            var ok = LogonTaskHelper.EnableTask();
            if (!ok)
            {
                AppMessageBox.Show(
                    AppString.Message.LogonRestoreFailed ?? "Failed to create the scheduled task!",
                    AppString.General.AppName);
                return false;
            }

            AppMessageBox.Show(
                AppString.Message.LogonRestoreEnabled ?? "Logon restore task enabled successfully!",
                AppString.General.AppName);

            UpdateLogonRestoreHintText(true);
            return true;
        }

        private static void DisableLogonRestoreTask()
        {
            LogonTaskHelper.DisableTask();
            AppConfig.LogonRestoreFilePath = "";
            AppConfig.LogonRestoreScenes = "";
            AppConfig.LogonRestoreMode = 0;
        }

        // ── Inner types ───────────────────────────────────────────────────────

        public sealed class BackupEntry
        {
            public BackupEntry(string filePath, string deviceName, DateTime createTime)
            {
                FilePath = filePath;
                DeviceName = deviceName;
                CreateTime = createTime;
            }

            public string FilePath { get; }
            public string DeviceName { get; }
            public DateTime CreateTime { get; }
            public string CreateTimeText => CreateTime.ToString("G");
            public string DisplayText => AppString.Other.RestoreItemText
                .Replace("%device", DeviceName)
                .Replace("%time", CreateTimeText);
        }

        private sealed class RestoreResultEntry
        {
            public RestoreResultEntry(string location, string value)
            {
                Location = location;
                Value = value;
            }

            public string Location { get; }
            public string Value { get; }
        }
    }
}
