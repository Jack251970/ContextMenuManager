using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace ContextMenuManager.Controls
{
    internal sealed partial class ShellStoreDialog
    {
        public string[] SelectedKeyNames { get; private set; }
        public Func<string, bool> Filter { get; set; }
        public string ShellPath { get; set; }
        public bool IsReference { get; set; }

        public bool ShowDialog() => RunDialog(null);

        public bool RunDialog(MainWindow owner)
        {
            var form = new ShellStoreForm(ShellPath, Filter, IsReference);
            
            var dialog = ContentDialogHost.CreateDialog(
                IsReference ? AppString.Dialog.CheckReference : AppString.Dialog.CheckCopy, 
                owner);
            dialog.PrimaryButtonText = ResourceString.OK;
            dialog.CloseButtonText = ResourceString.Cancel;
            dialog.FullSizeDesired = true;

            var host = new WindowsFormsHost
            {
                Child = form,
                Height = 400,
                Width = 600
            };

            dialog.Content = host;
            var result = ContentDialogHost.RunBlocking(dialog.ShowAsync, owner);
            
            if (result != ContentDialogResult.Primary)
            {
                return false;
            }

            SelectedKeyNames = form.SelectedKeyNames;
            return true;
        }

        public sealed class ShellStoreForm : Panel
        {
            public string ShellPath { get; private set; }
            public Func<string, bool> Filter { get; private set; }
            public string[] SelectedKeyNames { get; private set; }

            public ShellStoreForm(string shellPath, Func<string, bool> filter, bool isReference)
            {
                SuspendLayout();
                Filter = filter;
                ShellPath = shellPath;
                MinimumSize = Size = new System.Drawing.Size(652, 422).DpiZoom();
                btnOK.Click += (sender, e) => GetSelectedItems();
                chkSelectAll.Click += (sender, e) =>
                {
                    var flag = chkSelectAll.Checked;
                    foreach (StoreShellItem item in list.Controls)
                    {
                        item.IsSelected = flag;
                    }
                };
                list.Owner = listBox;
                InitializeComponents();
                LoadItems(isReference);
                ResumeLayout();
            }

            private readonly MyList list = new();
            private readonly MyListBox listBox = new();
            private readonly Panel pnlBorder = new()
            {
                BackColor = DarkModeHelper.FormFore
            };
            private readonly Button btnOK = new()
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Text = ResourceString.OK,
                AutoSize = true
            };
            private readonly Button btnCancel = new()
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Text = ResourceString.Cancel,
                AutoSize = true
            };
            private readonly CheckBox chkSelectAll = new()
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Text = AppString.Dialog.SelectAll,
                Cursor = Cursors.Hand,
                AutoSize = true
            };

            private void InitializeComponents()
            {
                Controls.AddRange([listBox, pnlBorder, chkSelectAll]);
                var a = 20.DpiZoom();
                listBox.Location = new System.Drawing.Point(a, a);
                pnlBorder.Location = new System.Drawing.Point(a - 1, a - 1);
                chkSelectAll.Top = Height - chkSelectAll.Height - a;
                chkSelectAll.Left = a;
                Resize += OnResize;
                OnResize(null, null);
            }

            protected void OnResize(object sender, EventArgs e)
            {
                listBox.Width = Width - 2 * listBox.Left;
                listBox.Height = Height - 3 * listBox.Top - chkSelectAll.Height;
                pnlBorder.Width = listBox.Width + 2;
                pnlBorder.Height = listBox.Height + 2;
            }

            private void LoadItems(bool isReference)
            {
                using var shellKey = RegistryEx.GetRegistryKey(ShellPath);
                foreach (var itemName in shellKey.GetSubKeyNames())
                {
                    if (Filter != null && !Filter(itemName)) continue;
                    var regPath = $@"{ShellPath}\{itemName}";
                    var item = new StoreShellItem(regPath, isReference);
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

            private void GetSelectedItems()
            {
                var names = new List<string>();
                foreach (StoreShellItem item in list.Controls)
                    if (item.IsSelected) names.Add(item.KeyName);
                SelectedKeyNames = names.ToArray();
            }
        }
    }

    internal sealed class StoreShellItem : ShellItem
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

        private readonly CheckBox chkSelected = new()
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