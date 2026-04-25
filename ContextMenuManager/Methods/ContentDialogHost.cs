using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace ContextMenuManager.Methods
{
    internal static class ContentDialogHost
    {
        public static ContentDialog CreateDialog(string title, MainWindow owner = null)
        {
            return new ContentDialog
            {
                Title = title,
                Owner = ResolveOwner(owner),
                DefaultButton = ContentDialogButton.Primary,
                PrimaryButtonText = AppString.Dialog.OK,
                CloseButtonText = AppString.Dialog.Cancel,
                IsSecondaryButtonEnabled = false
            };
        }

        public static T RunBlocking<T>(Func<Window, Task<T>> action, MainWindow owner = null)
        {
            var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            if (!dispatcher.CheckAccess())
            {
                return dispatcher.Invoke(() => RunBlocking(action, owner));
            }

            var ownerWindow = ResolveOwner(owner);

            // If the resolved owner window already has an open ContentDialog, create a
            // temporary transparent helper window to host the nested dialog, since
            // ContentDialog does not support multiple open dialogs on the same window.
            Window helperWindow = null;
            if (ownerWindow != null && ContentDialog.GetOpenDialog(ownerWindow) != null)
            {
                helperWindow = CreateHelperWindow(ownerWindow);
                ownerWindow = helperWindow;
            }

            Task<T> task;
            try
            {
                task = action(ownerWindow);
            }
            catch
            {
                helperWindow?.Close();
                throw;
            }

            if (task.IsCompleted)
            {
                try
                {
                    return task.GetAwaiter().GetResult();
                }
                finally
                {
                    helperWindow?.Close();
                }
            }

            Exception exception = null;
            T result = default;
            var frame = new DispatcherFrame();

            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    exception = t.Exception?.GetBaseException();
                }
                else if (t.IsCanceled)
                {
                    exception = new TaskCanceledException(t);
                }
                else
                {
                    result = t.Result;
                }

                helperWindow?.Close();
                frame.Continue = false;
            }, TaskScheduler.FromCurrentSynchronizationContext());

            Dispatcher.PushFrame(frame);

            if (exception != null)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }

            return result;
        }

        private static Window ResolveOwner(MainWindow owner)
        {
            var windows = Application.Current?.Windows.OfType<Window>().ToArray();
            if (windows == null || windows.Length == 0)
            {
                return null;
            }

            if (owner != null)
            {
                foreach (var window in windows)
                {
                    if (window == owner)
                    {
                        return window;
                    }
                }
            }

            return Application.Current?.MainWindow
                ?? windows.FirstOrDefault(w => w.IsActive)
                ?? windows.FirstOrDefault();
        }

        private static Window CreateHelperWindow(Window parentWindow)
        {
            var helperWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Width = parentWindow.ActualWidth,
                Height = parentWindow.ActualHeight,
                Left = parentWindow.Left,
                Top = parentWindow.Top,
                Owner = parentWindow
            };

            void SyncPosition(object s, EventArgs e)
            {
                helperWindow.Left = parentWindow.Left;
                helperWindow.Top = parentWindow.Top;
            }

            void SyncSize(object s, SizeChangedEventArgs e)
            {
                helperWindow.Width = parentWindow.ActualWidth;
                helperWindow.Height = parentWindow.ActualHeight;
            }

            void Unsubscribe(object s, EventArgs e)
            {
                parentWindow.LocationChanged -= SyncPosition;
                parentWindow.SizeChanged -= SyncSize;
                parentWindow.Closed -= Unsubscribe;
                helperWindow.Closed -= Unsubscribe;
            }

            parentWindow.LocationChanged += SyncPosition;
            parentWindow.SizeChanged += SyncSize;
            parentWindow.Closed += Unsubscribe;
            helperWindow.Closed += Unsubscribe;

            helperWindow.Show();
            return helperWindow;
        }
    }
}
