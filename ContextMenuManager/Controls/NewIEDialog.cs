using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using WpfButton = System.Windows.Controls.Button;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace ContextMenuManager.Controls
{
    internal sealed class NewIEDialog
    {
        public string RegPath { get; private set; }

        public bool ShowDialog()
        {
            return RunDialog(null);
        }

        public bool RunDialog(MainWindow owner)
        {
            var dialog = ContentDialogHost.CreateDialog(AppString.Other.NewItem, owner);

            var txtText = new WpfTextBox { Margin = new Thickness(0, 0, 0, 12) };
            var txtFilePath = new WpfTextBox { Margin = new Thickness(0, 0, 0, 12) };
            var txtArguments = new WpfTextBox { Margin = new Thickness(0, 0, 0, 12) };

            var btnBrowse = new WpfButton
            {
                Content = AppString.Dialog.Browse,
                Margin = new Thickness(0, 0, 0, 12),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            btnBrowse.Click += (sender, e) =>
            {
                var dlg = new OpenFileDialog();
                if (dlg.ShowDialog() == true)
                {
                    txtFilePath.Text = dlg.FileName;
                    txtText.Text = Path.GetFileNameWithoutExtension(dlg.FileName);
                }
            };

            dialog.Content = new WpfStackPanel
            {
                Children =
                {
                    new WpfTextBlock { Text = AppString.Dialog.ItemText, Margin = new Thickness(0, 0, 0, 4) },
                    txtText,
                    new WpfTextBlock { Text = AppString.Dialog.ItemCommand, Margin = new Thickness(0, 0, 0, 4) },
                    txtFilePath,
                    btnBrowse,
                    new WpfTextBlock { Text = AppString.Dialog.CommandArguments, Margin = new Thickness(0, 0, 0, 4) },
                    txtArguments
                }
            };

            var result = ContentDialogHost.RunBlocking(dialog.ShowAsync, owner);
            if (result != ContentDialogResult.Primary)
            {
                return false;
            }

            var itemText = txtText.Text;
            var itemFilePath = txtFilePath.Text;
            var arguments = txtArguments.Text;

            if (string.IsNullOrWhiteSpace(itemText))
            {
                AppMessageBox.Show(AppString.Message.TextCannotBeEmpty);
                return false;
            }

            var itemCommand = GetItemCommand(itemFilePath, arguments);
            if (string.IsNullOrWhiteSpace(itemCommand))
            {
                AppMessageBox.Show(AppString.Message.CommandCannotBeEmpty);
                return false;
            }

            RegPath = $@"{IEList.IEPath}\{IEItem.MeParts[0]}\{itemText.Replace("\\", "")}";
            Registry.SetValue(RegPath, "", itemCommand);
            return true;
        }

        private static string GetItemCommand(string filePath, string arguments)
        {
            if (string.IsNullOrWhiteSpace(arguments)) return filePath;
            if (string.IsNullOrWhiteSpace(filePath)) return arguments;
            if (filePath.Contains(" ")) filePath = $"\"{filePath}\"";
            if (!arguments.Contains("\"")) arguments = $"\"{arguments}\"";
            return $"{filePath} {arguments}";
        }
    }
}