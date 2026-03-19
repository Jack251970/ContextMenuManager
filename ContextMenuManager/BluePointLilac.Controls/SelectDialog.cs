using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Windows.Forms;
using WpfComboBox = System.Windows.Controls.ComboBox;

namespace BluePointLilac.Controls
{
    public class SelectDialog : CommonDialog
    {
        public string Title { get; set; }
        public string Selected { get; set; }
        public int SelectedIndex { get; set; }
        public string[] Items { get; set; }
        public bool CanEdit { get; set; }

        public override void Reset() { }

        protected override bool RunDialog(IntPtr hwndOwner)
        {
            var dialog = ContentDialogHost.CreateDialog(Title, hwndOwner);
            dialog.PrimaryButtonText = ResourceString.OK;
            dialog.CloseButtonText = ResourceString.Cancel;

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
            var result = ContentDialogHost.RunBlocking(owner => dialog.ShowAsync(owner), hwndOwner);
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
