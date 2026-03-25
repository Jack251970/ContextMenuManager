using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Windows;
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

            var task = action(ResolveOwner(owner));
            if (task.IsCompleted)
            {
                return task.GetAwaiter().GetResult();
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
    }
}
