using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

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
            using (SelectForm frm = new SelectForm())
            {
                frm.Text = Title;
                frm.CmbTitle = CmbTitle;
                frm.CmbItems = CmbItems;
                frm.TvTitle = TvTitle;
                frm.TvItems = TvItems;
                if (CmbSelectedText != null) frm.CmbSelectedText = CmbSelectedText;
                else frm.CmbSelectedIndex = CmbSelectedIndex;
                if (Control.FromHandle(hwndOwner) is Form owner) frm.TopMost = true;
                bool flag = frm.ShowDialog() == DialogResult.OK;
                if (flag)
                {
                    CmbSelectedText = frm.CmbSelectedText;
                    CmbSelectedIndex = frm.CmbSelectedIndex;
                    TvSelectedItems = frm.TvSelectedItems;
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
            }

            public string CmbTitle
            {
                get => cmbInfo.Text;
                set
                {
                    cmbInfo.Text = value;
                    cmbItems.Left = cmbInfo.Right;
                    cmbItems.Width -= cmbInfo.Width;
                }
            }
            public string[] CmbItems
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
            public int CmbSelectedIndex
            {
                get => cmbItems.SelectedIndex;
                set => cmbItems.SelectedIndex = value;
            }
            public string CmbSelectedText
            {
                get => cmbItems.Text;
                set => cmbItems.Text = value;
            }

            public string TvTitle
            {
                get => tvInfo.Text;
                set => tvInfo.Text = value;
            }
            private string[] tvValue;
            public string[] TvItems
            {
                get => tvValue;
                set { tvValue = value; ShowTreeView(); }
            }
            private readonly List<string> tvSelectedItems = new List<string>();
            public List<string> TvSelectedItems => GetSortedTvSelectedItems();

            readonly Label tvInfo = new Label { AutoSize = true };
            readonly TreeView treeView = new TreeView
            {
                ForeColor = MyMainForm.FormFore,
                BackColor = MyMainForm.FormBack,
                CheckBoxes = true,
                Indent = 20.DpiZoom(),
                ItemHeight = 25.DpiZoom(),
            };
            private bool isFirst = true;
            private bool changeDone = false;

            readonly CheckBox checkAll = new CheckBox
            {
                Name = "CheckAll",
                Text = AppString.Dialog.SelectAll,
                AutoSize = true,
            };

            readonly Label cmbInfo = new Label { AutoSize = true };
            readonly RComboBox cmbItems = new RComboBox
            {
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                DropDownHeight = 300.DpiZoom(),
                DropDownStyle = ComboBoxStyle.DropDownList,
                ImeMode = ImeMode.Disable
            };

            readonly Button btnOK = new Button
            {
                DialogResult = DialogResult.OK,
                Text = ResourceString.OK,
                AutoSize = true
            };
            readonly Button btnCancel = new Button
            {
                DialogResult = DialogResult.Cancel,
                Text = ResourceString.Cancel,
                AutoSize = true
            };

            private void InitializeComponents()
            {
                Controls.AddRange(new Control[] { tvInfo, treeView, checkAll, cmbInfo, cmbItems, btnOK, btnCancel });
                int margin = 20.DpiZoom();
                int cmbItemsWidth = 300.DpiZoom();
                int tvHeight = 300.DpiZoom();
                tvInfo.Top = checkAll.Top = margin;
                tvInfo.Left = treeView.Left = cmbInfo.Left = margin;
                treeView.Top = tvInfo.Bottom + 5.DpiZoom();
                treeView.Height = tvHeight;
                cmbInfo.Top = cmbItems.Top = treeView.Bottom + margin;
                cmbItems.Left = cmbInfo.Right;
                cmbItems.Width = cmbItemsWidth;
                btnOK.Top = btnCancel.Top = cmbItems.Bottom + margin;
                btnOK.Left = (cmbItems.Width + cmbInfo.Width + 2 * margin - margin) / 2 - btnOK.Width;
                btnCancel.Left = btnOK.Right + margin;
                ClientSize = new Size(cmbItems.Right + margin, btnCancel.Bottom + margin);
                treeView.Width = ClientSize.Width - 2 * margin;
                checkAll.Left = treeView.Right - checkAll.Width;
                checkAll.Click += CheckAll_CheckBoxMouseClick;
                cmbItems.AutosizeDropDownWidth();
            }

            private void ShowTreeView()
            {
                treeView.Nodes.Add(new TreeNode(AppString.ToolBar.Home));
                treeView.Nodes.Add(new TreeNode(AppString.ToolBar.Type));
                treeView.Nodes.Add(new TreeNode(AppString.ToolBar.Rule));

                for (int i = 0; i < TvItems.Length; i++)
                {
                    string treeNodeText = TvItems[i];
                    if (BackupHelper.HomeBackupScenesText.Contains(treeNodeText))
                        treeView.Nodes[0].Nodes.Add(new TreeNode(treeNodeText));
                    else if (BackupHelper.TypeBackupScenesText.Contains(treeNodeText))
                        treeView.Nodes[1].Nodes.Add(new TreeNode(treeNodeText));
                    else if (BackupHelper.RuleBackupScenesText.Contains(treeNodeText))
                        treeView.Nodes[2].Nodes.Add(new TreeNode(treeNodeText));
                }

                for (int i = 0; i < treeView.Nodes.Count; i++)
                {
                    if (treeView.Nodes[i].Nodes.Count == 0)
                    {
                        treeView.Nodes.RemoveAt(i);
                        i--;
                    }
                }

                treeView.BeforeCheck += TreeView_BeforeCheck;
                treeView.AfterSelect += TreeView_AfterSelect;
                treeView.AfterCheck += TreeView_AfterCheck;
            }

            private void TreeView_BeforeCheck(object sender, TreeViewCancelEventArgs e)
            {
                if (e.Node == treeView.Nodes[0] && isFirst)
                {
                    e.Cancel = true;
                    isFirst = false;
                }
            }

            private void TreeView_AfterCheck(object sender, TreeViewEventArgs e)
            {
                if (e.Node != null && !changeDone)
                {
                    TreeNode node = e.Node;
                    bool isChecked = node.Checked;
                    string nodeText = e.Node.Text;
                    changeDone = true;

                    if (nodeText == AppString.ToolBar.Home || nodeText == AppString.ToolBar.Type || nodeText == AppString.ToolBar.Rule)
                    {
                        for (int i = 0; i < node.Nodes.Count; i++)
                        {
                            TreeNode childNode = node.Nodes[i];
                            childNode.Checked = isChecked;
                            if (isChecked)
                            {
                                if (!tvSelectedItems.Contains(childNode.Text))
                                    tvSelectedItems.Add(childNode.Text);
                            }
                            else
                                tvSelectedItems.Remove(childNode.Text);
                        }
                    }
                    else
                    {
                        int brotherNodeCheckedCount = node.Parent.Nodes.Cast<TreeNode>().Count(tn => tn.Checked);
                        node.Parent.Checked = brotherNodeCheckedCount >= 1;
                        if (isChecked)
                        {
                            if (!tvSelectedItems.Contains(node.Text))
                                tvSelectedItems.Add(node.Text);
                        }
                        else
                            tvSelectedItems.Remove(node.Text);
                    }
                    checkAll.Checked = tvSelectedItems.Count == tvValue.Length;
                    changeDone = false;
                }
            }

            private void TreeView_AfterSelect(object sender, TreeViewEventArgs e)
            {
                if (e.Node != null)
                {
                    e.Node.Checked = !e.Node.Checked;
                    treeView.SelectedNode = null;
                }
            }

            private void CheckAll_CheckBoxMouseClick(object sender, EventArgs e)
            {
                // 修复：设置 changeDone 为 true 防止递归调用
                changeDone = true;

                bool isChecked = checkAll.Checked;
                for (int i = 0; i < treeView.Nodes.Count; i++)
                {
                    for (int j = 0; j < treeView.Nodes[i].Nodes.Count; j++)
                    {
                        treeView.Nodes[i].Nodes[j].Checked = isChecked;
                        if (isChecked)
                        {
                            if (!tvSelectedItems.Contains(treeView.Nodes[i].Nodes[j].Text))
                                tvSelectedItems.Add(treeView.Nodes[i].Nodes[j].Text);
                        }
                        else
                            tvSelectedItems.Remove(treeView.Nodes[i].Nodes[j].Text);
                    }
                    treeView.Nodes[i].Checked = isChecked;
                }

                changeDone = false;
            }

            private List<string> GetSortedTvSelectedItems()
            {
                // 直接从 tvSelectedItems 获取已选项，而不是重新遍历树节点
                List<string> sortedTvSelectedItems =
                [
                    // 按照原始顺序排序
                    .. BackupHelper.HomeBackupScenesText.Where(tvSelectedItems.Contains),
                    .. BackupHelper.TypeBackupScenesText.Where(tvSelectedItems.Contains),
                    .. BackupHelper.RuleBackupScenesText.Where(tvSelectedItems.Contains),
                ];

                return sortedTvSelectedItems;
            }
        }
    }
}