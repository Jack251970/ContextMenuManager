using BluePointLilac.Methods;
using ContextMenuManager.BluePointLilac.Controls;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public class SelectDialog : CommonDialog
    {
        public string Title { get; set; }
        public string Selected { get; set; }
        public int SelectedIndex { get; set; }
        public string[] Items { get; set; }
        public bool CanEdit { get; set; }

        public override void Reset() { }

        protected override bool RunDialog(IntPtr hwndOwner)
        {
            using var frm = new SelectForm();
            frm.Text = Title;
            frm.Items = Items;
            if (Selected != null) frm.Selected = Selected;
            else frm.SelectedIndex = SelectedIndex;
            frm.CanEdit = CanEdit;
            if (Control.FromHandle(hwndOwner) is Form owner) frm.TopMost = true;
            var flag = frm.ShowDialog() == DialogResult.OK;
            if (flag)
            {
                Selected = frm.Selected;
                SelectedIndex = frm.SelectedIndex;
            }
            return flag;
        }

        private sealed class SelectForm : RForm
        {
            public SelectForm()
            {
                SuspendLayout();
                AcceptButton = btnOK;
                CancelButton = btnCancel;
                Font = SystemFonts.MenuFont;
                ShowIcon = ShowInTaskbar = false;
                MaximizeBox = MinimizeBox = false;
                FormBorderStyle = FormBorderStyle.FixedSingle;
                StartPosition = FormStartPosition.CenterParent;
                InitializeComponents();
                ResumeLayout();
                InitTheme();
            }

            public string Selected
            {
                get => cmbItems.Text;
                set => cmbItems.Text = value;
            }

            public string[] Items
            {
                get
                {
                    var value = new string[cmbItems.Items.Count];
                    cmbItems.Items.CopyTo(value, 0);
                    return value;
                }
                set
                {
                    cmbItems.Items.Clear();
                    cmbItems.Items.AddRange(value);
                }
            }

            public bool CanEdit
            {
                get => cmbItems.DropDownStyle == ComboBoxStyle.DropDown;
                set
                {
                    cmbItems.DropDownStyle = value ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList;
                    // 根据 DropDownStyle 设置合适的 AutoCompleteMode
                    if (value)
                    {
                        cmbItems.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                        cmbItems.AutoCompleteSource = AutoCompleteSource.ListItems;
                    }
                    else
                    {
                        // 当 DropDownStyle 是 DropDownList 时，AutoCompleteMode 必须为 None
                        cmbItems.AutoCompleteMode = AutoCompleteMode.None;
                        cmbItems.AutoCompleteSource = AutoCompleteSource.None;
                    }
                }
            }

            public int SelectedIndex
            {
                get => cmbItems.SelectedIndex;
                set => cmbItems.SelectedIndex = value;
            }

            private readonly Button btnOK = new()
            {
                DialogResult = DialogResult.OK,
                Text = ResourceString.OK,
                AutoSize = true
            };
            private readonly Button btnCancel = new()
            {
                DialogResult = DialogResult.Cancel,
                Text = ResourceString.Cancel,
                AutoSize = true
            };
            private readonly RComboBox cmbItems = new()
            {
                // 移除初始化时的 AutoCompleteMode 和 AutoCompleteSource 设置
                // 这些设置将在 CanEdit 属性中根据模式动态设置
                DropDownHeight = 294.DpiZoom(),
                ImeMode = ImeMode.Disable
            };

            private void InitializeComponents()
            {
                Controls.AddRange(new Control[] { cmbItems, btnOK, btnCancel });
                var a = 20.DpiZoom();
                cmbItems.Left = a;
                cmbItems.Width = 85.DpiZoom();
                cmbItems.Top = btnOK.Top = btnCancel.Top = a;
                btnOK.Left = cmbItems.Right + a;
                btnCancel.Left = btnOK.Right + a;
                ClientSize = new Size(btnCancel.Right + a, btnCancel.Bottom + a);
                cmbItems.AutosizeDropDownWidth();
            }
        }
    }
}