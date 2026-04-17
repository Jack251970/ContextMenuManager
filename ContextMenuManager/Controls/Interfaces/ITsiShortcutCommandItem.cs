using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Controls.Helpers;
using System.Windows.Controls;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace ContextMenuManager.Controls.Interfaces
{
    internal interface ITsiShortcutCommandItem
    {
        ShellLink ShellLink { get; }
        ShortcutCommandMenuItem TsiChangeCommand { get; set; }
        MenuFlyout Flyout { get; set; }
    }

    internal sealed class ShortcutCommandMenuItem : RToolStripMenuItem
    {
        public ShortcutCommandMenuItem(ITsiShortcutCommandItem item) : base(AppString.Menu.ChangeCommand)
        {
            item.Flyout.Opened += (sender, e) =>
            {
                Visible = !string.IsNullOrEmpty(item.ShellLink?.TargetPath);
            };
        }

        public bool ChangeCommand(ShellLink shellLink)
        {
            var dlg = new CommandDialog
            {
                Command = shellLink.TargetPath,
                Arguments = shellLink.Arguments
            };
            if (!dlg.ShowDialog()) return false;
            shellLink.TargetPath = dlg.Command;
            shellLink.Arguments = dlg.Arguments;
            shellLink.Save();
            return true;
        }

        private sealed class CommandDialog
        {
            public string Command { get; set; }
            public string Arguments { get; set; }

            public bool ShowDialog()
            {
                return RunDialog(null);
            }

            public bool RunDialog(MainWindow owner)
            {
                var dialog = ContentDialogHost.CreateDialog(AppString.Menu.ChangeCommand, owner);

                var txtCommand = new WpfTextBox
                {
                    Text = Command ?? string.Empty,
                    Margin = new System.Windows.Thickness(0, 0, 0, 16),
                    MinWidth = 400
                };
                ControlHelper.SetHeader(txtCommand, AppString.Dialog.ItemCommand);

                var txtArguments = new WpfTextBox
                {
                    Text = Arguments ?? string.Empty
                };
                ControlHelper.SetHeader(txtArguments, AppString.Dialog.CommandArguments);

                dialog.Content = new WpfStackPanel
                {
                    Children =
                    {
                        txtCommand,
                        txtArguments
                    }
                };

                var result = ContentDialogHost.RunBlocking(dialog.ShowAsync, owner);
                if (result == ContentDialogResult.Primary)
                {
                    Command = txtCommand.Text;
                    Arguments = txtArguments.Text;
                    return true;
                }
                return false;
            }
        }
    }
}
