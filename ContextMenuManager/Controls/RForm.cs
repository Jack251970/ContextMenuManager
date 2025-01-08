using Microsoft.Win32;
using System.Drawing;
using System.Runtime.InteropServices;
using System;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    /// <summary>
    /// Edited from: https://github.com/seerge/g-helper
    /// </summary>
    public class RForm : Form
    {
        public static Color buttonMain => MyMainForm.buttonMain;
        public static Color buttonSecond => MyMainForm.buttonSecond;

        public static Color formBack => MyMainForm.formBack;
        public static Color foreMain => MyMainForm.foreMain;
        public static Color borderMain => MyMainForm.borderMain;

        [DllImport("UXTheme.dll", SetLastError = true, EntryPoint = "#138")]
        public static extern bool CheckSystemDarkModeStatus();

        [DllImport("DwmApi")] //System.Runtime.InteropServices
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, int[] attrValue, int attrSize);

        public bool darkTheme = false;
        protected override CreateParams CreateParams
        {
            get
            {
                var parms = base.CreateParams;
                parms.Style &= ~0x02000000;  // Turn off WS_CLIPCHILDREN
                parms.ClassStyle &= ~0x00020000;
                return parms;
            }
        }

        private static bool IsDarkTheme()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                var registryValueObject = key?.GetValue("AppsUseLightTheme");

                if (registryValueObject == null) return false;
                return (int)registryValueObject <= 0;
            }
        }

        public bool InitTheme()
        {
            bool newDarkTheme = IsDarkTheme();
            bool changed = darkTheme != newDarkTheme;
            darkTheme = newDarkTheme;

            if (changed)
            {
                DwmSetWindowAttribute(Handle, 20, new[] { darkTheme ? 1 : 0 }, 4);
                Adjust();
                Invalidate();
            }

            return changed;
        }

        private void Adjust()
        {
            BackColor = formBack;
            ForeColor = foreMain;
        }
    }
}
