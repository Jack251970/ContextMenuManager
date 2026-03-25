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

        private LoadingDialog(string title, Func<LoadingDialogInterface, Task> action, MainWindow owner = null)
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
            workThread = new Thread(() =>
            {
                ExecuteActionAsync(action).GetAwaiter().GetResult();
            })
            {
                Name = "LoadingDialogThread - " + title,
                IsBackground = false
            };
        }

        public static bool ShowDialog(string title, Func<LoadingDialogInterface, Task> action, MainWindow owner = null)
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

        private async Task ExecuteActionAsync(Func<LoadingDialogInterface, Task> action)
        {
            controller.WaitTillDialogIsReady();
            try
            {
                await action(controller);
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

        public bool IsCancelled { get; internal set; }

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

        public void SetProgress(int value, string description = "...")
        {
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
            dialog.dialog.Dispatcher.Invoke(() => dialog.dialog.Title = newTitle);
        }

        internal void WaitTillDialogIsReady()
        {
            dialog.readyEvent.Wait();
        }
    }
}
