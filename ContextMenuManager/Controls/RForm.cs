using System;
using System.Drawing;
using System.Runtime.InteropServices;
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
            // 初始化主题
            InitTheme();
        }

        public static Color ButtonMain => DarkModeHelper.ButtonMain;
        public static Color ButtonSecond => DarkModeHelper.ButtonSecond;

        public static Color FormBack => DarkModeHelper.FormBack;
        public static Color FormFore => DarkModeHelper.FormFore;
        public static Color FormBorder => DarkModeHelper.FormBorder;

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
            var newDarkTheme = DarkModeHelper.IsDarkTheme;
            var changed = darkTheme != newDarkTheme;
            darkTheme = newDarkTheme;

            if (changed && IsHandleCreated)
            {
                try
                {
                    DwmSetWindowAttribute(Handle, 20, new[] { darkTheme ? 1 : 0 }, 4);
                }
                catch
                {
                    // API调用失败，忽略错误
                }

                // 在UI线程上调整颜色
                if (InvokeRequired)
                {
                    Invoke(new Action(Adjust));
                }
                else
                {
                    Adjust();
                }
                Invalidate();
            }

            return changed;
        }

        protected void ApplyDarkModeToDataGridView(DataGridView dgv)
        {
            // 确保在UI线程上执行
            if (dgv.InvokeRequired)
            {
                dgv.Invoke(new Action(() => ApplyDarkModeToDataGridView(dgv)));
                return;
            }

            // Background color
            dgv.BackgroundColor = DarkModeHelper.FormBack;
            dgv.DefaultCellStyle.BackColor = DarkModeHelper.FormBack;
            dgv.DefaultCellStyle.ForeColor = DarkModeHelper.FormFore;

            // Grid color
            dgv.GridColor = Color.DimGray;

            // Header style
            dgv.ColumnHeadersDefaultCellStyle.BackColor = DarkModeHelper.FormBack;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = DarkModeHelper.FormFore;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = DarkModeHelper.MainColor;
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = DarkModeHelper.FormFore;
            dgv.EnableHeadersVisualStyles = false;  // Ensure custom header styles apply

            // Row styles
            dgv.RowsDefaultCellStyle.BackColor = DarkModeHelper.FormBack;
            dgv.RowsDefaultCellStyle.ForeColor = DarkModeHelper.FormFore;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = DarkModeHelper.FormBack;

            // Selection color
            dgv.DefaultCellStyle.SelectionBackColor = DarkModeHelper.MainColor;
            dgv.DefaultCellStyle.SelectionForeColor = DarkModeHelper.FormFore;
        }

        private void Adjust()
        {
            BackColor = FormBack;
            ForeColor = FormFore;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // 应用深色模式到窗体
            DarkModeHelper.ApplyDarkModeToForm(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // 清理资源
            }
            base.Dispose(disposing);
        }
    }
}