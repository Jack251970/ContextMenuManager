using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ContextMenuManager.Methods
{
    internal static class ContentDialogHost
    {
        private static readonly HashSet<ContentDialog> _hiddenForNesting = new();
        private static TaskCompletionSource<bool> _currentResumeSignal;

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

        public static ContentDialogResult ShowContentDialog(ContentDialog dialog, MainWindow owner = null)
        {
            return RunBlocking(async resolvedOwner =>
            {
                // Hide any currently open dialog for this owner window
                ContentDialog previousDialog = null;
                if (resolvedOwner != null)
                {
                    previousDialog = ContentDialog.GetOpenDialog(resolvedOwner);
                    if (previousDialog != null)
                    {
                        _hiddenForNesting.Add(previousDialog);
                        previousDialog.Hide();
                    }
                }

                // Save the parent's resume signal and create our own
                var parentResumeSignal = _currentResumeSignal;
                var myResumeSignal = new TaskCompletionSource<bool>();
                _currentResumeSignal = myResumeSignal;

                ContentDialogResult result;
                while (true)
                {
                    result = await dialog.ShowAsync(resolvedOwner);

                    if (!_hiddenForNesting.Remove(dialog))
                    {
                        break; // Normal close by user
                    }

                    // This dialog was hidden because a nested dialog opened.
                    // Wait for the nested dialog to complete before re-showing.
                    await myResumeSignal.Task;
                    myResumeSignal = new TaskCompletionSource<bool>();
                    _currentResumeSignal = myResumeSignal;
                }

                // Restore parent's resume signal and signal it to re-show
                _currentResumeSignal = parentResumeSignal;
                parentResumeSignal?.TrySetResult(true);

                return result;
            }, owner);
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
