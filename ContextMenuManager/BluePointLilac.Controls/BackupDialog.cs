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
        public string Title { get; set; }   // 窗口标题

        public string CmbTitle { get; set; }    // cmb标题
        public string[] CmbItems { get; set; }  // cmb可供选择内容
        public int CmbSelectedIndex { get; set; }   // cmb选择内容索引
        public string CmbSelectedText { get; set; } // cmb选择内容文字

        public string TvTitle { get; set; }    // tv可供选择内容
        public string[] TvItems { get; set; }  // tv选择内容索引
        public List<string> TvSelectedItems { get; set; }  // tv选择内容文字

        public override void Reset() { }

        protected override bool RunDialog(IntPtr hwndOwner)
        {
            using(SelectForm frm = new SelectForm())
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
                if(flag)
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
            /*************************************外部函数***********************************/

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

            /*************************************外部属性***********************************/

            public string CmbTitle
            {
                get => cmbInfo.Text;
                set {
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
            // cmb选中项目索引
            public int CmbSelectedIndex
            {
                get => cmbItems.SelectedIndex;
                set => cmbItems.SelectedIndex = value;
            }
            // cmb选中项目内容
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
                get
                {
                    return tvValue;
                }
                set
                {
                    tvValue = value;
                    ShowTreeView();
                }
            }
            // tv选中的项目
            private readonly List<string> tvSelectedItems = new List<string>();
            public List<string> TvSelectedItems
            {
                get => GetSortedTvSelectedItems();
            }

            /*************************************内部控件***********************************/

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
                DropDownStyle = ComboBoxStyle.DropDownList, // 用户不可增加新项目
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

            /*************************************内部函数***********************************/

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
                checkAll.Click += (sender, e) => { CheckAll_CheckBoxMouseClick(sender, e); };
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

                    // 判断treeNodeText是否在BackupHelper.HomeBackupScenesText中
                    if (BackupHelper.HomeBackupScenesText.Contains(treeNodeText))
                    {
                        treeView.Nodes[0].Nodes.Add(new TreeNode(treeNodeText));
                    }
                    else if (BackupHelper.TypeBackupScenesText.Contains(treeNodeText))
                    {
                        treeView.Nodes[1].Nodes.Add(new TreeNode(treeNodeText));
                    }
                    else if (BackupHelper.RuleBackupScenesText.Contains(treeNodeText))
                    {
                        treeView.Nodes[2].Nodes.Add(new TreeNode(treeNodeText));
                    }
                }

                for (int i = 0; i < treeView.Nodes.Count; i++)
                {
                    // 如果该根节点下不存在任何子节点，则删除该根节点
                    if (treeView.Nodes[i].Nodes.Count == 0)
                    {
                        treeView.Nodes.RemoveAt(i);
                        i--;
                    }
                }

                // 取消第一个根节点CheckBox的默认选中状态
                treeView.BeforeCheck += TreeView_BeforeCheck;

                // 点击节点文字事件
                treeView.AfterSelect += TreeView_AfterSelect;

                // 节点Checked改变事件
                treeView.AfterCheck += TreeView_AfterCheck;
            }

            private void TreeView_BeforeCheck(object sender, TreeViewCancelEventArgs e)
            {
                // 第一次取消第一个根节点的CheckBox选中状态
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

                    if ((nodeText == AppString.ToolBar.Home) || (nodeText == AppString.ToolBar.Type) || (nodeText == AppString.ToolBar.Rule))
                    {
                        // 所有子节点状态同父节点
                        for (int i = 0; i < node.Nodes.Count; i++)
                        {
                            TreeNode childNode = node.Nodes[i];
                            childNode.Checked = isChecked;
                            if (isChecked)
                            {
                                if (!tvSelectedItems.Contains(childNode.Text))
                                {
                                    tvSelectedItems.Add(childNode.Text);
                                }
                                if (tvSelectedItems.Count == tvValue.Length)
                                {
                                    checkAll.Checked = true;
                                }
                            } 
                            else
                            {
                                tvSelectedItems.Remove(childNode.Text);
                                if (tvSelectedItems.Count < tvValue.Length)
                                {
                                    checkAll.Checked = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        // 兄弟节点被选中的个数 
                        int brotherNodeCheckedCount = 0;
                        foreach (TreeNode tn in node.Parent.Nodes)
                        {
                            if (tn.Checked == true)
                            {
                                brotherNodeCheckedCount++;
                            }
                        }

                        // 兄弟节点全没选，其父节点也不选 
                        if (brotherNodeCheckedCount == 0)
                        {
                            node.Parent.Checked = false;
                        }
                        // 兄弟节点只要有一个被选，其父节点也被选 
                        if (brotherNodeCheckedCount >= 1)
                        {
                            node.Parent.Checked = true;
                        }

                        if (isChecked)
                        {
                            if (!tvSelectedItems.Contains(node.Text))
                            {
                                tvSelectedItems.Add(node.Text);
                            }
                            if (tvSelectedItems.Count == tvValue.Length)
                            {
                                checkAll.Checked = true;
                            }
                        }
                        else
                        {
                            tvSelectedItems.Remove(node.Text);
                            if (tvSelectedItems.Count < tvValue.Length)
                            {
                                checkAll.Checked = false;
                            }
                        }
                    }

                    changeDone = false;
                }
            }

            private void TreeView_AfterSelect(object sender, TreeViewEventArgs e)
            {
                if (e.Node != null)
                {
                    // 传递节点Checked改变
                    e.Node.Checked = !e.Node.Checked;

                    // 取消选中，去除蓝色背景
                    treeView.SelectedNode = null;
                }
            }

            private void CheckAll_CheckBoxMouseClick(object sender, EventArgs e)
            {
                // 传递根节点Checked改变
                for (int i = 0; i < treeView.Nodes.Count; i++)
                {
                    for (int j = 0; j < treeView.Nodes[i].Nodes.Count; j++)
                    {
                        treeView.Nodes[i].Nodes[j].Checked = checkAll.Checked;
                        if (checkAll.Checked)
                        {
                            if (!tvSelectedItems.Contains(treeView.Nodes[i].Nodes[j].Text))
                            {
                                tvSelectedItems.Add(treeView.Nodes[i].Nodes[j].Text);
                            }
                        }
                        else
                        {
                            tvSelectedItems.Remove(treeView.Nodes[i].Nodes[j].Text);
                        }
                    }
                    treeView.Nodes[i].Checked = checkAll.Checked;
                }
            }

            private List<string> GetSortedTvSelectedItems()
            {
                // 获取Checked的节点
                List<string> tvSelectedItems = new List<string>();
                for (int i = 0; i < treeView.Nodes.Count; i++)
                {
                    for (int j = 0; j < treeView.Nodes[i].Nodes.Count; j++)
                    {
                        if (treeView.Nodes[i].Nodes[j].Checked)
                        {
                            tvSelectedItems.Add(treeView.Nodes[i].Nodes[j].Text);
                        }
                    }
                }

                // 对节点文字进行排序
                List<string> sortedTvSelectedItems = new List<string>();
                for (int i = 0; i < BackupHelper.HomeBackupScenesText.Length; i++)
                {
                    if (tvSelectedItems.Contains(BackupHelper.HomeBackupScenesText[i]))
                    {
                        sortedTvSelectedItems.Add(BackupHelper.HomeBackupScenesText[i]);
                    }
                }
                for (int i = 0; i < BackupHelper.TypeBackupScenesText.Length; i++)
                {
                    if (tvSelectedItems.Contains(BackupHelper.TypeBackupScenesText[i]))
                    {
                        sortedTvSelectedItems.Add(BackupHelper.TypeBackupScenesText[i]);
                    }
                }
                for (int i = 0; i < BackupHelper.RuleBackupScenesText.Length; i++)
                {
                    if (tvSelectedItems.Contains(BackupHelper.RuleBackupScenesText[i]))
                    {
                        sortedTvSelectedItems.Add(BackupHelper.RuleBackupScenesText[i]);
                    }
                }

                return sortedTvSelectedItems;
            }
        }
    }
}