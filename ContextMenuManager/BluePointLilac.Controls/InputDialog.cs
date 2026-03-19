using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Drawing;
using System.Windows.Forms;
using WpfScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfTextWrapping = System.Windows.TextWrapping;

namespace BluePointLilac.Controls
{
    public sealed class InputDialog : CommonDialog
    {
        /// <summary>输入对话框标题</summary>
        public string Title { get; set; } = Application.ProductName;
        /// <summary>输入对话框文本框文本</summary>
        public string Text { get; set; }
        public Size Size { get; set; }

        public override void Reset() { }

        protected override bool RunDialog(IntPtr hwndOwner)
        {
            var dialog = ContentDialogHost.CreateDialog(Title, hwndOwner);
            dialog.PrimaryButtonText = ResourceString.OK;
            dialog.CloseButtonText = ResourceString.Cancel;

            var inputBox = new WpfTextBox
            {
                Text = Text ?? string.Empty,
                AcceptsReturn = true,
                TextWrapping = WpfTextWrapping.Wrap,
                VerticalScrollBarVisibility = WpfScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = WpfScrollBarVisibility.Disabled,
                MinWidth = Math.Max(Size.Width, 340),
                MinHeight = Math.Max(Size.Height, 120)
            };

            dialog.Content = inputBox;
            var result = ContentDialogHost.RunBlocking(owner => dialog.ShowAsync(owner), hwndOwner);
            var accepted = result == ContentDialogResult.Primary;
            Text = accepted ? inputBox.Text : null;
            return accepted;
        }
    }
}
