using ContextMenuManager.Controls;
using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ContextMenuManager.Views
{
    public partial class LanguagesView : UserControl
    {
        private bool isLoading;
        private readonly ObservableCollection<LanguageOption> languageOptions = new();

        public ObservableCollection<TranslatorGroup> TranslatorGroups { get; } = new();

        public Window OwnerWindow { get; set; }

        public LanguagesView()
        {
            InitializeComponent();
            DataContext = this;
            LoadLabels();
            LoadLanguages();
        }

        public void LoadLanguages()
        {
            isLoading = true;

            LoadLabels();
            languageOptions.Clear();
            TranslatorGroups.Clear();

            languageOptions.Add(new LanguageOption("default", "(default) 简体中文"));

            if (Directory.Exists(AppConfig.LangsDir))
            {
                foreach (var fileName in Directory.GetFiles(AppConfig.LangsDir, "*.ini"))
                {
                    var writer = new IniWriter(fileName);
                    var languageName = writer.GetValue("General", "Language");
                    var translator = writer.GetValue("General", "Translator");
                    var translatorUrl = writer.GetValue("General", "TranslatorUrl");

                    var langCode = Path.GetFileNameWithoutExtension(fileName);
                    if (string.IsNullOrWhiteSpace(languageName))
                    {
                        languageName = langCode;
                    }

                    languageOptions.Add(new LanguageOption(langCode, languageName));

                    var translators = translator.Split(new[] { "\\r\\n", "\\n" }, StringSplitOptions.RemoveEmptyEntries);
                    var urls = translatorUrl.Split(new[] { "\\r\\n", "\\n" }, StringSplitOptions.RemoveEmptyEntries);
                    var group = new TranslatorGroup(languageName);

                    for (var i = 0; i < translators.Length; i++)
                    {
                        var url = urls.Length > i ? urls[i].Trim() : null;
                        group.Translators.Add(new TranslatorEntry(translators[i], url));
                    }

                    if (group.Translators.Count > 0)
                    {
                        TranslatorGroups.Add(group);
                    }
                }
            }

            LanguageComboBox.ItemsSource = languageOptions;
            LanguageComboBox.DisplayMemberPath = nameof(LanguageOption.DisplayName);
            LanguageComboBox.SelectedValuePath = nameof(LanguageOption.Code);
            LanguageComboBox.SelectedIndex = GetSelectedIndex();

            isLoading = false;
        }

        public async void ShowLanguageDialog()
        {
            await DownloadAndApplyLanguageAsync();
        }

        private void LoadLabels()
        {
            PageTitleText.Text = AppString.SideBar.AppLanguage ?? "Language";
            LanguageLabel.Text = AppString.SideBar.AppLanguage ?? "Language";
            LanguageHintText.Text = AppConfig.LangsDir;
            OpenLanguageDirButton.Content = AppString.Menu.FileLocation ?? "Open";
            DownloadLanguageButton.Content = AppString.Dialog.DownloadLanguages ?? "Download";
            TranslateButton.Content = AppString.Dialog.TranslateTool ?? "Translate";
            TranslatorsHeaderText.Text = AppString.Other.Translators ?? "Translators";
            ThankYouText.Text = "Thank you for your translation!";
        }

        private int GetSelectedIndex()
        {
            var index = languageOptions
                .Select((item, i) => new { item, i })
                .FirstOrDefault(x => x.item.Code.Equals(AppConfig.Language, StringComparison.OrdinalIgnoreCase))
                ?.i ?? -1;

            return index >= 0 ? index : 0;
        }

        private void LanguageComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isLoading || LanguageComboBox.SelectedItem is not LanguageOption option)
            {
                return;
            }

            ChangeLanguage(option);
        }

        private void ChangeLanguage(LanguageOption option)
        {
            var currentIndex = GetSelectedIndex();
            if (LanguageComboBox.SelectedIndex == currentIndex)
            {
                return;
            }

            var message = string.Empty;
            if (option.Code != "default")
            {
                var langPath = Path.Combine(AppConfig.LangsDir, option.Code + ".ini");
                message = new IniWriter(langPath).GetValue("Message", "RestartApp");
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                message = AppString.Message.RestartApp;
            }

            var result = AppMessageBox.Show(
                message,
                AppString.General.AppName,
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.OK)
            {
                isLoading = true;
                LanguageComboBox.SelectedIndex = currentIndex;
                isLoading = false;
                return;
            }

            var language = option.Code;
            if (language == CultureInfo.CurrentUICulture.Name || language == "default")
            {
                language = string.Empty;
            }

            AppConfig.Language = language;
            SingleInstance.Restart();
        }

        private void OpenLanguageDirButton_OnClick(object sender, RoutedEventArgs e)
        {
            ExternalProgram.OpenDirectory(AppConfig.LangsDir);
        }

        private async void DownloadLanguageButton_OnClick(object sender, RoutedEventArgs e)
        {
            await DownloadAndApplyLanguageAsync();
        }

        private async Task DownloadAndApplyLanguageAsync()
        {
            using var client = new UAWebClient();
            var apiUrl = AppConfig.RequestUseGithub ? AppConfig.GithubLangsApi : AppConfig.GiteeLangsApi;
            var doc = await client.GetWebJsonToXmlAsync(apiUrl);
            if (doc == null)
            {
                AppMessageBox.Show(AppString.Message.WebDataReadFailed, AppString.General.AppName);
                return;
            }

            var list = doc.FirstChild?.ChildNodes;
            if (list == null || list.Count == 0)
            {
                AppMessageBox.Show(AppString.Message.WebDataReadFailed, AppString.General.AppName);
                return;
            }

            var langs = new string[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                var nameNode = list.Item(i)?.SelectSingleNode("name");
                langs[i] = Path.GetFileNameWithoutExtension(nameNode?.InnerText ?? string.Empty);
            }

            var selection = await PromptLanguageSelectionAsync(langs);
            if (string.IsNullOrWhiteSpace(selection))
            {
                return;
            }

            var fileName = $"{selection}.ini";
            var filePath = Path.Combine(AppConfig.LangsDir, fileName);
            var dirUrl = AppConfig.RequestUseGithub ? AppConfig.GithubLangsRawDir : AppConfig.GiteeLangsRawDir;
            var fileUrl = $"{dirUrl}/{fileName}";

            var downloaded = await client.WebStringToFileAsync(filePath, fileUrl);
            if (!downloaded)
            {
                var openWeb = AppMessageBox.Show(
                    $"{AppString.Message.WebDataReadFailed}\r\n ● {fileName}\r\n{AppString.Message.OpenWebUrl}",
                    AppString.General.AppName,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (openWeb == MessageBoxResult.Yes)
                {
                    ExternalProgram.OpenWebUrl(fileUrl);
                }
                return;
            }

            LoadLanguages();
            var language = new IniWriter(filePath).GetValue("General", "Language");
            var code = string.IsNullOrWhiteSpace(language) ? selection : selection;
            var selected = languageOptions.FirstOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
            if (selected != null)
            {
                LanguageComboBox.SelectedItem = selected;
            }
            else
            {
                LanguageComboBox.SelectedIndex = languageOptions
                    .Select((item, index) => new { item, index })
                    .FirstOrDefault(x => x.item.Code.Equals(selection, StringComparison.OrdinalIgnoreCase))
                    ?.index ?? 0;
            }
        }

        private async Task<string> PromptLanguageSelectionAsync(string[] langs)
        {
            var dialog = ContentDialogHost.CreateDialog(AppString.Dialog.DownloadLanguages);

            var comboBox = new ComboBox
            {
                MinWidth = 320,
                IsEditable = false,
                ItemsSource = langs
            };

            var lang = CultureInfo.CurrentUICulture.Name;
            comboBox.SelectedItem = langs.Contains(lang) ? lang : langs.FirstOrDefault();
            dialog.Content = comboBox;

            var result = await dialog.ShowAsync(OwnerWindow ?? Window.GetWindow(this));
            return result == ContentDialogResult.Primary ? comboBox.SelectedItem as string : null;
        }

        private async void TranslateButton_OnClick(object sender, RoutedEventArgs e)
        {
            await ShowTranslateDialogAsync();
        }

        private async Task ShowTranslateDialogAsync()
        {
            var dialog = ContentDialogHost.CreateDialog(AppString.Dialog.TranslateTool);
            dialog.PrimaryButtonText = AppString.Menu.Save ?? AppString.Dialog.OK;

            var editor = new TranslateEditor();
            dialog.Content = editor.Root;

            var result = await dialog.ShowAsync(OwnerWindow ?? Window.GetWindow(this));
            if (result == ContentDialogResult.Primary)
            {
                editor.Save();
            }
        }

        private sealed class LanguageOption
        {
            public LanguageOption(string code, string displayName)
            {
                Code = code;
                DisplayName = displayName;
            }

            public string Code { get; }
            public string DisplayName { get; }
        }

        public sealed class TranslatorGroup
        {
            public TranslatorGroup(string languageDisplayName)
            {
                LanguageDisplayName = languageDisplayName;
            }

            public string LanguageDisplayName { get; }
            public ObservableCollection<TranslatorEntry> Translators { get; } = new();
        }

        public sealed class TranslatorEntry
        {
            public TranslatorEntry(string name, string url)
            {
                Name = name;
                if (!string.IsNullOrWhiteSpace(url) && !string.Equals(url, "null", StringComparison.OrdinalIgnoreCase))
                {
                    if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                    {
                        Url = uri;
                    }
                }
            }

            public string Name { get; }
            public Uri Url { get; }
        }

        private sealed class TranslateEditor
        {
            private static readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>> EditingDic = BuildEditingDictionary();
            private static readonly IniWriter ReferentialWriter = new();

            private readonly ComboBox sectionComboBox = new() { MinWidth = 220 };
            private readonly ComboBox keyComboBox = new() { MinWidth = 220 };
            private readonly TextBox defaultTextBox = new() { IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MinHeight = 100 };
            private readonly TextBox oldTextBox = new() { IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MinHeight = 100 };
            private readonly TextBox newTextBox = new() { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MinHeight = 120 };

            public TranslateEditor()
            {
                Root = BuildRoot();
                sectionComboBox.ItemsSource = AppString.DefLangReader.Sections;
                sectionComboBox.SelectionChanged += (_, _) =>
                {
                    keyComboBox.ItemsSource = AppString.DefLangReader.GetSectionKeys(Section).ToArray();
                    keyComboBox.SelectedIndex = 0;
                };
                keyComboBox.SelectionChanged += (_, _) => LoadCurrentValues();
                oldTextBox.TextChanged += (_, _) =>
                {
                    if (string.IsNullOrEmpty(newTextBox.Text))
                    {
                        newTextBox.Text = oldTextBox.Text;
                    }
                };
                newTextBox.TextChanged += (_, _) =>
                {
                    if (!string.IsNullOrWhiteSpace(Section) && !string.IsNullOrWhiteSpace(Key))
                    {
                        EditingDic[Section][Key] = Encode(newTextBox.Text);
                    }
                };
                sectionComboBox.SelectedIndex = 0;
            }

            public FrameworkElement Root { get; }

            private string Section => sectionComboBox.SelectedItem as string ?? string.Empty;
            private string Key => keyComboBox.SelectedItem as string ?? string.Empty;

            public void Save()
            {
                var dialog = new SaveFileDialog
                {
                    InitialDirectory = AppConfig.LangsDir,
                    Filter = $"{AppString.SideBar.AppLanguage}|*.ini"
                };

                var language = EditingDic["General"]["Language"];
                var index = language.IndexOf(' ');
                if (index > 0)
                {
                    language = language[..index];
                }
                dialog.FileName = $"{language}.ini";

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                var builder = new StringBuilder();
                foreach (var section in EditingDic.Keys)
                {
                    builder.AppendLine($"[{section}]");
                    foreach (var key in EditingDic[section].Keys)
                    {
                        builder.AppendLine($"{key} = {EditingDic[section][key]}");
                    }
                    builder.AppendLine();
                }

                File.WriteAllText(dialog.FileName, builder.ToString(), Encoding.Unicode);
            }

            private FrameworkElement BuildRoot()
            {
                var browseButton = new Button
                {
                    Content = AppString.Dialog.Browse ?? "Browse",
                    Padding = new Thickness(12, 6, 12, 6),
                    Margin = new Thickness(0, 0, 0, 12),
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                browseButton.Click += (_, _) => SelectReferenceFile();

                return new Grid
                {
                    MinWidth = 640,
                    Children =
                    {
                        new StackPanel
                        {
                            Children =
                            {
                                new StackPanel
                                {
                                    Orientation = Orientation.Horizontal,
                                    Margin = new Thickness(0, 0, 0, 12),
                                    Children =
                                    {
                                        new TextBlock { Text = "Section", VerticalAlignment = VerticalAlignment.Center, Width = 70 },
                                        sectionComboBox,
                                        new TextBlock { Text = "Key", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(24, 0, 0, 0), Width = 40 },
                                        keyComboBox
                                    }
                                },
                                browseButton,
                                new TextBlock { Text = AppString.Dialog.DefaultText ?? "Default" },
                                defaultTextBox,
                                new TextBlock { Text = AppString.Dialog.OldTranslation ?? "Existing Translation", Margin = new Thickness(0, 12, 0, 0) },
                                oldTextBox,
                                new TextBlock { Text = AppString.Dialog.NewTranslation ?? "New Translation", Margin = new Thickness(0, 12, 0, 0) },
                                newTextBox
                            }
                        }
                    }
                };
            }

            private void SelectReferenceFile()
            {
                var dialog = new OpenFileDialog
                {
                    InitialDirectory = AppConfig.LangsDir,
                    Filter = $"{AppString.SideBar.AppLanguage}|*.ini"
                };

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                ReferentialWriter.FilePath = dialog.FileName;
                LoadCurrentValues();
            }

            private void LoadCurrentValues()
            {
                if (string.IsNullOrWhiteSpace(Section) || string.IsNullOrWhiteSpace(Key))
                {
                    return;
                }

                defaultTextBox.Text = Decode(AppString.DefLangReader.GetValue(Section, Key));
                oldTextBox.Text = Decode(ReferentialWriter.GetValue(Section, Key));
                newTextBox.Text = Decode(EditingDic[Section][Key]);
            }

            private static System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>> BuildEditingDictionary()
            {
                var dic = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>();
                foreach (var section in AppString.DefLangReader.Sections)
                {
                    var values = new System.Collections.Generic.Dictionary<string, string>();
                    foreach (var key in AppString.DefLangReader.GetSectionKeys(section))
                    {
                        values[key] = string.Empty;
                    }
                    dic.Add(section, values);
                }
                return dic;
            }

            private static string Encode(string text)
            {
                return text.Replace("\n", "\\n").Replace("\r", "\\r");
            }

            private static string Decode(string text)
            {
                return (text ?? string.Empty).Replace("\\r", "\r").Replace("\\n", "\n");
            }
        }
    }
}
