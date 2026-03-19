using ContextMenuManager.Methods;
using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WpfProgressBar = System.Windows.Controls.ProgressBar;

namespace ContextMenuManager.Controls
{
    internal sealed class DownloadDialog : CommonDialog
    {
        public string Text { get; set; }
        public string Url { get; set; }
        public string FilePath { get; set; }
        public override void Reset() { }

        protected override bool RunDialog(IntPtr hwndOwner)
        {
            return ContentDialogHost.RunBlocking(async owner =>
            {
                var dialog = ContentDialogHost.CreateDialog(Text, hwndOwner);
                dialog.CloseButtonText = ResourceString.Cancel;
                dialog.DefaultButton = ContentDialogButton.Close;

                var progressBar = new WpfProgressBar
                {
                    Minimum = 0,
                    Maximum = 100,
                    Height = 8,
                    MinWidth = 320
                };
                dialog.Content = progressBar;

                using var cancellationSource = new CancellationTokenSource();
                var downloadTask = DownloadAsync(dialog, progressBar, cancellationSource.Token);
                dialog.CloseButtonClick += (_, _) => cancellationSource.Cancel();

                var _ = dialog.ShowAsync(owner);
                var success = await downloadTask;
                if (success || cancellationSource.IsCancellationRequested)
                {
                    dialog.Hide();
                }
                await _;
                return success;
            }, hwndOwner);
        }

        private async Task<bool> DownloadAsync(ContentDialog dialog, WpfProgressBar progressBar, CancellationToken cancellationToken)
        {
            try
            {
                using var client = new HttpClient();
                using var response = await client.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength;
                long totalBytesRead = 0;
                await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var fileStream = new FileStream(FilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                var buffer = new byte[8192];

                while (true)
                {
                    var bytesRead = await contentStream.ReadAsync(buffer, cancellationToken);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    totalBytesRead += bytesRead;

                    if (totalBytes.HasValue && totalBytes.Value > 0)
                    {
                        var progressPercentage = (int)((totalBytesRead * 100L) / totalBytes.Value);
                        dialog.Title = $"Downloading: {progressPercentage}%";
                        progressBar.Value = progressPercentage;
                    }
                    else
                    {
                        progressBar.IsIndeterminate = true;
                    }
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                TryDeleteFile();
                return false;
            }
            catch (Exception e)
            {
                TryDeleteFile();
                System.Windows.Forms.MessageBox.Show(e.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        private void TryDeleteFile()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                }
            }
            catch
            {
            }
        }
    }
}
