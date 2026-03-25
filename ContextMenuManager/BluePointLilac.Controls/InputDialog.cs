using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Drawing;
using WpfScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfTextWrapping = System.Windows.TextWrapping;

namespace ContextMenuManager.Controls
{
    public sealed class InputDialog
    {
        /// <summary>输入对话框标题</summary>
        public string Title { get; set; } = AppString.General.AppName;

        /// <summary>输入对话框文本框文本</summary>
        public string Text { get; set; }
        public Size Size { get; set; }

        public bool ShowDialog() => RunDialog(null);

        public bool RunDialog(MainWindow owner)
        {
            var dialog = ContentDialogHost.CreateDialog(Title, owner);

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
            var result = ContentDialogHost.RunBlocking(owner => dialog.ShowAsync(owner), owner);
            var accepted = result == ContentDialogResult.Primary;
            Text = accepted ? inputBox.Text : null;
            return accepted;
        }
    }
}
