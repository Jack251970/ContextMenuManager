using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Controls.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;
using Image = System.Drawing.Image;
using WpfButton = System.Windows.Controls.Button;
using WpfImage = System.Windows.Controls.Image;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace ContextMenuManager.Controls.Interfaces
{
    internal interface ITsiGuidItem
    {
        Guid Guid { get; }
        string ItemText { get; }
        HandleGuidMenuItem TsiHandleGuid { get; set; }
        DetailedEditButton BtnDetailedEdit { get; set; }
    }

    internal sealed class HandleGuidMenuItem : RToolStripMenuItem
    {
        public HandleGuidMenuItem(ITsiGuidItem item) : base(AppString.Menu.HandleGuid)
        {
            Item = item;
            foreach (var dropDownItem in new Control[] { TsiAddGuidDic,
                new RToolStripSeparator(), TsiCopyGuid, TsiBlockGuid, TsiClsidLocation })
            {
                Items.Add(dropDownItem);
            }
            TsiCopyGuid.Click += (sender, e) => CopyGuid();
            TsiBlockGuid.Click += (sender, e) => BlockGuid();
            TsiAddGuidDic.Click += (sender, e) => AddGuidDic();
            TsiClsidLocation.Click += (sender, e) => OpenClsidPath();
            ((IBtnShowMenuItem)item).Flyout.Opened += (sender, e) => RefreshMenuItem();
        }

        private readonly RToolStripMenuItem TsiCopyGuid = new(AppString.Menu.CopyGuid);
        private readonly RToolStripMenuItem TsiBlockGuid = new(AppString.Menu.BlockGuid);
        private readonly RToolStripMenuItem TsiAddGuidDic = new(AppString.Menu.AddGuidDic);
        private readonly RToolStripMenuItem TsiClsidLocation = new(AppString.Menu.ClsidLocation);

        public ITsiGuidItem Item { get; set; }

        private void CopyGuid()
        {
            var guid = Item.Guid.ToString("B");
            Clipboard.SetText(guid);
            AppMessageBox.Show($"{AppString.Message.CopiedToClipboard}\n{guid}", null,
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BlockGuid()
        {
            foreach (var path in GuidBlockedList.BlockedPaths)
            {
                if (TsiBlockGuid.Checked)
                {
                    RegistryEx.DeleteValue(path, Item.Guid.ToString("B"));
                }
                else
                {
                    if (Item.Guid.Equals(ShellExItem.LnkOpenGuid) && AppConfig.ProtectOpenItem)
                    {
                        if (AppMessageBox.Show(AppString.Message.PromptIsOpenItem, null,
                            MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
                    }
                    Microsoft.Win32.Registry.SetValue(path, Item.Guid.ToString("B"), string.Empty);
                }
            }
            ExplorerRestarter.Show();
        }

        private void AddGuidDic()
        {
            var dlg = new AddGuidDicDialog
            {
                ItemText = GuidInfo.GetText(Item.Guid),
                ItemIcon = GuidInfo.GetImage(Item.Guid)
            };
            var location = GuidInfo.GetIconLocation(Item.Guid);
            dlg.ItemIconPath = location.IconPath;
            dlg.ItemIconIndex = location.IconIndex;
            var writer = new IniWriter
            {
                FilePath = AppConfig.UserGuidInfosDic,
                DeleteFileWhenEmpty = true
            };
            var section = Item.Guid.ToString();
            var listItem = (MyListItem)Item;
            if (!dlg.ShowDialog())
            {
                if (dlg.IsDelete)
                {
                    writer.DeleteSection(section);
                    GuidInfo.RemoveDic(Item.Guid);
                    listItem.Text = Item.ItemText;
                    listItem.Image = GuidInfo.GetImage(Item.Guid);
                }
                return;
            }
            if (string.IsNullOrWhiteSpace(dlg.ItemText))
            {
                AppMessageBox.Show(AppString.Message.TextCannotBeEmpty);
                return;
            }
            dlg.ItemText = ResourceString.GetDirectString(dlg.ItemText);
            if (string.IsNullOrWhiteSpace(dlg.ItemText))
            {
                AppMessageBox.Show(AppString.Message.StringParsingFailed);
                return;
            }
            else
            {
                GuidInfo.RemoveDic(Item.Guid);
                writer.SetValue(section, "Text", dlg.ItemText);
                writer.SetValue(section, "Icon", dlg.ItemIconLocation);
                listItem.Text = dlg.ItemText;
                listItem.Image = dlg.ItemIcon;
            }
        }

        private void OpenClsidPath()
        {
            var clsidPath = GuidInfo.GetClsidPath(Item.Guid);
            ExternalProgram.JumpRegEdit(clsidPath, null, AppConfig.OpenMoreRegedit);
        }

        private void RefreshMenuItem()
        {
            TsiClsidLocation.Visible = GuidInfo.GetClsidPath(Item.Guid) != null;
            TsiBlockGuid.Visible = TsiBlockGuid.Checked = false;
            if (Item is ShellExItem)
            {
                TsiBlockGuid.Visible = true;
                foreach (var path in GuidBlockedList.BlockedPaths)
                {
                    if (Microsoft.Win32.Registry.GetValue(path, Item.Guid.ToString("B"), null) != null)
                    {
                        TsiBlockGuid.Checked = true; break;
                    }
                }
            }
        }

        private sealed class AddGuidDicDialog
        {
            public Image ItemIcon { get; set; }
            public string ItemText { get; set; }
            public bool IsDelete { get; private set; }
            public string ItemIconPath { get; set; }
            public int ItemIconIndex { get; set; }
            public string ItemIconLocation
            {
                get
                {
                    if (ItemIconPath == null) return null;
                    return $"{ItemIconPath},{ItemIconIndex}";
                }
            }

            public bool ShowDialog()
            {
                return RunDialog(null);
            }

            public bool RunDialog(MainWindow owner)
            {
                var dialog = ContentDialogHost.CreateDialog(AppString.Dialog.AddGuidDic, owner);
                dialog.SecondaryButtonText = AppString.Dialog.DeleteGuidDic;

                var stackPanel = new WpfStackPanel { MinWidth = 350 };

                var txtName = new WpfTextBox
                {
                    Text = ItemText ?? string.Empty,
                    Margin = new Thickness(0, 0, 0, 16)
                };
                ControlHelper.SetHeader(txtName, AppString.Dialog.ItemText);
                stackPanel.Children.Add(txtName);

                var iconPanel = new WpfStackPanel { Orientation = Orientation.Horizontal };

                var imgIcon = new WpfImage
                {
                    Width = 32,
                    Height = 32,
                    Source = ItemIcon?.ToBitmapSource(),
                    Margin = new Thickness(0, 0, 16, 0)
                };

                var btnBrowse = new WpfButton
                {
                    Content = AppString.Dialog.Browse,
                    VerticalAlignment = VerticalAlignment.Center
                };
                btnBrowse.Click += (s, e) =>
                {
                    var iconDlg = new IconDialog
                    {
                        IconPath = ItemIconPath,
                        IconIndex = ItemIconIndex
                    };
                    if (iconDlg.ShowDialog())
                    {
                        using var icon = ResourceIcon.GetIcon(iconDlg.IconPath, iconDlg.IconIndex);
                        ItemIcon = icon?.ToBitmap();
                        if (ItemIcon != null)
                        {
                            imgIcon.Source = ItemIcon.ToBitmapSource();
                            ItemIconPath = iconDlg.IconPath;
                            ItemIconIndex = iconDlg.IconIndex;
                        }
                    }
                };

                var iconLabel = new Label { Content = AppString.Menu.ItemIcon, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };

                iconPanel.Children.Add(iconLabel);
                iconPanel.Children.Add(imgIcon);
                iconPanel.Children.Add(btnBrowse);

                stackPanel.Children.Add(new Label { Content = AppString.Menu.ItemIcon, FontWeight = FontWeights.Bold });
                stackPanel.Children.Add(iconPanel);

                dialog.Content = stackPanel;

                var result = ContentDialogHost.RunBlocking(dialog.ShowAsync, owner);

                if (result == ContentDialogResult.Primary)
                {
                    ItemText = txtName.Text;
                    return true;
                }
                else if (result == ContentDialogResult.Secondary)
                {
                    IsDelete = true;
                    return false;
                }

                return false;
            }
        }
    }

    internal sealed class DetailedEditButton : PictureButton
    {
        public DetailedEditButton(ITsiGuidItem item) : base(AppImage.SubItems)
        {
            var listItem = (MyListItem)item;
            listItem.AddCtr(this);
            ToolTipBox.SetToolTip(this, AppString.SideBar.DetailedEdit);
            listItem.Control.Loaded += (sender, e) =>
            {
                Visibility = XmlDicHelper.DetailedEditGuidDic.ContainsKey(item.Guid) ? Visibility.Visible : Visibility.Collapsed;
            };
            Click += (sender, e) =>
            {
                var dlg = new DetailedEditDialog
                {
                    GroupGuid = item.Guid
                };
                dlg.ShowDialog();
            };
        }
    }
}
