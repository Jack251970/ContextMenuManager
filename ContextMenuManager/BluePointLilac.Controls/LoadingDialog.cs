using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WpfProgressBar = System.Windows.Controls.ProgressBar;

namespace ContextMenuManager.Controls
{
    public sealed class LoadingDialog
    {
        private readonly Thread workThread;
        private readonly LoadingDialogInterface controller;
        internal readonly ContentDialog dialog;
        internal readonly WpfProgressBar progressBar;
        internal readonly TextBlock descriptionText;
        internal readonly ManualResetEventSlim readyEvent = new(false);

        public bool IsCancelled => controller.IsCancelled;

        private LoadingDialog(string title, Action<LoadingDialogInterface> action, MainWindow owner = null)
        {
            dialog = ContentDialogHost.CreateDialog(title, owner);
            dialog.IsPrimaryButtonEnabled = false;
            dialog.DefaultButton = ContentDialogButton.None;

            progressBar = new WpfProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                MinWidth = 360,
                Height = 8,
                IsIndeterminate = true
            };

            descriptionText = new TextBlock
            {
                Text = "...",
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            var panel = new StackPanel { Orientation = Orientation.Vertical };
            panel.Children.Add(descriptionText);
            panel.Children.Add(progressBar);

            dialog.Content = panel;
            dialog.Opened += (_, _) => readyEvent.Set();
            dialog.CloseButtonClick += (_, _) =>
            {
                controller.IsCancelled = true;
            };

            controller = new LoadingDialogInterface(this);
            workThread = new Thread(() => ExecuteAction(action))
            {
                Name = "LoadingDialogThread - " + title,
                IsBackground = true
            };
        }

        public static bool ShowDialog(string title, Action<LoadingDialogInterface> action, MainWindow owner = null)
        {
            var instance = new LoadingDialog(title, action, owner);
            return ContentDialogHost.RunBlocking(async dialogOwner =>
            {
                var showTask = instance.dialog.ShowAsync(dialogOwner);
                instance.workThread.Start();
                await showTask;
                return !instance.IsCancelled;
            });
        }

        private void ExecuteAction(Action<LoadingDialogInterface> action)
        {
            controller.WaitTillDialogIsReady();
            try
            {
                action(controller);
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
        private readonly bool silent;

        internal LoadingDialogInterface(LoadingDialog dialog)
        {
            this.dialog = dialog;
            this.silent = false;
        }

        /// <summary>Creates a silent (no-UI) progress reporter for headless/background operations.</summary>
        internal LoadingDialogInterface()
        {
            this.silent = true;
        }

        public bool IsCancelled { get; internal set; }

        public void CloseDialog()
        {
            if (silent) return;
            dialog.dialog.Dispatcher.BeginInvoke(new Action(() => dialog.dialog.Hide()));
        }

        public void SetMaximum(int value)
        {
            if (silent) return;
            dialog.progressBar.Dispatcher.Invoke(() =>
            {
                dialog.progressBar.IsIndeterminate = false;
                dialog.progressBar.Maximum = value;
            });
        }

        public void SetMinimum(int value)
        {
            if (silent) return;
            dialog.progressBar.Dispatcher.Invoke(() =>
            {
                dialog.progressBar.IsIndeterminate = false;
                dialog.progressBar.Minimum = value;
            });
        }

        public void SetProgress(int value, string description = "...")
        {
            if (silent) return;
            try
            {
                dialog.progressBar.Dispatcher.Invoke(() =>
                {
                    dialog.progressBar.IsIndeterminate = false;
                    if (value < dialog.progressBar.Minimum || value > dialog.progressBar.Maximum)
                    {
                        dialog.progressBar.IsIndeterminate = true;
                    }
                    else
                    {
                        dialog.progressBar.Value = value;
                    }

                    description = string.IsNullOrEmpty(description) ? "..." : description;
                    dialog.descriptionText.Text = description;
                });
            }
            catch (TaskCanceledException)
            {
                if (!IsCancelled)
                {
                    throw;
                }
            }
        }

        public void SetTitle(string newTitle)
        {
            if (silent) return;
            dialog.dialog.Dispatcher.Invoke(() => dialog.dialog.Title = newTitle);
        }

        internal void WaitTillDialogIsReady()
        {
            if (silent) return;
            dialog.readyEvent.Wait();
        }
    }
}
