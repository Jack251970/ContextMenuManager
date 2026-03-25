using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using WpfComboBox = System.Windows.Controls.ComboBox;

namespace ContextMenuManager.Controls
{
    public class SelectDialog
    {
        public string Title { get; set; }
        public string Selected { get; set; }
        public int SelectedIndex { get; set; }
        public string[] Items { get; set; }
        public bool CanEdit { get; set; }

        public bool ShowDialog() => RunDialog(null);

        public bool RunDialog(MainWindow owner)
        {
            var dialog = ContentDialogHost.CreateDialog(Title, owner);

            var comboBox = new WpfComboBox
            {
                IsEditable = CanEdit,
                IsTextSearchEnabled = true,
                MinWidth = 320,
                ItemsSource = Items ?? Array.Empty<string>()
            };

            if (Selected != null)
            {
                comboBox.Text = Selected;
            }
            else if (SelectedIndex >= 0 && SelectedIndex < (Items?.Length ?? 0))
            {
                comboBox.SelectedIndex = SelectedIndex;
            }

            dialog.Content = comboBox;
            var result = ContentDialogHost.RunBlocking(dialog.ShowAsync, owner);
            if (result != ContentDialogResult.Primary)
            {
                return false;
            }

            SelectedIndex = comboBox.SelectedIndex;
            Selected = comboBox.SelectedItem as string ?? comboBox.Text;
            return true;
        }
    }
}
