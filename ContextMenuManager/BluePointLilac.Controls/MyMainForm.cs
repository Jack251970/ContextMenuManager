using BluePointLilac.Methods;
using Microsoft.Win32;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public class MyMainForm : Form
    {
        // <summary>程序主题色</summary>
        public static Color MainColor = Color.FromArgb(255, 143, 31);

        public MyMainForm()
        {
            SuspendLayout();
            Text = Application.ProductName;
            ForeColor = FormFore;
            BackColor = FormBack;
            StartPosition = FormStartPosition.CenterScreen;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            Controls.AddRange(new Control[] { MainBody, SideBar, StatusBar, ToolBar });
            SideBar.Resize += (sender, e) => OnResize(null);
            ClientSize = new Size(850, 610).DpiZoom();
            MinimumSize = Size;
            MainBody.Dock = DockStyle.Left;
            StatusBar.CanMoveForm();
            ToolBar.CanMoveForm();
            ResumeLayout();
        }

        public readonly MyToolBar ToolBar = new MyToolBar();
        public readonly MySideBar SideBar = new MySideBar();
        public readonly MyStatusBar StatusBar = new MyStatusBar();
        public readonly MyListBox MainBody = new MyListBox();

        /// <summary>窗体移动时是否临时挂起MainBody</summary>
        public bool SuspendMainBodyWhenMove { get; set; } = false;

        /// <summary>窗体调整大小时是否临时挂起MainBody</summary>
        public bool SuspendMainBodyWhenResize { get; set; } = true;

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            MainBody.Width = ClientSize.Width - SideBar.Width;
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCLBUTTONDBLCLK = 0x00A3;
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_MAXIMIZE = 0xF030;
            const int SC_MINIMIZE = 0xF020;
            const int SC_RESTORE = 0xF120;
            const int SC_MOVE = 0xF012;
            const int SC_SIZE = 0xF000;
            const int HT_CAPTION = 0x2;
            bool suspend = false;//临时挂起MainBody
            switch (m.Msg)
            {
                case WM_SYSCOMMAND:
                    switch (m.WParam.ToInt32())
                    {
                        //解决控件过多移动窗体时延迟问题
                        case SC_MOVE:
                        //解决控件过多调整窗体大小时延迟问题
                        case SC_SIZE:
                            suspend = SuspendMainBodyWhenMove; break;
                        //解决控件过多最大化、最小化、还原重绘卡顿问题
                        case SC_RESTORE:
                        case SC_MINIMIZE:
                        case SC_MAXIMIZE:
                            suspend = SuspendMainBodyWhenResize; break;
                    }
                    break;
                case WM_NCLBUTTONDBLCLK:
                    switch (m.WParam.ToInt32())
                    {
                        //双击标题栏最大化和还原窗口
                        case HT_CAPTION:
                            suspend = SuspendMainBodyWhenResize; break;
                    }
                    break;
            }
            if (suspend)
            {
                SuspendLayout();
                MainBody.SuspendLayout();
                Controls.Remove(MainBody);
                base.WndProc(ref m);
                Controls.Add(MainBody);
                MainBody.BringToFront();
                MainBody.ResumeLayout();
                ResumeLayout();
            }
            else base.WndProc(ref m);
        }

        #region Dark Theme

        /*
         * Edited from: https://github.com/seerge/g-helper
         */

        public static Color ButtonMain;
        public static Color ButtonSecond;
        public static Color titleArea;

        public static Color FormBack;
        public static Color FormFore;
        public static Color FormBorder;

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

        public static void InitColors()
        {
            InitColors(IsDarkTheme());
        }

        private static void InitColors(bool darkTheme)
        {
            if (darkTheme)
            {
                titleArea = Color.FromArgb(255, 32, 32, 32);

                ButtonMain = Color.FromArgb(255, 55, 55, 55);
                ButtonSecond = Color.FromArgb(255, 38, 38, 38);

                FormBack = Color.FromArgb(255, 28, 28, 28);
                FormFore = Color.FromArgb(255, 240, 240, 240);
                FormBorder = Color.FromArgb(255, 50, 50, 50);
            }
            else
            {
                titleArea = Color.FromArgb(255, 243, 243, 243);

                ButtonMain = SystemColors.ControlLightLight;
                ButtonSecond = SystemColors.ControlLight;

                FormBack = SystemColors.Control;
                FormFore = SystemColors.ControlText;
                FormBorder = Color.LightGray;
            }
        }

        public static bool IsDarkTheme()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                var registryValueObject = key?.GetValue("AppsUseLightTheme");

                if (registryValueObject == null) return false;
                return (int)registryValueObject <= 0;
            }
        }

        public bool InitTheme(bool firstTime = false)
        {
            bool newDarkTheme = IsDarkTheme();
            bool changed = darkTheme != newDarkTheme;
            darkTheme = newDarkTheme;

            InitColors(darkTheme);

            if (firstTime || changed)
            {
                DwmSetWindowAttribute(Handle, 20, new[] { darkTheme ? 1 : 0 }, 4);
                Adjust();
                Invalidate();
            }

            return changed;
        }

        private void Adjust()
        {
            BackColor = FormBack;
            ForeColor = FormFore;

            AdjustControls(Controls);
        }

        private void AdjustControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                AdjustControls(control.Controls);

                if (control is MyListBox listBox)
                {
                    listBox.BackColor = FormBack;
                    listBox.ForeColor = FormFore;
                }

                if (control is MyListItem listItem)
                {
                    listItem.BackColor = FormBack;
                    listItem.ForeColor = FormFore;
                }

                if (control is MyToolBar toolBar)
                {
                    toolBar.BackColor = titleArea;
                    toolBar.ForeColor = FormFore;
                }

                if (control is MyToolBarButton toolBarButton)
                {
                    toolBarButton.ForeColor = FormFore;
                }

                if (control is MySideBar sideBar)
                {
                    sideBar.BackColor = ButtonSecond;// More darker than buttonMain
                    sideBar.ForeColor = FormFore;
                }

                if (control is MyStatusBar statusBar)
                {
                    statusBar.BackColor = ButtonMain;
                    statusBar.ForeColor = FormFore;
                }

                if (control is RComboBox combo)
                {
                    combo.BackColor = ButtonMain;
                    combo.ForeColor = FormFore;
                    combo.BorderColor = ButtonMain;
                    combo.ButtonColor = ButtonMain;
                    combo.ArrowColor = FormFore;
                }
            }
        }

        #endregion
    }
}
