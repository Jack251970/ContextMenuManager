using ContextMenuManager.BluePointLilac.Controls;
using Microsoft.Win32;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public static class DarkModeHelper
    {
        [DllImport("UXTheme.dll", SetLastError = true, EntryPoint = "#138")]
        public static extern bool CheckSystemDarkModeStatus();

        [DllImport("DwmApi")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, int[] attrValue, int attrSize);

        public static event EventHandler ThemeChanged;
        private static SynchronizationContext uiContext;
        public static Color MainColor = Color.FromArgb(255, 143, 31);

        // 颜色属性
        public static Color TitleArea { get; private set; }
        public static Color FormBack { get; private set; }
        public static Color FormFore { get; private set; }
        public static Color FormBorder { get; private set; }
        public static Color ButtonMain { get; private set; }
        public static Color ButtonSecond { get; private set; }
        public static Color SideBarBackground { get; private set; }
        public static Color SideBarSeparator { get; private set; }
        public static Color SideBarHovered { get; private set; }
        public static Color ToolBarGradientTop { get; private set; }
        public static Color ToolBarGradientMiddle { get; private set; }
        public static Color ToolBarGradientBottom { get; private set; }
        public static Color StatusBarGradientTop { get; private set; }
        public static Color StatusBarGradientMiddle { get; private set; }
        public static Color StatusBarGradientBottom { get; private set; }
        public static Color SearchBoxBack { get; private set; }
        public static Color SearchBoxBorder { get; private set; }
        public static Color SearchBoxPlaceholder { get; private set; }
        public static Color ComboBoxBack { get; private set; }
        public static Color ComboBoxFore { get; private set; }
        public static Color ComboBoxBorder { get; private set; }
        public static Color ComboBoxArrow { get; private set; }
        public static Color ComboBoxHover { get; private set; }
        public static Color ComboBoxSelected { get; private set; }

        private static bool _isDarkTheme = false;
        public static bool IsDarkTheme => _isDarkTheme;
        private static bool _listeningForThemeChanges = false;

        public static void Initialize()
        {
            uiContext = SynchronizationContext.Current;
            if (uiContext == null && Application.MessageLoop)
            {
                uiContext = new WindowsFormsSynchronizationContext();
            }

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
                try { return CheckSystemDarkModeStatus(); }
                catch { return false; }
            }
        }

        public static bool UpdateTheme()
        {
            var newDarkTheme = IsDarkThemeEnabled();
            var changed = _isDarkTheme != newDarkTheme;
            _isDarkTheme = newDarkTheme;

            UpdateAllColors(_isDarkTheme);

            if (changed)
            {
                if (uiContext != null)
                {
                    uiContext.Post(_ => SafeInvokeThemeChanged(), null);
                }
                else
                {
                    SafeInvokeThemeChanged();
                }
            }

            return changed;
        }

        public static void ApplyDarkModeToForm(Form form)
        {
            if (_isDarkTheme && form.IsHandleCreated)
            {
                try { DwmSetWindowAttribute(form.Handle, 20, new[] { 1 }, 4); }
                catch { /* 忽略API错误 */ }
            }
        }

        private static void UpdateAllColors(bool isDark)
        {
            if (isDark) SetDarkModeColors();
            else SetLightModeColors();
        }

        private static void SetDarkModeColors()
        {
            TitleArea = Color.FromArgb(255, 32, 32, 32);
            FormBack = Color.FromArgb(255, 28, 28, 28);
            FormFore = Color.FromArgb(255, 240, 240, 240);
            FormBorder = Color.FromArgb(255, 50, 50, 50);
            ButtonMain = Color.FromArgb(255, 55, 55, 55);
            ButtonSecond = Color.FromArgb(255, 38, 38, 38);
            SideBarBackground = Color.FromArgb(255, 26, 26, 26);
            SideBarSeparator = Color.FromArgb(255, 64, 64, 64);
            SideBarHovered = Color.FromArgb(255, 51, 51, 51);
            ToolBarGradientTop = Color.FromArgb(255, 128, 128, 128);
            ToolBarGradientMiddle = Color.FromArgb(255, 56, 56, 56);
            ToolBarGradientBottom = Color.FromArgb(255, 128, 128, 128);
            StatusBarGradientTop = Color.FromArgb(255, 128, 128, 128);
            StatusBarGradientMiddle = Color.FromArgb(255, 56, 56, 56);
            StatusBarGradientBottom = Color.FromArgb(255, 128, 128, 128);
            SearchBoxBack = Color.FromArgb(255, 45, 45, 45);
            SearchBoxBorder = Color.FromArgb(255, 80, 80, 80);
            SearchBoxPlaceholder = Color.FromArgb(255, 150, 150, 150);
            ComboBoxBack = Color.FromArgb(255, 45, 45, 48);
            ComboBoxFore = Color.FromArgb(255, 245, 245, 245);
            ComboBoxBorder = Color.FromArgb(255, 70, 70, 75);
            ComboBoxArrow = Color.FromArgb(255, 200, 200, 200);
            ComboBoxHover = SideBarHovered;
            ComboBoxSelected = MainColor;
        }

        private static void SetLightModeColors()
        {
            TitleArea = Color.FromArgb(255, 243, 243, 243);
            FormBack = SystemColors.Control;
            FormFore = SystemColors.ControlText;
            FormBorder = Color.LightGray;
            ButtonMain = SystemColors.ControlLightLight;
            ButtonSecond = SystemColors.ControlLight;
            SideBarBackground = SystemColors.Control;
            SideBarSeparator = Color.FromArgb(255, 200, 200, 200);
            SideBarHovered = Color.FromArgb(255, 230, 230, 230);
            ToolBarGradientTop = Color.FromArgb(255, 255, 255, 255);
            ToolBarGradientMiddle = Color.FromArgb(255, 230, 230, 230);
            ToolBarGradientBottom = Color.FromArgb(255, 255, 255, 255);
            StatusBarGradientTop = Color.FromArgb(255, 255, 255, 255);
            StatusBarGradientMiddle = Color.FromArgb(255, 230, 230, 230);
            StatusBarGradientBottom = Color.FromArgb(255, 255, 255, 255);
            SearchBoxBack = Color.White;
            SearchBoxBorder = Color.FromArgb(255, 200, 200, 200);
            SearchBoxPlaceholder = Color.FromArgb(255, 120, 120, 120);
            ComboBoxBack = Color.FromArgb(255, 250, 250, 252);
            ComboBoxFore = Color.FromArgb(255, 25, 25, 25);
            ComboBoxBorder = Color.FromArgb(255, 210, 210, 215);
            ComboBoxArrow = Color.FromArgb(255, 100, 100, 100);
            ComboBoxHover = SideBarHovered;
            ComboBoxSelected = MainColor;
        }

        public static Color GetBorderColor(bool isFocused = false)
        {
            return isFocused ? MainColor : (IsDarkTheme ?
                Color.FromArgb(255, 80, 80, 80) :
                Color.FromArgb(255, 200, 200, 200));
        }

        public static Color GetPlaceholderColor()
        {
            return IsDarkTheme ?
                Color.FromArgb(255, 150, 150, 150) :
                Color.FromArgb(255, 120, 120, 120);
        }

        private static void OnSystemPreferencesChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                if (uiContext != null)
                {
                    uiContext.Post(_ => UpdateTheme(), null);
                }
                else
                {
                    UpdateTheme();
                }
            }
        }

        public static void AdjustControlColors(Control control)
        {
            if (control == null || control.IsDisposed) return;

            if (control.InvokeRequired)
            {
                try { control.Invoke(new Action(() => AdjustControlColors(control))); }
                catch { return; }
                return;
            }

            if (control.IsDisposed) return;

            foreach (Control child in control.Controls)
                AdjustControlColors(child);

            try
            {
                var typeName = control.GetType().FullName;

                if (typeName is "BluePointLilac.Controls.MyListBox" or
                    "BluePointLilac.Controls.MyListItem")
                {
                    control.BackColor = FormBack;
                    control.ForeColor = FormFore;
                }
                else if (typeName == "BluePointLilac.Controls.MyToolBar")
                {
                    control.BackColor = TitleArea;
                    control.ForeColor = FormFore;
                }
                else if (typeName == "BluePointLilac.Controls.MyToolBarButton")
                {
                    control.ForeColor = FormFore;
                }
                else if (typeName == "BluePointLilac.Controls.MySideBar")
                {
                    control.BackColor = ButtonSecond;
                    control.ForeColor = FormFore;
                }
                else if (typeName == "BluePointLilac.Controls.MyStatusBar")
                {
                    control.BackColor = ButtonMain;
                    control.ForeColor = FormFore;
                }
                else if (control is RComboBox combo)
                {
                    if (combo.InvokeRequired)
                        combo.Invoke(new Action(() => combo.UpdateColors()));
                    else
                        combo.UpdateColors();
                }
                else if (control is SearchBox searchBox)
                {
                    searchBox.Invoke(new Action(() =>
                    {
                        var method = searchBox.GetType().GetMethod("UpdateThemeColors");
                        method?.Invoke(searchBox, null);
                    }));
                }
            }
            catch { /* 忽略错误 */ }
        }

        public static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            var diameter = radius * 2;
            var arc = new Rectangle(rect.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        private static void SafeInvokeThemeChanged()
        {
            try { ThemeChanged?.Invoke(null, EventArgs.Empty); }
            catch { /* 忽略异常 */ }
        }
    }
}