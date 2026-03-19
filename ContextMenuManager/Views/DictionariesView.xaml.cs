using ContextMenuManager.Methods;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ContextMenuManager.Views
{
    public partial class DictionariesView : UserControl
    {
        private readonly Dictionary<int, DictionaryEntry> entries;

        public DictionariesView()
        {
            InitializeComponent();

            entries = new Dictionary<int, DictionaryEntry>
            {
                [0] = new DictionaryEntry(() => AppString.Other.Dictionaries, DescriptionTextBox, false, null),
                [1] = new DictionaryEntry(() => Properties.Resources.AppLanguageDic, AppLanguageTextBox, true, AppConfig.ZH_CNINI),
                [2] = new DictionaryEntry(() => Properties.Resources.GuidInfosDic, GuidInfoTextBox, true, AppConfig.GUIDINFOSDICINI),
                [3] = new DictionaryEntry(() => Properties.Resources.DetailedEditDic, DetailedEditTextBox, true, AppConfig.DETAILEDEDITDICXML),
                [4] = new DictionaryEntry(() => Properties.Resources.EnhanceMenusDic, EnhanceMenusTextBox, true, AppConfig.ENHANCEMENUSICXML),
                [5] = new DictionaryEntry(() => Properties.Resources.UwpModeItemsDic, UwpModeTextBox, true, AppConfig.UWPMODEITEMSDICXML)
            };

            LoadLabels();
            LoadText();
        }

        public void LoadText()
        {
            LoadLabels();
            LoadCurrentTabContent();
        }

        private void LoadLabels()
        {
            PageTitleText.Text = AppString.SideBar.Dictionaries ?? "Dictionaries";
            PageDescriptionText.Text = "Browse the built-in dictionary and template files used by the application.";
            OpenDirButton.Content = AppString.Menu.FileLocation ?? "Open";
            EditButton.Content = AppString.Menu.Edit ?? "Edit";
            SaveButton.Content = AppString.Menu.Save ?? "Save";

            DescriptionTab.Header = AppString.Other.DictionaryDescription;
            AppLanguageTab.Header = AppString.SideBar.AppLanguage;
            GuidInfoTab.Header = AppString.Other.GuidInfosDictionary;
            DetailedEditTab.Header = AppString.SideBar.DetailedEdit;
            EnhanceMenusTab.Header = AppString.SideBar.EnhanceMenu;
            UwpModeTab.Header = AppString.Other.UwpMode;

            UpdateHeader();
        }

        private void DictionaryTabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || e.Source != DictionaryTabControl)
            {
                return;
            }

            LoadCurrentTabContent();
            UpdateHeader();
        }

        private void LoadCurrentTabContent()
        {
            var entry = GetCurrentEntry();
            if (!string.IsNullOrEmpty(entry.TextBox.Text))
            {
                return;
            }

            entry.TextBox.Text = entry.GetText();
            entry.TextBox.ScrollToHome();
        }

        private void UpdateHeader()
        {
            var index = DictionaryTabControl.SelectedIndex;
            var tabHeader = (DictionaryTabControl.SelectedItem as TabItem)?.Header?.ToString() ?? string.Empty;
            CurrentTabLabel.Text = tabHeader;
            CurrentTabHintText.Text = index == 0 ? AppConfig.DicsDir : AppConfig.UserDicsDir;
            OpenDirButton.Visibility = index == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OpenDirButton_OnClick(object sender, RoutedEventArgs e)
        {
            ExternalProgram.OpenDirectory(AppConfig.DicsDir);
        }

        private void EditButton_OnClick(object sender, RoutedEventArgs e)
        {
            ExternalProgram.OpenNotepadWithText(GetCurrentEntry().GetText());
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            var index = DictionaryTabControl.SelectedIndex;
            var entry = GetCurrentEntry();

            if (!entry.CanSave)
            {
                return;
            }

            var dialog = new SaveFileDialog();
            var dirPath = index == 1 ? AppConfig.LangsDir : AppConfig.UserDicsDir;
            Directory.CreateDirectory(dirPath);

            dialog.FileName = entry.DefaultFileName;
            dialog.Filter = $"{entry.DefaultFileName}|*{Path.GetExtension(entry.DefaultFileName)}";
            dialog.InitialDirectory = dirPath;

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            File.WriteAllText(dialog.FileName, entry.GetText(), Encoding.Unicode);
        }

        private DictionaryEntry GetCurrentEntry()
        {
            return entries.TryGetValue(DictionaryTabControl.SelectedIndex, out var entry) ? entry : entries[0];
        }

        private sealed class DictionaryEntry
        {
            public DictionaryEntry(Func<string> getText, TextBox textBox, bool canSave, string defaultFileName)
            {
                GetText = getText;
                TextBox = textBox;
                CanSave = canSave;
                DefaultFileName = defaultFileName;
            }

            public Func<string> GetText { get; }
            public TextBox TextBox { get; }
            public bool CanSave { get; }
            public string DefaultFileName { get; }
        }
    }
}
