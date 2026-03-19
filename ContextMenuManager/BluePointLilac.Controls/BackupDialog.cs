using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility;
using WpfScrollViewer = System.Windows.Controls.ScrollViewer;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTextBlock = System.Windows.Controls.TextBlock;

namespace BluePointLilac.Controls
{
    public class BackupDialog : CommonDialog
    {
        public string Title { get; set; }
        public string CmbTitle { get; set; }
        public string[] CmbItems { get; set; }
        public int CmbSelectedIndex { get; set; }
        public string CmbSelectedText { get; set; }
        public string TvTitle { get; set; }
        public string[] TvItems { get; set; }
        public List<string> TvSelectedItems { get; set; }

        public override void Reset() { }

        protected override bool RunDialog(IntPtr hwndOwner)
        {
            var dialog = ContentDialogHost.CreateDialog(Title, hwndOwner);
            dialog.PrimaryButtonText = ResourceString.OK;
            dialog.CloseButtonText = ResourceString.Cancel;
            dialog.FullSizeDesired = true;

            var checkAll = new WpfCheckBox
            {
                Content = AppString.Dialog.SelectAll,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var comboBox = new WpfComboBox
            {
                MinWidth = 260,
                ItemsSource = CmbItems ?? Array.Empty<string>(),
                SelectedIndex = CmbSelectedIndex
            };
            if (!string.IsNullOrWhiteSpace(CmbSelectedText))
            {
                comboBox.Text = CmbSelectedText;
            }

            var nodeMap = CreateTreeNodes(TvItems ?? Array.Empty<string>());
            var treePanel = new WpfStackPanel();
            foreach (var panel in nodeMap.Select(x => x.Panel))
            {
                treePanel.Children.Add(panel);
            }

            checkAll.Checked += (_, _) => SetAll(nodeMap, true);
            checkAll.Unchecked += (_, _) => SetAll(nodeMap, false);

            foreach (var group in nodeMap)
            {
                group.Parent.Checked += (_, _) => SetGroup(group, true);
                group.Parent.Unchecked += (_, _) => SetGroup(group, false);
                foreach (var child in group.Children)
                {
                    child.Checked += (_, _) => RefreshState(nodeMap, checkAll);
                    child.Unchecked += (_, _) => RefreshState(nodeMap, checkAll);
                }
            }

            RefreshState(nodeMap, checkAll);

            dialog.Content = new WpfStackPanel
            {
                Children =
                {
                    new WpfTextBlock { Text = TvTitle, Margin = new Thickness(0, 0, 0, 8) },
                    checkAll,
                    new WpfScrollViewer
                    {
                        Content = treePanel,
                        VerticalScrollBarVisibility = WpfScrollBarVisibility.Auto,
                        MaxHeight = 340
                    },
                    new WpfStackPanel
                    {
                        Orientation = WpfOrientation.Horizontal,
                        Margin = new Thickness(0, 16, 0, 0),
                        Children =
                        {
                            new WpfTextBlock
                            {
                                Text = CmbTitle,
                                VerticalAlignment = VerticalAlignment.Center,
                                Margin = new Thickness(0, 0, 12, 0)
                            },
                            comboBox
                        }
                    }
                }
            };

            var result = ContentDialogHost.RunBlocking(owner => dialog.ShowAsync(owner), hwndOwner);
            if (result != ContentDialogResult.Primary)
            {
                return false;
            }

            CmbSelectedIndex = comboBox.SelectedIndex;
            CmbSelectedText = comboBox.SelectedItem as string ?? comboBox.Text;
            TvSelectedItems = GetSortedSelection(nodeMap);
            return true;
        }

        private static List<TreeGroup> CreateTreeNodes(string[] items)
        {
            var groups = new List<TreeGroup>
            {
                new(AppString.ToolBar.Home, BackupHelper.HomeBackupScenesText),
                new(AppString.ToolBar.Type, BackupHelper.TypeBackupScenesText),
                new(AppString.ToolBar.Rule, BackupHelper.RuleBackupScenesText)
            };

            foreach (var group in groups)
            {
                foreach (var item in items.Where(group.Match.Contains))
                {
                    group.AddChild(item);
                }
            }

            return groups.Where(g => g.Children.Count > 0).ToList();
        }

        private static void SetAll(IEnumerable<TreeGroup> groups, bool isChecked)
        {
            foreach (var group in groups)
            {
                SetGroup(group, isChecked);
            }
        }

        private static void SetGroup(TreeGroup group, bool isChecked)
        {
            foreach (var child in group.Children)
            {
                child.IsChecked = isChecked;
            }
            group.Parent.IsChecked = isChecked;
        }

        private static void RefreshState(IEnumerable<TreeGroup> groups, WpfCheckBox checkAll)
        {
            foreach (var group in groups)
            {
                var allChecked = group.Children.All(x => x.IsChecked == true);
                var anyChecked = group.Children.Any(x => x.IsChecked == true);
                group.Parent.IsChecked = allChecked;
                group.Parent.IsThreeState = true;
                if (!allChecked && anyChecked)
                {
                    group.Parent.IsChecked = null;
                }
            }

            var childNodes = groups.SelectMany(x => x.Children).ToArray();
            checkAll.IsChecked = childNodes.Length > 0 && childNodes.All(x => x.IsChecked == true);
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

        private sealed class TreeGroup
        {
            public TreeGroup(string title, IEnumerable<string> match)
            {
                Match = match.ToArray();
                Parent = new WpfCheckBox { Content = title, FontWeight = FontWeights.SemiBold };
                ChildrenHost = new WpfStackPanel { Margin = new Thickness(24, 6, 0, 12) };
                Panel = new WpfStackPanel();
                Panel.Children.Add(Parent);
                Panel.Children.Add(ChildrenHost);
            }

            public string[] Match { get; }
            public WpfCheckBox Parent { get; }
            public WpfStackPanel ChildrenHost { get; }
            public WpfStackPanel Panel { get; }
            public List<WpfCheckBox> Children { get; } = new();

            public void AddChild(string text)
            {
                var child = new WpfCheckBox
                {
                    Content = text,
                    Tag = text,
                    Margin = new Thickness(0, 2, 0, 2)
                };
                Children.Add(child);
                ChildrenHost.Children.Add(child);
            }
        }
    }
}
