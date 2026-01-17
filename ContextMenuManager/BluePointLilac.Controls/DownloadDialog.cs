using BluePointLilac.Methods;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    internal sealed class DownloadDialog : CommonDialog
    {
        public string Text { get; set; }
        public string Url { get; set; }
        public string FilePath { get; set; }
        public override void Reset() { }

        protected override bool RunDialog(IntPtr hwndOwner)
        {
            using var process = Process.GetCurrentProcess();
            using var frm = new DownloadForm();
            frm.Url = Url;
            frm.Text = Text;
            frm.FilePath = FilePath;
            return frm.ShowDialog() == DialogResult.OK;
        }

        private sealed class DownloadForm : RForm
        {
            public DownloadForm()
            {
                SuspendLayout();
                Font = SystemFonts.MessageBoxFont;
                FormBorderStyle = FormBorderStyle.FixedSingle;
                MinimizeBox = MaximizeBox = ShowInTaskbar = false;
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                Controls.AddRange(new Control[] { pgbDownload, btnCancel });
                Load += (sender, e) => DownloadFile(Url, FilePath);
                InitializeComponents();
                ResumeLayout();
                InitTheme();

                // 监听主题变化
                DarkModeHelper.ThemeChanged += OnThemeChanged;
            }

            private readonly ProgressBar pgbDownload = new()
            {
                Width = 200.DpiZoom(),
                Maximum = 100
            };
            private readonly Button btnCancel = new()
            {
                DialogResult = DialogResult.Cancel,
                Text = ResourceString.Cancel,
                AutoSize = true
            };

            public string Url { get; set; }
            public string FilePath { get; set; }

            private void InitializeComponents()
            {
                var a = 20.DpiZoom();
                pgbDownload.Left = pgbDownload.Top = btnCancel.Top = a;
                pgbDownload.Height = btnCancel.Height;
                btnCancel.Left = pgbDownload.Right + a;
                ClientSize = new Size(btnCancel.Right + a, btnCancel.Bottom + a);
            }

            private new void InitTheme()
            {
                BackColor = DarkModeHelper.FormBack;
                ForeColor = DarkModeHelper.FormFore;

                btnCancel.BackColor = DarkModeHelper.ButtonMain;
                btnCancel.ForeColor = DarkModeHelper.FormFore;
            }

            // 主题变化事件处理
            private void OnThemeChanged(object sender, EventArgs e)
            {
                InitTheme();
                Invalidate();
            }

            private async void DownloadFile(string url, string filePath)
            {
                try
                {
                    using (var client = new System.Net.Http.HttpClient())
                    using (var response = await client.GetAsync(url, System.Net.Http.HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        var totalBytes = response.Content.Headers.ContentLength;
                        long totalBytesRead = 0;

                        using var contentStream = await response.Content.ReadAsStreamAsync();
                        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                        var buffer = new byte[8192];
                        int bytesRead;
                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            if (DialogResult == DialogResult.Cancel)
                            {
                                File.Delete(FilePath);
                                return;
                            }
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;
                            if (totalBytes.HasValue)
                            {
                                var progressPercentage = (int)((totalBytesRead * 100) / totalBytes.Value);
                                Text = $"Downloading: {progressPercentage}%";
                                pgbDownload.Value = progressPercentage;
                            }
                        }
                    }
                    DialogResult = DialogResult.OK;
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.Cancel;
                }
            }

            protected override void OnLoad(EventArgs e)
            {
                if (Owner == null && Form.ActiveForm != this) Owner = Form.ActiveForm;
                if (Owner == null) StartPosition = FormStartPosition.CenterScreen;
                else
                {
                    TopMost = true;
                    StartPosition = FormStartPosition.CenterParent;
                }
                base.OnLoad(e);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    DarkModeHelper.ThemeChanged -= OnThemeChanged;
                }
                base.Dispose(disposing);
            }
        }
    }
}