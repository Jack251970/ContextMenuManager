using BluePointLilac.Methods;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    sealed class DownloadDialog : CommonDialog
    {
        public string Text { get; set; }
        public string Url { get; set; }
        public string FilePath { get; set; }
        public override void Reset() { }

        protected override bool RunDialog(IntPtr hwndOwner)
        {
            using(Process process = Process.GetCurrentProcess())
            using(DownloadForm frm = new DownloadForm())
            {
                frm.Url = Url;
                frm.Text = Text;
                frm.FilePath = FilePath;
                return frm.ShowDialog() == DialogResult.OK;
            }
        }

        sealed class DownloadForm : RForm
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
            }

            readonly ProgressBar pgbDownload = new ProgressBar
            {
                Width = 200.DpiZoom(),
                Maximum = 100
            };
            readonly Button btnCancel = new Button
            {
                DialogResult = DialogResult.Cancel,
                Text = ResourceString.Cancel,
                AutoSize = true
            };

            public string Url { get; set; }
            public string FilePath { get; set; }

            private void InitializeComponents()
            {
                int a = 20.DpiZoom();
                pgbDownload.Left = pgbDownload.Top = btnCancel.Top = a;
                pgbDownload.Height = btnCancel.Height;
                btnCancel.Left = pgbDownload.Right + a;
                ClientSize = new Size(btnCancel.Right + a, btnCancel.Bottom + a);
            }

            private void DownloadFile(string url, string filePath)
            {
                try
                {
                    using(UAWebClient client = new UAWebClient())
                    {
                        client.DownloadProgressChanged += (sender, e) =>
                        {
                            int value = e.ProgressPercentage;
                            Text = $"Downloading: {value}%";
                            pgbDownload.Value = value;
                            if(DialogResult == DialogResult.Cancel)
                            {
                                client.CancelAsync();
                                File.Delete(FilePath);
                            }
                        };
                        client.DownloadFileCompleted += (sender, e) =>
                        {
                            DialogResult = DialogResult.OK;
                        };
                        client.DownloadFileAsync(new Uri(url), filePath);
                    }
                }
                catch(Exception e)
                {
                    MessageBox.Show(e.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.Cancel;
                }
            }

            protected override void OnLoad(EventArgs e)
            {
                if(Owner == null && Form.ActiveForm != this) Owner = Form.ActiveForm;
                if(Owner == null) StartPosition = FormStartPosition.CenterScreen;
                else
                {
                    TopMost = Owner.TopMost;
                    StartPosition = FormStartPosition.CenterParent;
                }
                base.OnLoad(e);
            }
        }
    }
}