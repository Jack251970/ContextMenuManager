using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern;
using iNKORE.UI.WPF.Modern.Common;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows;

namespace ContextMenuManager
{
    public partial class App : Application, IDisposable, ISingleInstanceApp
    {
        private static bool _disposed;

        // To prevent two disposals running at the same time.
        private static readonly Lock _disposingLock = new();

        public App()
        {
            InitializeComponent();
            ShadowAssist.UseBitmapCache = false;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Current.ShutdownMode = ShutdownMode.OnMainWindowClose;

            // 初始化主题管理器，根据系统主题设置应用主题
            var isDarkMode = IsSystemDarkModeEnabled();
            ThemeManager.Current.ApplicationTheme = isDarkMode ? ApplicationTheme.Dark : ApplicationTheme.Light;

            RegisterAppDomainExceptions();
            RegisterDispatcherUnhandledException();

            Current.MainWindow = new MainWindow();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            RegisterExitEvents();

            Current.MainWindow.Show();

            Updater.PeriodicUpdate();
        }

        private void RegisterExitEvents()
        {
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                Dispose();
            };

            Current.Exit += (s, e) =>
            {
                Dispose();
            };

            Current.SessionEnding += (s, e) =>
            {
                Dispose();
            };
        }

        /// <summary>
        /// Let exception throw as normal is better for Debug
        /// </summary>
        [Conditional("RELEASE")]
        private void RegisterDispatcherUnhandledException()
        {
            DispatcherUnhandledException += ErrorReporting.DispatcherUnhandledException;
        }

        /// <summary>
        /// Let exception throw as normal is better for Debug
        /// </summary>
        [Conditional("RELEASE")]
        private static void RegisterAppDomainExceptions()
        {
            AppDomain.CurrentDomain.UnhandledException += ErrorReporting.UnhandledException;
        }

        protected virtual void Dispose(bool disposing)
        {
            // Prevent two disposes at the same time.
            lock (_disposingLock)
            {
                if (!disposing)
                {
                    return;
                }

                if (_disposed)
                {
                    return;
                }

                _disposed = true;
            }

            AppConfig.CleanDirectory();
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void OnSecondAppStarted()
        {
            Current?.MainWindow.Show();
            Current?.MainWindow.Focus();
        }

        /// <summary>
        /// 检测系统是否启用深色模式
        /// </summary>
        private static bool IsSystemDarkModeEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key != null)
                {
                    var value = key.GetValue("AppsUseLightTheme");
                    if (value is int intValue)
                    {
                        return intValue == 0; // 0 = Dark mode, 1 = Light mode
                    }
                }
            }
            catch
            {
                // 如果读取注册表失败，默认使用浅色模式
            }
            return false; // 默认浅色模式
        }
    }
}
