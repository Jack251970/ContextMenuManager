using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfRadioButton = System.Windows.Controls.RadioButton;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfOrientation = System.Windows.Controls.Orientation;

namespace ContextMenuManager.Controls
{
    internal sealed class NewShellDialog : NewItemDialogBase
    {
        public string ShellPath { get; set; }//传入的Shell注册表路径
        public string ScenePath { get; set; }//菜单项所处环境注册表路径
        public string NewItemRegPath { get; private set; }//返回的新ShellItem的注册表路径
        public string NewItemKeyName => RegistryEx.GetKeyName(NewItemRegPath);

        public bool ShowDialog() => RunDialog(null);

        public bool RunDialog(MainWindow owner)
        {
            var dialog = ContentDialogHost.CreateDialog(AppString.Other.NewItem, owner);

            var stackPanel = new WpfStackPanel { MinWidth = 460 };
            CreateCommonControls(stackPanel);

            var rdoSingle = new WpfRadioButton
            {
                Content = AppString.Dialog.SingleMenu,
                IsChecked = true,
                Margin = new Thickness(0, 0, 16, 0)
            };
            var rdoMulti = new WpfRadioButton
            {
                Content = AppString.Dialog.MultiMenu,
                Margin = new Thickness(0, 0, 16, 0)
            };
            var chkSE = new WpfCheckBox
            {
                Content = "ShellExecute",
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 0
            };
            // Logic for ShellExecute needs to be handled. Since ShellExecuteCheckBox was a WinForms control with its own dialog,
            // we'll need to store its state here if it's checked.
            string seVerb = null;
            int seWindowStyle = 0;

            chkSE.Click += (s, e) =>
            {
                if (chkSE.IsChecked == true)
                {
                    var dlg = new ShellExecuteDialog();
                    if (dlg.ShowDialog())
                    {
                        seVerb = dlg.Verb;
                        seWindowStyle = dlg.WindowStyle;
                    }
                    else
                    {
                        chkSE.IsChecked = false;
                    }
                }
            };

            rdoMulti.Checked += (s, e) =>
            {
                chkSE.IsChecked = false;
                if (WinOsVersion.Current == WinOsVersion.Vista)
                {
                    AppMessageBox.Show(AppString.Message.VistaUnsupportedMulti);
                    rdoSingle.IsChecked = true;
                    return;
                }
                txtFilePath.IsEnabled = txtArguments.IsEnabled = chkSE.IsEnabled = false;
            };
            rdoMulti.Unchecked += (s, e) =>
            {
                txtFilePath.IsEnabled = txtArguments.IsEnabled = chkSE.IsEnabled = true;
            };

            var radioPanel = new WpfStackPanel
            {
                Orientation = WpfOrientation.Horizontal,
                Margin = new Thickness(0, 12, 0, 0),
                Children = { rdoSingle, rdoMulti, chkSE }
            };
            stackPanel.Children.Add(radioPanel);

            dialog.Content = stackPanel;

            var result = ContentDialogHost.RunBlocking(dialog.ShowAsync, owner);
            if (result == ContentDialogResult.Primary)
            {
                if (string.IsNullOrWhiteSpace(txtText.Text))
                {
                    AppMessageBox.Show(AppString.Message.TextCannotBeEmpty);
                    return RunDialog(owner); // Re-show if invalid
                }
                SyncData();
                AddNewItem(rdoMulti.IsChecked == true, chkSE.IsChecked == true, seVerb, seWindowStyle);
                return true;
            }

            return false;
        }

        protected override void OnBrowseClick()
        {
            var dlg = new OpenFileDialog
            {
                DereferenceLinks = false,
                Filter = $"{AppString.Dialog.Program}|*.exe|{AppString.Dialog.AllFiles}|*"
            };
            if (dlg.ShowDialog() != true) return;
            var filePath = dlg.FileName;
            var arguments = "";
            txtText.Text = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath).ToLower();
            if (extension == ".lnk")
            {
                using var shellLink = new ShellLink(filePath);
                filePath = shellLink.TargetPath;
                arguments = shellLink.Arguments;
                extension = Path.GetExtension(filePath);
            }
            var exePath = FileExtension.GetExtentionInfo(FileExtension.AssocStr.Executable, extension);
            if (File.Exists(exePath))
            {
                txtFilePath.Text = exePath;
                txtArguments.Text = filePath;
                if (!string.IsNullOrWhiteSpace(arguments)) txtArguments.Text += " " + arguments;
            }
            else
            {
                txtFilePath.Text = filePath;
                txtArguments.Text = arguments;
            }

            if (Array.FindIndex(DirScenePaths, path
               => ScenePath.StartsWith(path, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                if (ScenePath != ShellList.MENUPATH_BACKGROUND)
                {
                    if (!string.IsNullOrWhiteSpace(txtArguments.Text)) txtArguments.Text += " ";
                    txtArguments.Text += "\"%V\"";//自动加目录后缀
                }
            }
            else if (Array.FindIndex(FileObjectsScenePaths, path
               => ScenePath.StartsWith(path, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                if (!string.IsNullOrWhiteSpace(txtArguments.Text)) txtArguments.Text += " ";
                txtArguments.Text += "\"%1\"";//自动加文件对象后缀
            }
        }

        private static readonly string[] DirScenePaths = {
            ShellList.MENUPATH_DIRECTORY,
            ShellList.MENUPATH_BACKGROUND,
            $@"{ShellList.SYSFILEASSPATH}\Directory."
        };
        private static readonly string[] FileObjectsScenePaths = {
            ShellList.MENUPATH_FILE,
            ShellList.MENUPATH_FOLDER,
            ShellList.MENUPATH_ALLOBJECTS,
            ShellList.SYSFILEASSPATH,
            ShellList.MENUPATH_UNKNOWN,
            ShellList.MENUPATH_UWPLNK
        };

        private void AddNewItem(bool isMulti, bool isSE, string seVerb, int seWindowStyle)
        {
            using var shellKey = RegistryEx.GetRegistryKey(ShellPath, true, true);
            var keyName = "Item";
            NewItemRegPath = ObjectPath.GetNewPathWithIndex($@"{ShellPath}\{keyName}", ObjectPath.PathType.Registry, 0);
            keyName = RegistryEx.GetKeyName(NewItemRegPath);

            using var key = shellKey.CreateSubKey(keyName, true);
            key.SetValue("MUIVerb", ItemText);
            if (isMulti)
                key.SetValue("SubCommands", "");
            else
            {
                if (!string.IsNullOrWhiteSpace(ItemCommand))
                {
                    string command;
                    if (!isSE) command = ItemCommand;
                    else command = ShellExecuteDialog.GetCommand(ItemFilePath, Arguments, seVerb, seWindowStyle);
                    key.CreateSubKey("command", true).SetValue("", command);
                }
            }
        }
    }
}
