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
        public MyMainForm()
        {
            SuspendLayout();
            Text = Application.ProductName;
            ForeColor = Color.FromArgb(80, 80, 80);
            BackColor = Color.FromArgb(250, 250, 250);
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
            switch(m.Msg)
            {
                case WM_SYSCOMMAND:
                    switch(m.WParam.ToInt32())
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
                    switch(m.WParam.ToInt32())
                    {
                        //双击标题栏最大化和还原窗口
                        case HT_CAPTION:
                            suspend = SuspendMainBodyWhenResize; break;
                    }
                    break;
            }
            if(suspend)
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
         * Picked from: https://github.com/seerge/g-helper
         */

        public static Color buttonMain;
        public static Color buttonSecond;

        public static Color formBack;
        public static Color foreMain;
        public static Color borderMain;
        public static Color chartMain;
        public static Color chartGrid;

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

        private static void InitColors(bool darkTheme)
        {
            if (darkTheme)
            {
                buttonMain = Color.FromArgb(255, 55, 55, 55);
                buttonSecond = Color.FromArgb(255, 38, 38, 38);

                formBack = Color.FromArgb(255, 28, 28, 28);
                foreMain = Color.FromArgb(255, 240, 240, 240);
                borderMain = Color.FromArgb(255, 50, 50, 50);

                chartMain = Color.FromArgb(255, 35, 35, 35);
                chartGrid = Color.FromArgb(255, 70, 70, 70);
            }
            else
            {
                buttonMain = SystemColors.ControlLightLight;
                buttonSecond = SystemColors.ControlLight;

                formBack = SystemColors.Control;
                foreMain = SystemColors.ControlText;
                borderMain = Color.LightGray;

                chartMain = SystemColors.ControlLightLight;
                chartGrid = Color.LightGray;
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

            InitColors(darkTheme);

            if (changed)
            {
                DwmSetWindowAttribute(Handle, 20, new[] { darkTheme ? 1 : 0 }, 4);
                Adjust(changed);
                Invalidate();
            }

            return changed;
        }

        private bool _invert = false;

        private void Adjust(bool invert = false)
        {
            BackColor = formBack;
            ForeColor = foreMain;

            _invert = invert;
            AdjustControls(Controls);
            _invert = false;
        }

        private static void AdjustControls(Control.ControlCollection controls)
        {
            /*foreach (Control control in controls)
            {
                AdjustControls(control.Controls);

                var button = control as RButton;
                if (button != null)
                {
                    button.BackColor = button.Secondary ? RForm.buttonSecond : RForm.buttonMain;
                    button.ForeColor = RForm.foreMain;

                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = RForm.borderMain;

                    if (button.Image is not null)
                        button.Image = AdjustImage(button.Image);
                }

                var pictureBox = control as PictureBox;
                if (pictureBox != null && pictureBox.BackgroundImage is not null)
                    pictureBox.BackgroundImage = AdjustImage(pictureBox.BackgroundImage);


                var combo = control as RComboBox;
                if (combo != null)
                {
                    combo.BackColor = RForm.buttonMain;
                    combo.ForeColor = RForm.foreMain;
                    combo.BorderColor = RForm.buttonMain;
                    combo.ButtonColor = RForm.buttonMain;
                    combo.ArrowColor = RForm.foreMain;
                }
                var numbericUpDown = control as NumericUpDown;
                if (numbericUpDown is not null)
                {
                    numbericUpDown.ForeColor = RForm.foreMain;
                    numbericUpDown.BackColor = RForm.buttonMain;
                }

                var gb = control as GroupBox;
                if (gb != null)
                {
                    gb.ForeColor = RForm.foreMain;
                }

                var pn = control as Panel;
                if (pn != null && pn.Name.Contains("Header"))
                {
                    pn.BackColor = RForm.buttonSecond;
                }

                var sl = control as Slider;
                if (sl != null)
                {
                    sl.borderColor = RForm.buttonMain;
                }

                var chk = control as CheckBox;
                if (chk != null && chk.BackColor != RForm.formBack)
                {
                    chk.BackColor = RForm.buttonSecond;
                }

                var chart = control as Chart;
                if (chart != null)
                {
                    chart.BackColor = RForm.chartMain;
                    chart.ChartAreas[0].BackColor = RForm.chartMain;

                    chart.ChartAreas[0].AxisX.TitleForeColor = RForm.foreMain;
                    chart.ChartAreas[0].AxisY.TitleForeColor = RForm.foreMain;

                    chart.ChartAreas[0].AxisX.LabelStyle.ForeColor = RForm.foreMain;
                    chart.ChartAreas[0].AxisY.LabelStyle.ForeColor = RForm.foreMain;

                    chart.ChartAreas[0].AxisX.MajorTickMark.LineColor = RForm.foreMain;
                    chart.ChartAreas[0].AxisY.MajorTickMark.LineColor = RForm.foreMain;

                    chart.ChartAreas[0].AxisX.MajorGrid.LineColor = RForm.chartGrid;
                    chart.ChartAreas[0].AxisY.MajorGrid.LineColor = RForm.chartGrid;
                    chart.ChartAreas[0].AxisX.LineColor = RForm.chartGrid;
                    chart.ChartAreas[0].AxisY.LineColor = RForm.chartGrid;

                    chart.Titles[0].ForeColor = RForm.foreMain;

                }

            }*/
        }

        #endregion
    }
}
