using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;

namespace ContextMenuManager.Controls
{
    public static class DarkModeHelper
    {
        public static event EventHandler ThemeChanged;

        private static bool _isDarkTheme = false;
        public static bool IsDarkTheme => _isDarkTheme;
        private static bool _listeningForThemeChanges = false;

        public static void Initialize()
        {
            UpdateTheme();
            StartListeningForThemeChanges();
        }

        private static void StartListeningForThemeChanges()
        {
            if (!_listeningForThemeChanges)
            {
                SystemEvents.UserPreferenceChanged += OnSystemPreferencesChanged;
                _listeningForThemeChanges = true;
            }
        }

        public static void StopListening()
        {
            if (_listeningForThemeChanges)
            {
                SystemEvents.UserPreferenceChanged -= OnSystemPreferencesChanged;
                _listeningForThemeChanges = false;
            }
        }

        public static bool IsDarkThemeEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                return value != null && (int)value == 0;
            }
            catch
            {
                try
                {
                    return CheckSystemDarkModeStatus();
                }
                catch
                {
                    return false;
                }
            }
        }

        public static bool UpdateTheme()
        {
            var newDarkTheme = IsDarkThemeEnabled();
            var changed = _isDarkTheme != newDarkTheme;
            _isDarkTheme = newDarkTheme;

            if (changed)
            {
                try
                {
                    ThemeChanged?.Invoke(null, EventArgs.Empty);
                }
                catch
                {
                    // Ignored
                }
            }

            EnableWin32DarkMode(IsDarkTheme);

            return changed;
        }

        private static void OnSystemPreferencesChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                UpdateTheme();
            }
        }

        [DllImport("UXTheme.dll", SetLastError = true, EntryPoint = "#138")]
        public static extern bool CheckSystemDarkModeStatus();

        [DllImport("uxtheme.dll", EntryPoint = "#135", SetLastError = true)]
        private static extern int SetPreferredAppMode(int appMode);

        public static void EnableWin32DarkMode(bool darkTheme)
        {
            try
            {
                // Undocumented API from Windows 10 1809
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                    Environment.OSVersion.Version.Build >= 17763)
                {
                    _ = SetPreferredAppMode(darkTheme ? 2 : 3);
                }

            }
            catch
            {
                // Ignore errors on unsupported OS
            }
        }
    }
}
