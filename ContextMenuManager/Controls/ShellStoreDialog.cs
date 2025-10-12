using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    sealed class ShellStoreDialog : CommonDialog
    {
        public string[] SelectedKeyNames { get; private set; }
        public Func<string, bool> Filter { get; set; }
        public string ShellPath { get; set; }
        public bool IsReference { get; set; }

        public override void Reset() { }

        protected override bool RunDialog(IntPtr hwndOwner)
        {
            using (ShellStoreForm frm = new ShellStoreForm(ShellPath, Filter, IsReference))
            {
                frm.TopMost = true;
                bool flag = frm.ShowDialog() == DialogResult.OK;
                if (flag) SelectedKeyNames = frm.SelectedKeyNames;
                return flag;
            }
        }

        public sealed class ShellStoreForm : RForm
        {
            public string ShellPath { get; private set; }
            public Func<string, bool> Filter { get; private set; }
            public string[] SelectedKeyNames { get; private set; }

            public ShellStoreForm(string shellPath, Func<string, bool> filter, bool isReference)
            {
                SuspendLayout();
                Filter = filter;
                ShellPath = shellPath;
                AcceptButton = btnOK;
                CancelButton = btnCancel;
                Font = SystemFonts.MessageBoxFont;
                SizeGripStyle = SizeGripStyle.Hide;
                ShowIcon = ShowInTaskbar = false;
                MinimizeBox = MaximizeBox = false;
                StartPosition = FormStartPosition.CenterParent;
                MinimumSize = Size = new Size(652, 422).DpiZoom();
                Text = isReference ? AppString.Dialog.CheckReference : AppString.Dialog.CheckCopy;
                btnOK.Click += (sender, e) => GetSelectedItems();
                chkSelectAll.Click += (sender, e) =>
                {
                    bool flag = chkSelectAll.Checked;
                    foreach (StoreShellItem item in list.Controls)
                    {
                        item.IsSelected = flag;
                    }
                };
                list.Owner = listBox;
                InitializeComponents();
                LoadItems(isReference);
                ResumeLayout();
                InitTheme();
            }

            readonly MyList list = new MyList();
            readonly MyListBox listBox = new MyListBox();
            readonly Panel pnlBorder = new Panel
            {
                BackColor = MyMainForm.FormFore
            };
            readonly Button btnOK = new Button
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                DialogResult = DialogResult.OK,
                Text = ResourceString.OK,
                AutoSize = true
            };
            readonly Button btnCancel = new Button
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                DialogResult = DialogResult.Cancel,
                Text = ResourceString.Cancel,
                AutoSize = true
            };
            readonly CheckBox chkSelectAll = new CheckBox
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Text = AppString.Dialog.SelectAll,
                Cursor = Cursors.Hand,
                AutoSize = true
            };

            private void InitializeComponents()
            {
                Controls.AddRange(new Control[] { listBox, pnlBorder, btnOK, btnCancel, chkSelectAll });
                int a = 20.DpiZoom();
                listBox.Location = new Point(a, a);
                pnlBorder.Location = new Point(a - 1, a - 1);
                chkSelectAll.Top = btnOK.Top = btnCancel.Top = ClientSize.Height - btnCancel.Height - a;
                btnCancel.Left = ClientSize.Width - btnCancel.Width - a;
                btnOK.Left = btnCancel.Left - btnOK.Width - a;
                chkSelectAll.Left = a;
                OnResize(null);
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                listBox.Width = ClientSize.Width - 2 * listBox.Left;
                listBox.Height = btnOK.Top - 2 * listBox.Top;
                pnlBorder.Width = listBox.Width + 2;
                pnlBorder.Height = listBox.Height + 2;
            }

            private void LoadItems(bool isReference)
            {
                using (var shellKey = RegistryEx.GetRegistryKey(ShellPath))
                {
                    foreach (string itemName in shellKey.GetSubKeyNames())
                    {
                        if (Filter != null && !Filter(itemName)) continue;
                        string regPath = $@"{ShellPath}\{itemName}";
                        StoreShellItem item = new StoreShellItem(regPath, isReference);
                        item.SelectedChanged += () =>
                        {
                            foreach (StoreShellItem shellItem in list.Controls)
                            {
                                if (!shellItem.IsSelected)
                                {
                                    chkSelectAll.Checked = false;
                                    return;
                                }
                            }
                            chkSelectAll.Checked = true;
                        };
                        list.AddItem(item);
                    }
                }
            }

            private void GetSelectedItems()
            {
                List<string> names = new List<string>();
                foreach (StoreShellItem item in list.Controls)
                    if (item.IsSelected) names.Add(item.KeyName);
                SelectedKeyNames = names.ToArray();
            }
        }
    }

    sealed class StoreShellItem : ShellItem
    {
        public StoreShellItem(string regPath, bool isPublic, bool isSelect = true) : base(regPath)
        {
            IsPublic = isPublic;
            if (isSelect)
            {
                ContextMenuStrip = null;
                AddCtr(chkSelected);
                ChkVisible.Visible = BtnShowMenu.Visible = BtnSubItems.Visible = false;
                MouseClick += (sender, e) => chkSelected.Checked = !chkSelected.Checked;
                chkSelected.CheckedChanged += (sender, e) => SelectedChanged?.Invoke();
            }
            RegTrustedInstaller.TakeRegTreeOwnerShip(regPath);
        }

        public bool IsPublic { get; set; }
        public bool IsSelected
        {
            get => chkSelected.Checked;
            set => chkSelected.Checked = value;
        }

        readonly CheckBox chkSelected = new CheckBox
        {
            Cursor = Cursors.Hand,
            AutoSize = true
        };

        public Action SelectedChanged;

        public override void DeleteMe()
        {
            if (IsPublic && AppMessageBox.Show(AppString.Message.ConfirmDeleteReferenced,
                MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            base.DeleteMe();
        }
    }
}