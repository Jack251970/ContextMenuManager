using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System;
using System.Drawing;
using System.Windows.Forms;

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
            DropDownItems.AddRange(new ToolStripItem[] { TsiAddGuidDic,
                new RToolStripSeparator(), TsiCopyGuid, TsiBlockGuid, TsiClsidLocation });
            TsiCopyGuid.Click += (sender, e) => CopyGuid();
            TsiBlockGuid.Click += (sender, e) => BlockGuid();
            TsiAddGuidDic.Click += (sender, e) => AddGuidDic();
            TsiClsidLocation.Click += (sender, e) => OpenClsidPath();
            ((MyListItem)item).ContextMenuStrip.Opening += (sender, e) => RefreshMenuItem();
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
            AppMessageBox.Show($"{AppString.Message.CopiedToClipboard}\n{guid}",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                        if (AppMessageBox.Show(AppString.Message.PromptIsOpenItem,
                            MessageBoxButtons.YesNo) != DialogResult.Yes) return;
                    }
                    Microsoft.Win32.Registry.SetValue(path, Item.Guid.ToString("B"), string.Empty);
                }
            }
            ExplorerRestarter.Show();
        }

        private void AddGuidDic()
        {
            using var dlg = new AddGuidDicDialog();
            dlg.ItemText = GuidInfo.GetText(Item.Guid);
            dlg.ItemIcon = GuidInfo.GetImage(Item.Guid);
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
            if (dlg.ShowDialog() != DialogResult.OK)
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
            if (dlg.ItemText.IsNullOrWhiteSpace())
            {
                AppMessageBox.Show(AppString.Message.TextCannotBeEmpty);
                return;
            }
            dlg.ItemText = ResourceString.GetDirectString(dlg.ItemText);
            if (dlg.ItemText.IsNullOrWhiteSpace())
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

        private sealed class AddGuidDicDialog : CommonDialog
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

            public override void Reset() { }

            protected override bool RunDialog(IntPtr hwndOwner)
            {
                using var frm = new AddGuidDicForm();
                frm.ItemText = ItemText;
                frm.ItemIcon = ItemIcon;
                frm.ItemIconPath = ItemIconPath;
                frm.ItemIconIndex = ItemIconIndex;
                frm.TopMost = true;
                var flag = frm.ShowDialog() == DialogResult.OK;
                if (flag)
                {
                    ItemText = frm.ItemText;
                    ItemIcon = frm.ItemIcon;
                    ItemIconPath = frm.ItemIconPath;
                    ItemIconIndex = frm.ItemIconIndex;
                }
                IsDelete = frm.IsDelete;
                return flag;
            }

            private sealed class AddGuidDicForm : RForm
            {
                public AddGuidDicForm()
                {
                    AcceptButton = btnOK;
                    CancelButton = btnCancel;
                    Font = SystemFonts.MenuFont;
                    Text = AppString.Dialog.AddGuidDic;
                    ShowIcon = ShowInTaskbar = false;
                    MaximizeBox = MinimizeBox = false;
                    FormBorderStyle = FormBorderStyle.FixedSingle;
                    StartPosition = FormStartPosition.CenterParent;
                    InitializeComponents();
                    InitTheme();
                }

                public string ItemText
                {
                    get => txtName.Text;
                    set => txtName.Text = value;
                }
                public Image ItemIcon
                {
                    get => picIcon.Image;
                    set => picIcon.Image = value;
                }
                public string ItemIconPath { get; set; }
                public int ItemIconIndex { get; set; }
                public bool IsDelete { get; private set; }

                private readonly TextBox txtName = new();
                private readonly Label lblName = new()
                {
                    Text = AppString.Dialog.ItemText,
                    AutoSize = true
                };
                private readonly Label lblIcon = new()
                {
                    Text = AppString.Menu.ItemIcon,
                    AutoSize = true
                };
                private readonly PictureBox picIcon = new()
                {
                    Size = SystemInformation.IconSize
                };
                private readonly Button btnBrowse = new()
                {
                    Text = AppString.Dialog.Browse,
                    AutoSize = true
                };
                private readonly Button btnOK = new()
                {
                    Text = ResourceString.OK,
                    DialogResult = DialogResult.OK,
                    AutoSize = true
                };
                private readonly Button btnCancel = new()
                {
                    Text = ResourceString.Cancel,
                    DialogResult = DialogResult.Cancel,
                    AutoSize = true
                };
                private readonly Button btnDelete = new()
                {
                    Text = AppString.Dialog.DeleteGuidDic,
                    DialogResult = DialogResult.Cancel,
                    AutoSize = true
                };

                private void InitializeComponents()
                {
                    Controls.AddRange(new Control[] { lblName, txtName, lblIcon, picIcon, btnBrowse, btnDelete, btnOK, btnCancel });
                    var a = 20.DpiZoom();
                    lblName.Left = lblName.Top = lblIcon.Left = btnDelete.Left = txtName.Top = a;
                    txtName.Left = lblName.Right + a;
                    btnOK.Left = btnDelete.Right + a;
                    btnCancel.Left = btnOK.Right + a;
                    txtName.Width = btnCancel.Right - txtName.Left;
                    btnBrowse.Left = btnCancel.Right - btnBrowse.Width;
                    picIcon.Left = btnOK.Left + (btnOK.Width - picIcon.Width) / 2;
                    btnBrowse.Top = txtName.Bottom + a;
                    picIcon.Top = btnBrowse.Top + (btnBrowse.Height - picIcon.Height) / 2;
                    lblIcon.Top = btnBrowse.Top + (btnBrowse.Height - lblIcon.Height) / 2;
                    btnDelete.Top = btnOK.Top = btnCancel.Top = btnBrowse.Bottom + a;
                    ClientSize = new Size(btnCancel.Right + a, btnCancel.Bottom + a);
                    ToolTipBox.SetToolTip(btnDelete, AppString.Tip.DeleteGuidDic);
                    btnBrowse.Click += (sender, e) => SelectIcon();
                    btnDelete.Click += (sender, e) => IsDelete = true;
                }

                private void SelectIcon()
                {
                    using var dlg = new IconDialog();
                    dlg.IconPath = ItemIconPath;
                    dlg.IconIndex = ItemIconIndex;
                    if (dlg.ShowDialog() != DialogResult.OK) return;
                    using var icon = ResourceIcon.GetIcon(dlg.IconPath, dlg.IconIndex);
                    Image image = icon?.ToBitmap();
                    if (image == null) return;
                    picIcon.Image = image;
                    ItemIconPath = dlg.IconPath;
                    ItemIconIndex = dlg.IconIndex;
                }
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
            listItem.ParentChanged += (sender, e) =>
            {
                if (listItem.IsDisposed) return;
                if (listItem.Parent == null) return;
                Visible = XmlDicHelper.DetailedEditGuidDic.ContainsKey(item.Guid);
            };
            MouseDown += (sender, e) =>
            {
                using var dlg = new DetailedEditDialog();
                dlg.GroupGuid = item.Guid;
                dlg.ShowDialog();
            };
        }
    }
}