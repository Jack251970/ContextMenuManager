using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfComboBoxItem = System.Windows.Controls.ComboBoxItem;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility;
using WpfScrollViewer = System.Windows.Controls.ScrollViewer;
using WpfSeparator = System.Windows.Controls.Separator;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTextBlock = System.Windows.Controls.TextBlock;

namespace ContextMenuManager.Controls
{
    /// <summary>Dialog that lets the user configure the logon-restore scheduled task:
    /// which backup to use, which scenes to restore, and the restore mode.</summary>
    public class LogonRestoreDialog
    {
        public string Title { get; set; }

        /// <summary>Backup entries to populate the backup selection combo-box.</summary>
        public IReadOnlyList<BackupEntry> BackupEntries { get; set; }

        /// <summary>Pre-selected backup file path (for editing an existing configuration).</summary>
        public string SelectedFilePath { get; set; }

        /// <summary>Pre-selected restore scenes (text strings).</summary>
        public IReadOnlyList<string> SelectedScenes { get; set; }

        /// <summary>Pre-selected restore mode index (0 / 1 / 2).</summary>
        public int SelectedModeIndex { get; set; }

        // Output properties (populated after ShowDialog returns true)
        public string ResultFilePath { get; private set; }
        public List<string> ResultScenes { get; private set; }
        public int ResultModeIndex { get; private set; }

        public bool ShowDialog() => RunDialog(null);

