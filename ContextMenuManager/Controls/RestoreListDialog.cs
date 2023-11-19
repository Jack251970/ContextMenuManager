using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    sealed class RestoreListDialog : CommonDialog
    {
        public List<RestoreChangedItem> RestoreData { get; set; }

        public override void Reset() { }

        protected override bool RunDialog(IntPtr hwndOwner)
        {
            using (RestoreListForm frm = new RestoreListForm())
            {
                frm.ShowDonateList(RestoreData);
                MainForm mainForm = (MainForm)Control.FromHandle(hwndOwner);
                frm.Left = mainForm.Left + (mainForm.Width + mainForm.SideBar.Width - frm.Width) / 2;
                frm.Top = mainForm.Top + 150.DpiZoom();
                frm.TopMost = AppConfig.TopMost;
                frm.ShowDialog();
            }
            return true;
        }

        sealed class RestoreListForm : Form
        {
            public RestoreListForm()
            {
                Font = SystemFonts.DialogFont;
                Text = AppString.Other.DonationList;
                SizeGripStyle = SizeGripStyle.Hide;
                StartPosition = FormStartPosition.Manual;
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                MinimizeBox = MaximizeBox = ShowInTaskbar = false;
                ClientSize = new Size(520, 350).DpiZoom();
                MinimumSize = Size;
                dgvRestore.ColumnHeadersDefaultCellStyle.Alignment
                    = dgvRestore.RowsDefaultCellStyle.Alignment
                    = DataGridViewContentAlignment.BottomCenter;
                Controls.AddRange(new Control[] { lblRestore, dgvRestore });
                lblRestore.Resize += (sender, e) => OnResize(null);
                this.AddEscapeButton();
            }

            readonly DataGridView dgvRestore = new DataGridView
            {
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = SystemColors.Control,
                BorderStyle = BorderStyle.None,
                AllowUserToResizeRows = false,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                ColumnHeadersVisible = false,
                MultiSelect = false,
                ReadOnly = true
            };

            readonly Label lblRestore = new Label { AutoSize = true };

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                int a = 20.DpiZoom();
                lblRestore.Location = new Point(a, a);
                dgvRestore.Location = new Point(a, lblRestore.Bottom + a);
                dgvRestore.Width = ClientSize.Width - 2 * a;
                dgvRestore.Height = ClientSize.Height - 3 * a - lblRestore.Height;
            }

            public void ShowDonateList(List<RestoreChangedItem> restoreList)
            {
                dgvRestore.ColumnCount = 4;
                dgvRestore.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                int restoreCount = restoreList.Count;
                for (int n = 0; n < restoreCount; n++)
                {
                    RestoreChangedItem item = restoreList[n];
                    Scenes scene = item.BackupScene;
                    string sceneText = BackupHelper.BackupScenesText[(int)scene];
                    string[] values;
                    if (Array.IndexOf(BackupHelper.TypeBackupScenesText, sceneText) != -1)
                    {
                        values = new[] { AppString.ToolBar.Type, sceneText, item.KeyName, item.ItemData };
                    }
                    else if (Array.IndexOf(BackupHelper.RuleBackupScenesText, sceneText) != -1)
                    {
                        values = new[] { AppString.ToolBar.Rule, sceneText, item.KeyName, item.ItemData };
                    }
                    else
                    {
                        values = new[] { AppString.ToolBar.Home, sceneText, item.KeyName, item.ItemData };
                    }
                    dgvRestore.Rows.Add(values);
                }
                lblRestore.Text = AppString.Message.RestoreSucceeded.Replace("%s", restoreCount.ToString()); ;
            }
        }
    }
}