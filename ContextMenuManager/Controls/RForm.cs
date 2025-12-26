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
        protected override void OnLoad(EventArgs e)
        {
            StartPosition = FormStartPosition.CenterScreen;
            base.OnLoad(e);
        }

        public static Color ButtonMain => MyMainForm.ButtonMain;
        public static Color ButtonSecond => MyMainForm.ButtonSecond;

        public static Color FormBack => MyMainForm.FormBack;
        public static Color FormFore => MyMainForm.FormFore;
        public static Color FormBorder => MyMainForm.FormBorder;

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

        public bool InitTheme()
        {
            bool newDarkTheme = MyMainForm.IsDarkTheme();
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

        protected void ApplyDarkModeToDataGridView(DataGridView dgv)
        {
            // Background color
            dgv.BackgroundColor = MyMainForm.FormBack;
            dgv.DefaultCellStyle.BackColor = MyMainForm.FormBack;
            dgv.DefaultCellStyle.ForeColor = MyMainForm.FormFore;

            // Grid color
            dgv.GridColor = Color.DimGray;

            // Header style
            dgv.ColumnHeadersDefaultCellStyle.BackColor = MyMainForm.FormBack;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = MyMainForm.FormFore;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = MyMainForm.MainColor;
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = MyMainForm.FormFore;
            dgv.EnableHeadersVisualStyles = false;  // Ensure custom header styles apply

            // Row styles
            dgv.RowsDefaultCellStyle.BackColor = MyMainForm.FormBack;
            dgv.RowsDefaultCellStyle.ForeColor = MyMainForm.FormFore;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = MyMainForm.FormBack;

            // Selection color
            dgv.DefaultCellStyle.SelectionBackColor = MyMainForm.MainColor;
            dgv.DefaultCellStyle.SelectionForeColor = MyMainForm.FormFore;
        }

        private void Adjust()
        {
            BackColor = FormBack;
            ForeColor = FormFore;
        }
    }
}