        public bool RunDialog(MainWindow owner)
        {
            var dialog = ContentDialogHost.CreateDialog(Title, owner);

            // ── Backup selection row ──────────────────────────────────────────
            var backupComboBox = new WpfComboBox { MinWidth = 340 };
            var preSelectedIndex = -1;
            if (BackupEntries != null)
            {
                for (var i = 0; i < BackupEntries.Count; i++)
                {
                    var entry = BackupEntries[i];
                    backupComboBox.Items.Add(new WpfComboBoxItem
                    {
                        Content = entry.DisplayText,
                        Tag = entry.FilePath
                    });
                    if (!string.IsNullOrEmpty(SelectedFilePath)
                        && string.Equals(entry.FilePath, SelectedFilePath, StringComparison.OrdinalIgnoreCase))
                        preSelectedIndex = i;
                }
            }
            backupComboBox.SelectedIndex = preSelectedIndex >= 0 ? preSelectedIndex : (backupComboBox.Items.Count > 0 ? 0 : -1);

            // ── Restore-scenes tree ───────────────────────────────────────────
            var sceneItems = GetRestoreSceneItems(backupComboBox);
            var nodeMap = CreateTreeNodes(sceneItems, SelectedScenes);

            var checkAll = new WpfCheckBox
            {
                Content = AppString.Dialog.SelectAll,
                Margin = new Thickness(0, 0, 0, 8),
                MinWidth = 0
            };

            var treePanel = new WpfStackPanel();
            foreach (var panel in nodeMap.Select(x => x.Panel))
                treePanel.Children.Add(panel);

            var isUpdating = false;

            checkAll.Checked += (_, _) =>
            {
                if (isUpdating) return;
                isUpdating = true;
                SetAll(nodeMap, true);
                RefreshState(nodeMap, checkAll);
                isUpdating = false;
            };
            checkAll.Unchecked += (_, _) =>
            {
                if (isUpdating) return;
                isUpdating = true;
                SetAll(nodeMap, false);
                RefreshState(nodeMap, checkAll);
                isUpdating = false;
            };

            foreach (var group in nodeMap)
            {
                group.Parent.Checked += (_, _) =>
                {
                    if (isUpdating) return;
                    isUpdating = true;
                    SetGroup(group, true);
                    RefreshState(nodeMap, checkAll);
                    isUpdating = false;
                };
                group.Parent.Unchecked += (_, _) =>
                {
                    if (isUpdating) return;
                    isUpdating = true;
                    SetGroup(group, false);
                    RefreshState(nodeMap, checkAll);
                    isUpdating = false;
                };
                foreach (var child in group.Children)
                {
                    child.Checked += (_, _) =>
                    {
                        if (isUpdating) return;
                        isUpdating = true;
                        RefreshState(nodeMap, checkAll);
                        isUpdating = false;
                    };
                    child.Unchecked += (_, _) =>
                    {
                        if (isUpdating) return;
                        isUpdating = true;
                        RefreshState(nodeMap, checkAll);
                        isUpdating = false;
                    };
                }
            }

            isUpdating = true;
            RefreshState(nodeMap, checkAll);
            isUpdating = false;

            var scenesScrollViewer = new WpfScrollViewer
            {
                Content = treePanel,
                VerticalScrollBarVisibility = WpfScrollBarVisibility.Auto,
                MaxHeight = 260
            };

            // Rebuild scene tree when the selected backup changes
            backupComboBox.SelectionChanged += (_, _) =>
            {
                var newItems = GetRestoreSceneItems(backupComboBox);
                var newMap = CreateTreeNodes(newItems, null);
                treePanel.Children.Clear();
                foreach (var panel in newMap.Select(x => x.Panel))
                    treePanel.Children.Add(panel);
                nodeMap.Clear();
                nodeMap.AddRange(newMap);
                isUpdating = true;
                RefreshState(nodeMap, checkAll);
                isUpdating = false;
            };

            // ── Restore mode row ──────────────────────────────────────────────
            var modeComboBox = new WpfComboBox
            {
                MinWidth = 260,
                ItemsSource = new[]
                {
                    AppString.Dialog.RestoreMode1,
                    AppString.Dialog.RestoreMode2,
                    AppString.Dialog.RestoreMode3
                },
                SelectedIndex = SelectedModeIndex >= 0 ? SelectedModeIndex : 0
            };

            // ── Assemble content ──────────────────────────────────────────────
            dialog.Content = new WpfStackPanel
            {
                Children =
                {
                    new WpfTextBlock
                    {
                        Text = AppString.Dialog.SelectLogonRestoreBackup ?? "Select backup file:",
                        Margin = new Thickness(0, 0, 0, 8)
                    },
                    backupComboBox,
                    new WpfSeparator { Margin = new Thickness(0, 12, 0, 12) },
                    new WpfTextBlock
                    {
                        Text = AppString.Dialog.RestoreContent ?? "Restore content:",
                        Margin = new Thickness(0, 0, 0, 8)
                    },
                    checkAll,
                    scenesScrollViewer,
                    new WpfStackPanel
                    {
                        Orientation = WpfOrientation.Horizontal,
                        Margin = new Thickness(0, 16, 0, 0),
                        Children =
                        {
                            new WpfTextBlock
                            {
                                Text = AppString.Dialog.RestoreMode ?? "Restore mode:",
                                VerticalAlignment = VerticalAlignment.Center,
                                Margin = new Thickness(0, 0, 12, 0)
                            },
                            modeComboBox
                        }
                    }
                }
            };

            var result = ContentDialogHost.RunBlocking(dialog.ShowAsync, owner);
            if (result != ContentDialogResult.Primary)
                return false;

            // Collect backup selection
            if (backupComboBox.SelectedItem is WpfComboBoxItem selected && selected.Tag is string path)
                ResultFilePath = path;
            else
                return false;

            ResultScenes = GetSortedSelection(nodeMap);
            ResultModeIndex = modeComboBox.SelectedIndex;
            return true;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string[] GetRestoreSceneItems(WpfComboBox backupComboBox)
        {
            if (backupComboBox.SelectedItem is WpfComboBoxItem selected
                && selected.Tag is string path
                && System.IO.File.Exists(path))
            {
                try
                {
                    BackupList.LoadBackupDataMetaData(path);
                    if (BackupList.metaData?.BackupScenes != null)
                        return BackupHelper.GetBackupRestoreScenesText(BackupList.metaData.BackupScenes);
                }
                catch { }
            }
            return BackupHelper.BackupScenesText;
        }

        private static List<TreeGroup> CreateTreeNodes(string[] items, IReadOnlyList<string> preSelected)
        {
            var preSelectedSet = preSelected?.ToHashSet() ?? [];

            var groups = new List<TreeGroup>
            {
                new(AppString.ToolBar.Home, BackupHelper.HomeBackupScenesText),
                new(AppString.ToolBar.Type, BackupHelper.TypeBackupScenesText),
                new(AppString.ToolBar.Rule, BackupHelper.RuleBackupScenesText)
            };

            foreach (var group in groups)
            {
                foreach (var item in items.Where(group.Match.Contains))
                    group.AddChild(item, preSelectedSet.Contains(item) || preSelectedSet.Count == 0);
            }

            return [.. groups.Where(g => g.Children.Count > 0)];
        }

        private static void SetAll(IEnumerable<TreeGroup> groups, bool isChecked)
        {
            foreach (var group in groups)
                SetGroup(group, isChecked);
        }

        private static void SetGroup(TreeGroup group, bool isChecked)
        {
            foreach (var child in group.Children)
                child.IsChecked = isChecked;
            group.Parent.IsChecked = isChecked;
        }

        private static void RefreshState(IEnumerable<TreeGroup> groups, WpfCheckBox checkAll)
        {
            foreach (var group in groups)
            {
                var allChecked = group.Children.All(x => x.IsChecked == true);
                var anyChecked = group.Children.Any(x => x.IsChecked == true);
                group.Parent.IsThreeState = false;
                if (allChecked) group.Parent.IsChecked = true;
                else if (anyChecked) group.Parent.IsChecked = null;
                else group.Parent.IsChecked = false;
            }

            var childNodes = groups.SelectMany(x => x.Children).ToArray();
            checkAll.IsThreeState = false;
            if (childNodes.Length > 0 && childNodes.All(x => x.IsChecked == true)) checkAll.IsChecked = true;
            else if (childNodes.Any(x => x.IsChecked == true)) checkAll.IsChecked = null;
            else checkAll.IsChecked = false;
        }

        private static List<string> GetSortedSelection(IEnumerable<TreeGroup> groups)
        {
            var selected = groups
                .SelectMany(x => x.Children)
                .Where(x => x.IsChecked == true)
                .Select(x => x.Tag as string)
                .Where(x => x != null)
                .ToHashSet();

            return BackupHelper.HomeBackupScenesText
                .Concat(BackupHelper.TypeBackupScenesText)
                .Concat(BackupHelper.RuleBackupScenesText)
                .Where(selected.Contains)
                .ToList();
        }

        // ── Inner types ───────────────────────────────────────────────────────

        public sealed class BackupEntry
        {
            public string FilePath { get; init; }
            public string DisplayText { get; init; }
        }

        private sealed class TreeGroup
        {
            public TreeGroup(string title, IEnumerable<string> match)
            {
                Match = [.. match];
                Parent = new WpfCheckBox { Content = title, MinWidth = 0 };
                ChildrenHost = new WpfStackPanel { Margin = new Thickness(24, 6, 0, 12) };
                Panel = new WpfStackPanel();
                Panel.Children.Add(Parent);
                Panel.Children.Add(ChildrenHost);
            }

            public string[] Match { get; }
            public WpfCheckBox Parent { get; }
            public WpfStackPanel ChildrenHost { get; }
            public WpfStackPanel Panel { get; }
            public List<WpfCheckBox> Children { get; } = [];

            public void AddChild(string text, bool isChecked = true)
            {
                var child = new WpfCheckBox
                {
                    Content = text,
                    Tag = text,
                    IsChecked = isChecked,
                    Margin = new Thickness(0, 2, 0, 2),
                    MinWidth = 0
                };
                Children.Add(child);
                ChildrenHost.Children.Add(child);
            }
        }
    }
}
