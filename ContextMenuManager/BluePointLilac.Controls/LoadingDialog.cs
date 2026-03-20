using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Drawing;
using System.Threading;
using System.Windows;
using DrawingContentAlignment = System.Drawing.ContentAlignment;
using WpfProgressBar = System.Windows.Controls.ProgressBar;

namespace ContextMenuManager.Controls
{
    public sealed class LoadingDialog
    {
        private readonly Thread workThread;
        private readonly LoadingDialogInterface controller;
        internal readonly ContentDialog dialog;
        internal readonly WpfProgressBar progressBar;
        internal readonly ManualResetEventSlim readyEvent = new(false);

        private LoadingDialog(string title, Action<LoadingDialogInterface> action, MainWindow owner = null)
        {
            dialog = ContentDialogHost.CreateDialog(title, owner);
            dialog.DefaultButton = ContentDialogButton.None;
            progressBar = new WpfProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                MinWidth = 360,
                Height = 8,
                IsIndeterminate = true
            };
            dialog.Content = progressBar;
            dialog.Opened += (_, _) => readyEvent.Set();

            controller = new LoadingDialogInterface(this);
            workThread = new Thread(() => ExecuteAction(action))
            {
                Name = "LoadingDialogThread - " + title
            };
        }

        public Exception Error { get; private set; }

        public static Exception ShowDialog(string title, Action<LoadingDialogInterface> action, MainWindow owner = null)
        {
            var instance = new LoadingDialog(title, action, owner);
            return ContentDialogHost.RunBlocking(async dialogOwner =>
            {
                var showTask = instance.dialog.ShowAsync(dialogOwner);
                instance.workThread.Start();
                await showTask;
                return instance.Error;
            });
        }

        private void ExecuteAction(Action<LoadingDialogInterface> action)
        {
            controller.WaitTillDialogIsReady();
            try
            {
                action(controller);
            }
            catch (Exception ex)
            {
                Error = ex;
            }
            finally
            {
                controller.CloseDialog();
            }
        }
    }

    public sealed class LoadingDialogInterface
    {
        private readonly LoadingDialog dialog;

        internal LoadingDialogInterface(LoadingDialog dialog)
        {
            this.dialog = dialog;
        }

        public bool Abort { get; internal set; }

        public void CloseDialog()
        {
            dialog.dialog.Dispatcher.BeginInvoke(new Action(() => dialog.dialog.Hide()));
        }

        public void SetMaximum(int value)
        {
            dialog.progressBar.Dispatcher.Invoke(() =>
            {
                dialog.progressBar.IsIndeterminate = false;
                dialog.progressBar.Maximum = value;
            });
        }

        public void SetMinimum(int value)
        {
            dialog.progressBar.Dispatcher.Invoke(() =>
            {
                dialog.progressBar.IsIndeterminate = false;
                dialog.progressBar.Minimum = value;
            });
        }

        public void SetProgress(int value, string description = null, bool forceNoAnimation = false)
        {
            dialog.progressBar.Dispatcher.Invoke(() =>
            {
                dialog.progressBar.IsIndeterminate = false;
                if (value < dialog.progressBar.Minimum || value > dialog.progressBar.Maximum)
                {
                    dialog.progressBar.IsIndeterminate = true;
                    return;
                }

                dialog.progressBar.Value = value;
            });
        }

        public void SetTitle(string newTitle)
        {
            dialog.dialog.Dispatcher.Invoke(() => dialog.dialog.Title = newTitle);
        }

        internal void WaitTillDialogIsReady()
        {
            dialog.readyEvent.Wait();
        }
    }
}
