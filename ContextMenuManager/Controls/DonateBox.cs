using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    internal sealed class DonateBox : Panel
    {
        public DonateBox()
        {
            SuspendLayout();
            AutoScroll = true;
            Dock = DockStyle.Fill;
            ForeColor = DarkModeHelper.FormFore; // 修改这里
            BackColor = DarkModeHelper.FormBack; // 修改这里
            Font = SystemFonts.MenuFont;
            Font = new Font(Font.FontFamily, Font.Size + 1F);
            Controls.AddRange(new Control[] { lblInfo, picQR, lblList });
            VisibleChanged += (sender, e) => this.SetEnabled(Visible);
            lblList.Click += (sender, e) => ShowDonateDialog();
            picQR.Resize += (sender, e) => OnResize(null);
            picQR.MouseDown += SwitchQR;
            ResumeLayout();
        }

        private readonly Label lblInfo = new()
        {
            Text = AppString.Other.Donate,
            AutoSize = true
        };

        private readonly Label lblList = new()
        {
            ForeColor = DarkModeHelper.FormFore, // 修改这里
            Text = AppString.Other.DonationList,
            Cursor = Cursors.Hand,
            AutoSize = true
        };

        private readonly PictureBox picQR = new()
        {
            SizeMode = PictureBoxSizeMode.AutoSize,
            Cursor = Cursors.Hand,
            Image = AllQR,
        };

        private static readonly Image AllQR = Properties.Resources.Donate;
        private static readonly Image WechatQR = GetSingleQR(0);
        private static readonly Image AlipayQR = GetSingleQR(1);
        private static readonly Image QQQR = GetSingleQR(2);
        private static Image GetSingleQR(int index)
        {
            var bitmap = new Bitmap(200, 200);
            using (var g = Graphics.FromImage(bitmap))
            {
                var destRect = new Rectangle(0, 0, 200, 200);
                var srcRect = new Rectangle(index * 200, 0, 200, 200);
                g.DrawImage(AllQR, destRect, srcRect, GraphicsUnit.Pixel);
            }
            return bitmap;
        }

        protected override void OnResize(EventArgs e)
        {
            var a = 60.DpiZoom();
            base.OnResize(e);
            picQR.Left = (Width - picQR.Width) / 2;
            lblInfo.Left = (Width - lblInfo.Width) / 2;
            lblList.Left = (Width - lblList.Width) / 2;
            lblInfo.Top = a;
            picQR.Top = lblInfo.Bottom + a;
            lblList.Top = picQR.Bottom + a;
        }

        private void SwitchQR(object sender, MouseEventArgs e)
        {
            if (picQR.Image == AllQR)
            {
                if (e.X < 200) picQR.Image = WechatQR;
                else if (e.X < 400) picQR.Image = AlipayQR;
                else picQR.Image = QQQR;
            }
            else
            {
                picQR.Image = AllQR;
            }
        }

        private async void ShowDonateDialog()
        {
            Cursor = Cursors.WaitCursor;
            using (var client = new UAWebClient())
            {
                var url = AppConfig.RequestUseGithub ? AppConfig.GithubDonateRaw : AppConfig.GiteeDonateRaw;
                var contents = await client.GetWebStringAsync(url);
                //contents = System.IO.File.ReadAllText(@"..\..\..\Donate.md");//用于求和更新Donate.md文件
                if (contents == null)
                {
                    if (AppMessageBox.Show(AppString.Message.WebDataReadFailed + "\r\n"
                        + AppString.Message.OpenWebUrl, MessageBoxButtons.OKCancel) == DialogResult.OK)
                    {
                        url = AppConfig.RequestUseGithub ? AppConfig.GithubDonate : AppConfig.GiteeDonate;
                        ExternalProgram.OpenWebUrl(url);
                    }
                }
                else
                {
                    using var dlg = new DonateListDialog();
                    dlg.DanateData = contents;
                    dlg.ShowDialog();
                }
            }
            Cursor = Cursors.Default;
        }

        private sealed class DonateListDialog : CommonDialog
        {
            public string DanateData { get; set; }

            public override void Reset() { }

            protected override bool RunDialog(IntPtr hwndOwner)
            {
                using var frm = new DonateListForm();
                frm.ShowDonateList(DanateData);
                var mainForm = (MainForm)FromHandle(hwndOwner);
                frm.Left = mainForm.Left + (mainForm.Width + mainForm.SideBar.Width - frm.Width) / 2;
                frm.Top = mainForm.Top + 150.DpiZoom();
                frm.TopMost = true;
                frm.ShowDialog();
                return true;
            }

            private sealed class DonateListForm : RForm
            {
                public DonateListForm()
                {
                    Font = SystemFonts.DialogFont;
                    Text = AppString.Other.DonationList;
                    SizeGripStyle = SizeGripStyle.Hide;
                    StartPosition = FormStartPosition.Manual;
                    Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                    MinimizeBox = MaximizeBox = ShowInTaskbar = false;
                    ClientSize = new Size(520, 350).DpiZoom();
                    MinimumSize = Size;
                    dgvDonate.ColumnHeadersDefaultCellStyle.Alignment
                        = dgvDonate.RowsDefaultCellStyle.Alignment
                        = DataGridViewContentAlignment.BottomCenter;
                    Controls.AddRange(new Control[] { lblThank, lblDonate, dgvDonate });
                    lblThank.MouseEnter += (sender, e) => lblThank.ForeColor = DarkModeHelper.MainColor; // 修改这里
                    lblThank.MouseLeave += (sender, e) => lblThank.ForeColor = Color.DimGray;//Fixed
                    lblDonate.Resize += (sender, e) => OnResize(null);
                    this.AddEscapeButton();
                    InitTheme();
                    ApplyDarkModeToDataGridView(dgvDonate);
                }

                private readonly DataGridView dgvDonate = new()
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

                private readonly Label lblDonate = new()
                { AutoSize = true };
                private readonly Label lblThank = new()
                {
                    Font = new Font("Lucida Handwriting", 15F),
                    ForeColor = Color.DimGray,//Fixed
                    Text = "Thank you!",
                    AutoSize = true,
                };

                protected override void OnResize(EventArgs e)
                {
                    base.OnResize(e);
                    var a = 20.DpiZoom();
                    lblDonate.Location = new Point(a, a);
                    dgvDonate.Location = new Point(a, lblDonate.Bottom + a);
                    dgvDonate.Width = ClientSize.Width - 2 * a;
                    dgvDonate.Height = ClientSize.Height - 3 * a - lblDonate.Height;
                    lblThank.Location = new Point(dgvDonate.Right - lblThank.Width, lblDonate.Bottom - lblThank.Height);
                }

                public void ShowDonateList(string contents)
                {
                    var lines = contents.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    var index = Array.FindIndex(lines, line => line == "|:--:|:--:|:--:|:--:|:--:");
                    if (index == -1) return;
                    var heads = lines[index - 1].Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    dgvDonate.ColumnCount = heads.Length;
                    dgvDonate.Columns[4].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    for (var m = 0; m < heads.Length; m++)
                    {
                        dgvDonate.Columns[m].HeaderText = heads[m];
                    }
                    for (var n = index + 1; n < lines.Length; n++)
                    {
                        var strs = lines[n].Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                        var values = new object[strs.Length];
                        for (var k = 0; k < strs.Length; k++)
                        {
                            values[k] = k switch
                            {
                                3 => Convert.ToSingle(strs[k]),
                                _ => strs[k],
                            };
                        }
                        dgvDonate.Rows.Add(values);
                    }
                    dgvDonate.Sort(dgvDonate.Columns[0], ListSortDirection.Descending);
                    var date = Convert.ToDateTime(dgvDonate.Rows[0].Cells[0].Value);
                    var money = dgvDonate.Rows.Cast<DataGridViewRow>().Sum(row => (float)row.Cells[3].Value);
                    lblDonate.Text = AppString.Dialog.DonateInfo.Replace("%date", date.ToLongDateString())
                        .Replace("%money", money.ToString()).Replace("%count", dgvDonate.RowCount.ToString());
                }
            }
        }
    }
}