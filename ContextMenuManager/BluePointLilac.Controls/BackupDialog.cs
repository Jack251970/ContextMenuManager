using BluePointLilac.Methods;
using ContextMenuManager.BluePointLilac.Controls;
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
            using var frm = new SelectForm();
            frm.Text = Title;
            frm.CmbTitle = CmbTitle;
            frm.CmbItems = CmbItems;
            frm.TvTitle = TvTitle;
            frm.TvItems = TvItems;
            frm.CmbSelectedText = CmbSelectedText ?? (CmbSelectedIndex >= 0 ? CmbItems?[CmbSelectedIndex] : null);
            if (Control.FromHandle(hwndOwner) is Form owner) frm.TopMost = true;

            if (frm.ShowDialog() == DialogResult.OK)
            {
                CmbSelectedText = frm.CmbSelectedText;
                CmbSelectedIndex = frm.CmbSelectedIndex;
                TvSelectedItems = frm.TvSelectedItems;
                return true;
            }
            return false;
        }

        private sealed class SelectForm : RForm
        {
            public SelectForm()
            {
                SuspendLayout();
                AcceptButton = btnOK;
                CancelButton = btnCancel;
                Font = SystemFonts.MenuFont;
                ShowIcon = ShowInTaskbar = MaximizeBox = MinimizeBox = false;
                FormBorderStyle = FormBorderStyle.FixedSingle;
                StartPosition = FormStartPosition.CenterParent;
                InitializeComponents();
                ResumeLayout();
                InitTheme();
                DarkModeHelper.ThemeChanged += OnThemeChanged;
            }

            public string CmbTitle
            {
                get => cmbInfo.Text;
                set { cmbInfo.Text = value; cmbItems.Left = cmbInfo.Right; cmbItems.Width -= cmbInfo.Width; }
            }

            public string[] CmbItems
            {
                get
                {
                    var items = new string[cmbItems.Items.Count];
                    cmbItems.Items.CopyTo(items, 0);
                    return items;
                }
                set
                {
                    cmbItems.Items.Clear();
                    cmbItems.Items.AddRange(value);
                }
            }

            public int CmbSelectedIndex { get => cmbItems.SelectedIndex; set => cmbItems.SelectedIndex = value; }
            public string CmbSelectedText { get => cmbItems.Text; set => cmbItems.Text = value; }
            public string TvTitle { get => tvInfo.Text; set => tvInfo.Text = value; }

            public string[] TvItems
            {
                get => tvValue;
                set { tvValue = value; ShowTreeView(); }
            }

            public List<string> TvSelectedItems => GetSortedTvSelectedItems();

            private string[] tvValue;
            private readonly List<string> tvSelectedItems = new();
            private bool isFirst = true;
            private bool changeDone = false;

            private readonly Label tvInfo = new() { AutoSize = true };
            private readonly TreeView treeView = new()
            {
                ForeColor = DarkModeHelper.FormFore,
                BackColor = DarkModeHelper.FormBack,
                CheckBoxes = true,
                Indent = 20.DpiZoom(),
                ItemHeight = 25.DpiZoom(),
            };

            private readonly CheckBox checkAll = new()
            {
                Name = "CheckAll",
                Text = AppString.Dialog.SelectAll,
                AutoSize = true,
            };

            private readonly Label cmbInfo = new() { AutoSize = true };
            private readonly RComboBox cmbItems = new()
            {
                AutoCompleteMode = AutoCompleteMode.None,
                AutoCompleteSource = AutoCompleteSource.None,
                DropDownHeight = 300.DpiZoom(),
                DropDownStyle = ComboBoxStyle.DropDownList,
                ImeMode = ImeMode.Disable
            };

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

            private void InitializeComponents()
            {
                Controls.AddRange(new Control[] { tvInfo, treeView, checkAll, cmbInfo, cmbItems, btnOK, btnCancel });
                var margin = 20.DpiZoom();
                tvInfo.Top = checkAll.Top = margin;
                tvInfo.Left = treeView.Left = cmbInfo.Left = margin;
                treeView.Top = tvInfo.Bottom + 5.DpiZoom();
                treeView.Height = 300.DpiZoom();
                cmbInfo.Top = cmbItems.Top = treeView.Bottom + margin;
                cmbItems.Left = cmbInfo.Right;
                cmbItems.Width = 300.DpiZoom();
                btnOK.Top = btnCancel.Top = cmbItems.Bottom + margin;
                btnOK.Left = (cmbItems.Width + cmbInfo.Width + 2 * margin - margin) / 2 - btnOK.Width;
                btnCancel.Left = btnOK.Right + margin;
                ClientSize = new Size(cmbItems.Right + margin, btnCancel.Bottom + margin);
                treeView.Width = ClientSize.Width - 2 * margin;
                checkAll.Left = treeView.Right - checkAll.Width;
                checkAll.Click += CheckAll_Click;
                cmbItems.AutosizeDropDownWidth();

                treeView.BeforeCheck += TreeView_BeforeCheck;
                treeView.AfterSelect += TreeView_AfterSelect;
                treeView.AfterCheck += TreeView_AfterCheck;
            }

            private new void InitTheme()
            {
                BackColor = DarkModeHelper.FormBack;
                ForeColor = DarkModeHelper.FormFore;
                tvInfo.ForeColor = DarkModeHelper.FormFore;
                checkAll.ForeColor = DarkModeHelper.FormFore;
                cmbInfo.ForeColor = DarkModeHelper.FormFore;
                btnOK.BackColor = btnCancel.BackColor = DarkModeHelper.ButtonMain;
                btnOK.ForeColor = btnCancel.ForeColor = DarkModeHelper.FormFore;
                DarkModeHelper.AdjustControlColors(this);
            }

            private void OnThemeChanged(object sender, EventArgs e)
            {
                InitTheme();
                Invalidate();
            }

            private void ShowTreeView()
            {
                treeView.Nodes.AddRange(new[]
                {
                    new TreeNode(AppString.ToolBar.Home),
                    new TreeNode(AppString.ToolBar.Type),
                    new TreeNode(AppString.ToolBar.Rule)
                });

                foreach (var item in TvItems)
                {
                    var node = new TreeNode(item);
                    if (BackupHelper.HomeBackupScenesText.Contains(item))
                        treeView.Nodes[0].Nodes.Add(node);
                    else if (BackupHelper.TypeBackupScenesText.Contains(item))
                        treeView.Nodes[1].Nodes.Add(node);
                    else if (BackupHelper.RuleBackupScenesText.Contains(item))
                        treeView.Nodes[2].Nodes.Add(node);
                }

                for (var i = treeView.Nodes.Count - 1; i >= 0; i--)
                {
                    if (treeView.Nodes[i].Nodes.Count == 0)
                        treeView.Nodes.RemoveAt(i);
                }
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
                if (e.Node == null || changeDone) return;

                var node = e.Node;
                changeDone = true;

                if (node.Parent == null)
                {
                    foreach (TreeNode child in node.Nodes)
                    {
                        child.Checked = node.Checked;
                        UpdateSelectedItems(child, node.Checked);
                    }
                }
                else
                {
                    UpdateParentNodeState(node.Parent);
                    UpdateSelectedItems(node, node.Checked);
                }

                checkAll.Checked = tvSelectedItems.Count == tvValue.Length;
                changeDone = false;
            }

            private void UpdateParentNodeState(TreeNode parent)
            {
                var anyChecked = parent.Nodes.Cast<TreeNode>().Any(n => n.Checked);
                parent.Checked = anyChecked;
            }

            private void UpdateSelectedItems(TreeNode node, bool isChecked)
            {
                if (node.Parent == null) return;

                if (isChecked && !tvSelectedItems.Contains(node.Text))
                    tvSelectedItems.Add(node.Text);
                else if (!isChecked)
                    tvSelectedItems.Remove(node.Text);
            }

            private void TreeView_AfterSelect(object sender, TreeViewEventArgs e)
            {
                if (e.Node != null)
                {
                    e.Node.Checked = !e.Node.Checked;
                    treeView.SelectedNode = null;
                }
            }

            private void CheckAll_Click(object sender, EventArgs e)
            {
                var check = checkAll.Checked;
                foreach (TreeNode parent in treeView.Nodes)
                {
                    parent.Checked = check;
                    foreach (TreeNode child in parent.Nodes)
                    {
                        child.Checked = check;
                        UpdateSelectedItems(child, check);
                    }
                }
            }

            private List<string> GetSortedTvSelectedItems()
            {
                var selected = treeView.Nodes
                    .Cast<TreeNode>()
                    .SelectMany(p => p.Nodes.Cast<TreeNode>())
                    .Where(n => n.Checked)
                    .Select(n => n.Text)
                    .ToList();

                var sorted = new List<string>();
                var allItems = BackupHelper.HomeBackupScenesText
                    .Concat(BackupHelper.TypeBackupScenesText)
                    .Concat(BackupHelper.RuleBackupScenesText);

                foreach (var item in allItems)
                {
                    if (selected.Contains(item))
                        sorted.Add(item);
                }

                return sorted;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) DarkModeHelper.ThemeChanged -= OnThemeChanged;
                base.Dispose(disposing);
            }
        }
    }
}