using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using WpfButton = System.Windows.Controls.Button;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace ContextMenuManager.Controls
{
    internal sealed class NewOpenWithDialog
    {
        public string RegPath { get; private set; }

        public bool ShowDialog() => RunDialog(null);

        public bool RunDialog(MainWindow owner)
        {
            var dialog = ContentDialogHost.CreateDialog(AppString.Other.NewItem, owner);

            var txtText = new WpfTextBox { Margin = new Thickness(0, 0, 0, 12) };
            var txtFilePath = new WpfTextBox { Margin = new Thickness(0, 0, 0, 12) };
            var txtArguments = new WpfTextBox { Text = "\"%1\"", Margin = new Thickness(0, 0, 0, 12) };

            var btnBrowse = new WpfButton
            {
                Content = AppString.Dialog.Browse,
                Margin = new Thickness(0, 0, 0, 12),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            btnBrowse.Click += (sender, e) =>
            {
                var dlg = new OpenFileDialog
                {
                    Filter = $"{AppString.Dialog.Program}|*.exe"
                };
                if (dlg.ShowDialog() == true)
                {
                    txtFilePath.Text = dlg.FileName;
                    txtArguments.Text = "\"%1\"";
                    txtText.Text = FileVersionInfo.GetVersionInfo(dlg.FileName).FileDescription;
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

            if (string.IsNullOrEmpty(itemText))
            {
                AppMessageBox.Show(AppString.Message.TextCannotBeEmpty);
                return false;
            }

            var itemCommand = GetItemCommand(itemFilePath, arguments);
            if (itemCommand.IsNullOrWhiteSpace())
            {
                AppMessageBox.Show(AppString.Message.CommandCannotBeEmpty);
                return false;
            }

            var filePath = ObjectPath.ExtractFilePath(itemFilePath);
            var fileName = Path.GetFileName(filePath);
            var appRegPath = $@"{RegistryEx.CLASSES_ROOT}\Applications\{fileName}";
            var commandPath = $@"{appRegPath}\shell\open\command";

            using (var key = RegistryEx.GetRegistryKey(commandPath))
            {
                var path = ObjectPath.ExtractFilePath(key?.GetValue("")?.ToString());
                var name = Path.GetFileName(path);
                if (filePath != null && filePath.Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    AppMessageBox.Show(AppString.Message.HasBeenAdded);
                    return false;
                }
                if (fileName == null || fileName.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    AppMessageBox.Show(AppString.Message.UnsupportedFilename);
                    return false;
                }
            }

            using (var key = RegistryEx.GetRegistryKey(appRegPath, true, true))
            {
                key.SetValue("FriendlyAppName", itemText);
            }
            using var cmdKey = RegistryEx.GetRegistryKey(commandPath, true, true);
            cmdKey.SetValue("", itemCommand);
            RegPath = cmdKey.Name;

            return true;
        }

        private static string GetItemCommand(string filePath, string arguments)
        {
            if (arguments.IsNullOrWhiteSpace()) return filePath;
            if (filePath.IsNullOrWhiteSpace()) return arguments;
            if (filePath.Contains(' ')) filePath = $"\"{filePath}\"";
            if (!arguments.Contains('\"')) arguments = $"\"{arguments}\"";
            return $"{filePath} {arguments}";
        }
    }
}
