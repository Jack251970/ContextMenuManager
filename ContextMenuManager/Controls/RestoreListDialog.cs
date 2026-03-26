using ContextMenuManager.Methods;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfDataGrid = System.Windows.Controls.DataGrid;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTextBlock = System.Windows.Controls.TextBlock;

namespace ContextMenuManager.Controls
{
    internal sealed class RestoreListDialog
    {
        public List<RestoreChangedItem> RestoreData { get; set; }

        public bool ShowDialog()
        {
            return RunDialog(null);
        }

        public bool RunDialog(MainWindow owner)
        {
            var dialog = ContentDialogHost.CreateDialog(AppString.Dialog.RestoreDetails, owner);

            var restoreCount = RestoreData?.Count ?? 0;
            var lblRestore = new WpfTextBlock
            {
                Text = AppString.Message.RestoreSucceeded.Replace("%s", restoreCount.ToString()),
                Margin = new Thickness(0, 0, 0, 12),
                TextWrapping = TextWrapping.Wrap
            };

            var dataGrid = new WpfDataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Single,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                GridLinesVisibility = DataGridGridLinesVisibility.None,
                CanUserResizeRows = false,
                CanUserAddRows = false,
                Height = 340
            };

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = AppString.Dialog.ItemLocation,
                Binding = new System.Windows.Data.Binding("Location"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = AppString.Dialog.RestoredValue,
                Binding = new System.Windows.Data.Binding("Value"),
                Width = DataGridLength.Auto
            });

            var items = new List<RestoreDisplayItem>();
            if (RestoreData != null)
            {
                foreach (var item in RestoreData)
                {
                    var scene = item.BackupScene;
                    var sceneText = BackupHelper.BackupScenesText[(int)scene];
                    var changedValue = item.ItemData;
                    if (changedValue == false.ToString()) changedValue = AppString.Dialog.Disabled;
                    if (changedValue == true.ToString()) changedValue = AppString.Dialog.Enabled;

                    string location;
                    if (BackupHelper.TypeBackupScenesText.Contains(sceneText))
                    {
                        location = $"{AppString.ToolBar.Type} -> {sceneText} -> {item.KeyName}";
                    }
                    else if (BackupHelper.RuleBackupScenesText.Contains(sceneText))
                    {
                        location = $"{AppString.ToolBar.Rule} -> {sceneText} -> {item.KeyName}";
                    }
                    else
                    {
                        location = $"{AppString.ToolBar.Home} -> {sceneText} -> {item.KeyName}";
                    }

                    items.Add(new RestoreDisplayItem { Location = location, Value = changedValue });
                }
            }

            dataGrid.ItemsSource = items;

            dialog.Content = new WpfStackPanel
            {
                Children = { lblRestore, dataGrid }
            };

            ContentDialogHost.RunBlocking(dialog.ShowAsync, owner);
            return true;
        }

        private class RestoreDisplayItem
        {
            public string Location { get; set; }
            public string Value { get; set; }
        }
    }
}
