using BluePointLilac.Methods;
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
            using (SelectForm frm = new SelectForm())
            {
                frm.Text = Title;
                frm.Items = Items;
                if (Selected != null) frm.Selected = Selected;
                else frm.SelectedIndex = SelectedIndex;
                frm.CanEdit = CanEdit;
                if (Control.FromHandle(hwndOwner) is Form owner) frm.TopMost = true;
                bool flag = frm.ShowDialog() == DialogResult.OK;
                if (flag)
                {
                    Selected = frm.Selected;
                    SelectedIndex = frm.SelectedIndex;
                }
                return flag;
            }
        }

        sealed class SelectForm : RForm
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

                // 确保 ComboBox 正确初始化主题颜色
                InitializeComboBoxColors();
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
                    string[] value = new string[cmbItems.Items.Count];
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
                    // 根据编辑模式设置自动完成功能
                    if (value)
                    {
                        // 可编辑模式：启用自动完成
                        cmbItems.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                        cmbItems.AutoCompleteSource = AutoCompleteSource.ListItems;
                    }
                    else
                    {
                        // 下拉列表模式：禁用自动完成
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

            readonly MyButton btnOK = new MyButton
            {
                DialogResult = DialogResult.OK,
                Text = ResourceString.OK,
                AutoSize = true
            };
            readonly MyButton btnCancel = new MyButton
            {
                DialogResult = DialogResult.Cancel,
                Text = ResourceString.Cancel,
                AutoSize = true
            };
            readonly RComboBox cmbItems = new RComboBox
            {
                // 移除默认的自动完成设置，将在 CanEdit 属性中根据模式设置
                DropDownHeight = 294.DpiZoom(),
                ImeMode = ImeMode.Disable
            };

            private void InitializeComponents()
            {
                Controls.AddRange(new Control[] { cmbItems, btnOK, btnCancel });
                int a = 20.DpiZoom();
                cmbItems.Left = a;
                cmbItems.Width = 85.DpiZoom();
                cmbItems.Top = btnOK.Top = btnCancel.Top = a;
                btnOK.Left = cmbItems.Right + a;
                btnCancel.Left = btnOK.Right + a;
                ClientSize = new Size(btnCancel.Right + a, btnCancel.Bottom + a);
                cmbItems.AutosizeDropDownWidth();

                // 初始化自动完成设置
                CanEdit = false; // 默认设置为不可编辑模式

                // 添加主题变化事件处理
                this.Load += (s, e) => InitializeComboBoxColors();
            }

            private void InitializeComboBoxColors()
            {
                // 确保 RComboBox 使用正确的主题颜色
                if (MyMainForm.IsDarkTheme())
                {
                    cmbItems.BackColor = Color.FromArgb(45, 45, 48);
                    cmbItems.ForeColor = Color.FromArgb(245, 245, 245);
                    cmbItems.BorderColor = Color.FromArgb(70, 70, 75);
                }
                else
                {
                    cmbItems.BackColor = Color.FromArgb(250, 250, 252);
                    cmbItems.ForeColor = Color.FromArgb(25, 25, 25);
                    cmbItems.BorderColor = Color.FromArgb(210, 210, 215);
                }

                // 强制刷新 ComboBox
                cmbItems.Invalidate();
            }
        }
    }
}