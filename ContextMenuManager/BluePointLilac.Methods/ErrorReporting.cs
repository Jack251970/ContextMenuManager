using System;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace ContextMenuManager.Methods
{
    internal class ErrorReporting
    {
        private static void Report(Exception e, bool silent = false, [CallerMemberName] string methodName = "UnHandledException")
        {
            if (silent) return;

            // Workaround for issue https://github.com/Flow-Launcher/Flow.Launcher/issues/4016
            // The crash occurs in PresentationFramework.dll, not necessarily when the Runner UI is visible, originating from this line:
            // https://github.com/dotnet/wpf/blob/3439f20fb8c685af6d9247e8fd2978cac42e74ac/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Shell/WindowChromeWorker.cs#L1005
            // Many bug reports because users see the "Error report UI" after the crash with System.Runtime.InteropServices.COMException 0xD0000701 or 0x80263001.
            // However, displaying this "Error report UI" during WPF crashes, especially when DWM composition is changing, is not ideal; some users reported it hangs for up to a minute before the it appears.
            // This change modifies the behavior to log the exception instead of showing the "Error report UI".
            if (ExceptionHelper.IsRecoverableDwmCompositionException(e)) return;

            var reportWindow = new ReportWindow(e);
            reportWindow.Show();
        }

        public static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // handle non-ui thread exceptions
            Report((Exception)e.ExceptionObject);
        }

        public static void DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // handle ui thread exceptions
            Report(e.Exception);
            // prevent application exist, so the user can copy prompted error info
            e.Handled = true;
        }
    }
}
