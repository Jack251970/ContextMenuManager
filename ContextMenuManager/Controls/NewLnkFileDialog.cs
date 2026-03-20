using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using WpfButton = System.Windows.Controls.Button;
using WpfRadioButton = System.Windows.Controls.RadioButton;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfOrientation = System.Windows.Controls.Orientation;

namespace ContextMenuManager.Controls
{
    internal sealed partial class NewLnkFileDialog
    {
        public string ItemText { get; set; }
        public string ItemFilePath { get; set; }
        public string Arguments { get; set; }
        public string FileFilter { get; set; }

        public bool ShowDialog() => RunDialog(null);

        public bool RunDialog(MainWindow owner)
        {
            var dialog = ContentDialogHost.CreateDialog(AppString.Other.NewItem, owner);
            dialog.PrimaryButtonText = ResourceString.OK;
            dialog.CloseButtonText = ResourceString.Cancel;

            var txtText = new WpfTextBox { Margin = new Thickness(0, 0, 0, 12) };
            var txtFilePath = new WpfTextBox { Margin = new Thickness(0, 0, 0, 12) };
            var txtArguments = new WpfTextBox { Margin = new Thickness(0, 0, 0, 12) };

            var rdoFile = new WpfRadioButton
            {
                Content = AppString.SideBar.File,
                IsChecked = true,
                Margin = new Thickness(0, 0, 12, 0)
            };
            var rdoFolder = new WpfRadioButton
            {
                Content = AppString.SideBar.Folder,
                Margin = new Thickness(0, 0, 12, 0)
            };

            var btnBrowse = new WpfButton
            {
                Content = AppString.Dialog.Browse,
                Margin = new Thickness(0, 0, 0, 12),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            btnBrowse.Click += (sender, e) =>
            {
                if (rdoFile.IsChecked == true)
                {
                    BrowseFile(txtFilePath, txtArguments, txtText);
                }
                else
                {
                    BrowseFolder(txtFilePath, txtText);
                }
            };

            txtFilePath.TextChanged += (sender, e) =>
            {
                var filePath = txtFilePath.Text;
                if (Path.GetExtension(filePath).ToLower() == ".lnk")
                {
                    using var shortcut = new ShellLink(filePath);
                    if (File.Exists(shortcut.TargetPath))
                    {
                        txtFilePath.Text = shortcut.TargetPath;
                    }
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
                    txtArguments,
                    new WpfStackPanel
                    {
                        Orientation = WpfOrientation.Horizontal,
                        Margin = new Thickness(0, 8, 0, 0),
                        Children = { rdoFile, rdoFolder }
                    }
                }
            };

            var result = ContentDialogHost.RunBlocking(dialog.ShowAsync, owner);
            if (result != ContentDialogResult.Primary)
            {
                return false;
            }

            ItemText = txtText.Text;
            ItemFilePath = txtFilePath.Text;
            Arguments = txtArguments.Text;

            if (ItemText.IsNullOrWhiteSpace())
            {
                AppMessageBox.Show(AppString.Message.TextCannotBeEmpty);
                return false;
            }
            if (ItemFilePath.IsNullOrWhiteSpace())
            {
                AppMessageBox.Show(AppString.Message.CommandCannotBeEmpty);
                return false;
            }
            if (rdoFile.IsChecked == true && !ObjectPath.GetFullFilePath(ItemFilePath, out _))
            {
                AppMessageBox.Show(AppString.Message.FileNotExists);
                return false;
            }
            if (rdoFolder.IsChecked == true && !Directory.Exists(ItemFilePath))
            {
                AppMessageBox.Show(AppString.Message.FolderNotExists);
                return false;
            }

            return true;
        }

        private void BrowseFile(WpfTextBox txtFilePath, WpfTextBox txtArguments, WpfTextBox txtText)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = FileFilter;
            dlg.DereferenceLinks = false;
            if (dlg.ShowDialog() == true)
            {
                var filePath = dlg.FileName;
                var arguments = "";
                txtFilePath.Text = filePath;
                txtText.Text = Path.GetFileNameWithoutExtension(filePath);

                var extension = Path.GetExtension(filePath).ToLower();
                if (extension == ".lnk")
                {
                    using var shortcut = new ShellLink(filePath);
                    if (File.Exists(shortcut.TargetPath))
                    {
                        txtFilePath.Text = shortcut.TargetPath;
                        txtArguments.Text = shortcut.Arguments;
                    }
                }
            }
        }

        private void BrowseFolder(WpfTextBox txtFilePath, WpfTextBox txtText)
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            if (Directory.Exists(txtFilePath.Text))
                dlg.SelectedPath = txtFilePath.Text;
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtFilePath.Text = dlg.SelectedPath;
                txtText.Text = Path.GetFileName(dlg.SelectedPath);
            }
        }
    }
}
