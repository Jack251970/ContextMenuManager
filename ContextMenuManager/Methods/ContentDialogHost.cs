using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace ContextMenuManager.Methods
{
    internal static class ContentDialogHost
    {
        public static ContentDialog CreateDialog(string title, IntPtr hwndOwner = default)
        {
            return new ContentDialog
            {
                Title = title,
                Owner = ResolveOwner(hwndOwner),
                DefaultButton = ContentDialogButton.Primary
            };
        }

        public static T RunBlocking<T>(Func<Window, Task<T>> action, IntPtr hwndOwner = default)
        {
            var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            if (!dispatcher.CheckAccess())
            {
                return dispatcher.Invoke(() => RunBlocking(action, hwndOwner));
            }

            var task = action(ResolveOwner(hwndOwner));
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

        private static Window ResolveOwner(IntPtr hwndOwner)
        {
            var windows = Application.Current?.Windows.OfType<Window>().ToArray();
            if (windows == null || windows.Length == 0)
            {
                return null;
            }

            if (hwndOwner != IntPtr.Zero)
            {
                foreach (var window in windows)
                {
                    if (new WindowInteropHelper(window).Handle == hwndOwner)
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
