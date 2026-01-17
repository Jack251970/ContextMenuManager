using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    internal sealed class RestoreListDialog : CommonDialog
    {
        public List<RestoreChangedItem> RestoreData { get; set; }

        public override void Reset() { }

        protected override bool RunDialog(IntPtr hwndOwner)
        {
            var mainForm = (MainForm)Control.FromHandle(hwndOwner);
            if (mainForm != null)
            {
                using var frm = new RestoreListForm();
                frm.ShowDonateList(RestoreData);
                frm.Left = mainForm.Left + (mainForm.Width + mainForm.SideBar.Width - frm.Width) / 2;
                frm.Top = mainForm.Top + 150.DpiZoom();
                frm.TopMost = true;
                frm.ShowDialog();
            }
            return true;
        }

        private sealed class RestoreListForm : RForm
        {
            public RestoreListForm()
            {
                Font = SystemFonts.DialogFont;
                Text = AppString.Dialog.RestoreDetails;
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
                InitTheme();
                ApplyDarkModeToDataGridView(dgvRestore);
            }

            private readonly DataGridView dgvRestore = new()
            {
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = SystemColors.Control,
                BorderStyle = BorderStyle.None,
                AllowUserToResizeRows = false,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                ReadOnly = true
            };

            private readonly Label lblRestore = new()
            {
                Width = 480.DpiZoom()
            };

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                var a = 20.DpiZoom();
                lblRestore.Location = new Point(a, a);
                lblRestore.Width = ClientSize.Width;
                dgvRestore.Location = new Point(a, lblRestore.Bottom + a);
                dgvRestore.Width = ClientSize.Width - 2 * a;
                dgvRestore.Height = ClientSize.Height - 3 * a - lblRestore.Height;
            }

            public void ShowDonateList(List<RestoreChangedItem> restoreList)
            {
                var heads = new[] { AppString.Dialog.ItemLocation, AppString.Dialog.RestoredValue };
                dgvRestore.ColumnCount = heads.Length;
                dgvRestore.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                var restoreCount = restoreList.Count;
                for (var n = 0; n < heads.Length; n++)
                {
                    dgvRestore.Columns[n].HeaderText = heads[n];
                }
                for (var n = 0; n < restoreCount; n++)
                {
                    var item = restoreList[n];
                    var scene = item.BackupScene;
                    var sceneText = BackupHelper.BackupScenesText[(int)scene];
                    string[] values;
                    var changedValue = item.ItemData;
                    if (changedValue == false.ToString()) changedValue = AppString.Dialog.Disabled;
                    if (changedValue == true.ToString()) changedValue = AppString.Dialog.Enabled;
                    if (Array.IndexOf(BackupHelper.TypeBackupScenesText, sceneText) != -1)
                    {
                        values = new[] { AppString.ToolBar.Type + " -> " + sceneText + " -> " + item.KeyName, changedValue };
                    }
                    else if (Array.IndexOf(BackupHelper.RuleBackupScenesText, sceneText) != -1)
                    {
                        values = new[] { AppString.ToolBar.Rule + " -> " + sceneText + " -> " + item.KeyName, changedValue };
                    }
                    else
                    {
                        values = new[] { AppString.ToolBar.Home + " -> " + sceneText + " -> " + item.KeyName, changedValue };
                    }
                    dgvRestore.Rows.Add(values);
                }
                lblRestore.Text = AppString.Message.RestoreSucceeded.Replace("%s", restoreCount.ToString()); ;
            }
        }
    }
}