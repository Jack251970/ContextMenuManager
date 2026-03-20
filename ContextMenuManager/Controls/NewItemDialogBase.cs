using ContextMenuManager.Methods;
using System.Windows;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfButton = System.Windows.Controls.Button;
using WpfGrid = System.Windows.Controls.Grid;
using WpfColumnDefinition = System.Windows.Controls.ColumnDefinition;
using iNKORE.UI.WPF.Modern.Controls.Helpers;

namespace ContextMenuManager.Controls
{
    internal class NewItemDialogBase
    {
        public string ItemText { get; set; }
        public string ItemFilePath { get; set; }
        public string Arguments { get; set; }

        public string ItemCommand
        {
            get
            {
                var filePath = ItemFilePath;
                var arguments = Arguments;
                if (filePath.IsNullOrWhiteSpace()) return arguments ?? string.Empty;
                if (arguments.IsNullOrWhiteSpace()) return filePath;
                if (filePath.Contains(" ")) filePath = $"\"{filePath}\"";
                // Note: The original logic for arguments containing double quotes was a bit specific,
                // I'll keep it as provided.
                if (!arguments.Contains("\"")) arguments = $"\"{arguments}\"";
                return $"{filePath} {arguments}";
            }
        }

        protected WpfTextBox txtText;
        protected WpfTextBox txtFilePath;
        protected WpfTextBox txtArguments;

        protected virtual void CreateCommonControls(WpfStackPanel parent)
        {
            txtText = new WpfTextBox
            {
                Text = ItemText ?? string.Empty,
                Margin = new Thickness(0, 0, 0, 12)
            };
            ControlHelper.SetHeader(txtText, AppString.Dialog.ItemText);

            var grid = new WpfGrid { Margin = new Thickness(0, 0, 0, 12) };
            grid.ColumnDefinitions.Add(new WpfColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new WpfColumnDefinition { Width = GridLength.Auto });

            txtFilePath = new WpfTextBox
            {
                Text = ItemFilePath ?? string.Empty,
                Margin = new Thickness(0, 0, 8, 0)
            };
            ControlHelper.SetHeader(txtFilePath, AppString.Dialog.ItemCommand);
            WpfGrid.SetColumn(txtFilePath, 0);

            var btnBrowse = new WpfButton
            {
                Content = AppString.Dialog.Browse,
                VerticalAlignment = VerticalAlignment.Bottom,
                Padding = new Thickness(12, 4, 12, 4)
            };
            WpfGrid.SetColumn(btnBrowse, 1);
            btnBrowse.Click += (s, e) => OnBrowseClick();

            grid.Children.Add(txtFilePath);
            grid.Children.Add(btnBrowse);

            txtArguments = new WpfTextBox
            {
                Text = Arguments ?? string.Empty,
                Margin = new Thickness(0, 0, 0, 12)
            };
            ControlHelper.SetHeader(txtArguments, AppString.Dialog.CommandArguments);

            parent.Children.Add(txtText);
            parent.Children.Add(grid);
            parent.Children.Add(txtArguments);
        }

        protected virtual void OnBrowseClick() { }

        protected virtual void SyncData()
        {
            ItemText = txtText.Text;
            ItemFilePath = txtFilePath.Text;
            Arguments = txtArguments.Text;
        }
    }
}
