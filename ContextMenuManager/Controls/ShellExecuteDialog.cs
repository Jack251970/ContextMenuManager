using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using WpfRadioButton = System.Windows.Controls.RadioButton;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfGrid = System.Windows.Controls.Grid;
using WpfLabel = System.Windows.Controls.Label;

namespace ContextMenuManager.Controls
{
    internal sealed class ShellExecuteDialog
    {
        public string Verb { get; set; }
        public int WindowStyle { get; set; }

        public bool ShowDialog() => RunDialog(null);

        public bool RunDialog(MainWindow owner)
        {
            var dialog = ContentDialogHost.CreateDialog("ShellExecute", owner);
            dialog.PrimaryButtonText = ResourceString.OK;
            dialog.CloseButtonText = ResourceString.Cancel;
            dialog.DefaultButton = ContentDialogButton.Primary;

            var stackPanel = new WpfStackPanel { MinWidth = 300 };

            // Verb Selection
            var verbs = new[] { "open", "runas", "edit", "print", "find", "explore" };
            var radioButtons = new WpfRadioButton[verbs.Length];
            var verbPanel = new WpfStackPanel { Margin = new Thickness(0, 0, 0, 16) };
            verbPanel.Children.Add(new WpfLabel { Content = "Verb", FontWeight = FontWeights.Bold });

            for (int i = 0; i < verbs.Length; i++)
            {
                radioButtons[i] = new WpfRadioButton
                {
                    Content = verbs[i],
                    IsChecked = i == 0,
                    Margin = new Thickness(0, 4, 0, 4)
                };
                verbPanel.Children.Add(radioButtons[i]);
            }
            stackPanel.Children.Add(verbPanel);

            // WindowStyle Selection
            var stylePanel = new WpfStackPanel { Orientation = Orientation.Horizontal };
            stylePanel.Children.Add(new WpfLabel { Content = "WindowStyle", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
            
            var numberBox = new NumberBox
            {
                Value = 1,
                Minimum = 0,
                Maximum = 10,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                Width = 120
            };
            stylePanel.Children.Add(numberBox);
            stackPanel.Children.Add(stylePanel);

            dialog.Content = stackPanel;

            var result = ContentDialogHost.RunBlocking(dialog.ShowAsync, owner);
            if (result == ContentDialogResult.Primary)
            {
                for (int i = 0; i < verbs.Length; i++)
                {
                    if (radioButtons[i].IsChecked == true)
                    {
                        Verb = verbs[i];
                        break;
                    }
                }
                WindowStyle = (int)numberBox.Value;
                return true;
            }
            return false;
        }

        public static string GetCommand(string fileName, string arguments, string verb, int windowStyle, string directory = null)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                ObjectPath.GetFullFilePath(fileName, out var filePath);
                directory = Path.GetDirectoryName(filePath);
            }

            if (Environment.OSVersion.Version.Major >= 10)
            {
                string winStyleStr;
                switch (windowStyle)
                {
                    case 0: winStyleStr = "Hidden"; break;
                    case 1: winStyleStr = "Normal"; break;
                    case 2: winStyleStr = "Minimized"; break;
                    case 3: winStyleStr = "Maximized"; break;
                    default: winStyleStr = "Normal"; break;
                }

                string psFileName = "'" + fileName.Replace("'", "''") + "'";
                string psVerb = "'" + verb.Replace("'", "''") + "'";
                string psArgs = "'" + arguments.Replace("'", "''") + "'";

                string psDirPart = "";
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    psDirPart = $"-WorkingDirectory '{directory.Replace("'", "''")}'";
                }

                return $"powershell -WindowStyle Hidden -Command \"Start-Process -FilePath {psFileName} -ArgumentList {psArgs} {psDirPart} -Verb {psVerb} -WindowStyle {winStyleStr}\"";
            }
            else
            {
                arguments = arguments.Replace("\"", "\"\"");
                return "mshta vbscript:createobject(\"shell.application\").shellexecute" +
                    $"(\"{fileName}\",\"{arguments}\",\"{directory}\",\"{verb}\",{windowStyle})(close)";
            }
        }
    }
}
