using BluePointLilac.Methods;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public sealed class MyStatusBar : Panel
    {
        public static readonly string DefaultText = $"Ver: {Application.ProductVersion}    {Application.CompanyName}";

        // 添加三色渐变属性
        private Color topColor = Color.Empty;
        private Color middleColor = Color.Empty;
        private Color bottomColor = Color.Empty;

        // 深色模式标志
        private bool isDarkMode = false;

        public MyStatusBar()
        {
            Text = DefaultText;
            Height = 30.DpiZoom();
            Dock = DockStyle.Bottom;
            Font = SystemFonts.StatusFont;

            // 检测系统主题并设置颜色
            CheckSystemTheme();

            // 订阅系统主题更改事件
            Microsoft.Win32.SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
            this.Disposed += (s, e) => Microsoft.Win32.SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        }

        // 系统主题更改事件处理
        private void SystemEvents_UserPreferenceChanged(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
        {
            if (e.Category == Microsoft.Win32.UserPreferenceCategory.General)
            {
                CheckSystemTheme();
                Refresh();
            }
        }

        // 检测系统主题
        private void CheckSystemTheme()
        {
            // 检测是否深色模式 (Windows 10/11)
            isDarkMode = IsDarkThemeEnabled();

            if (isDarkMode)
            {
                // 深色模式颜色方案 - 使用三色渐变
                BackColor = Color.FromArgb(40, 40, 40); // 中间色调作为背景色
                ForeColor = Color.LightGray;

                // 深色模式渐变颜色 - 三色渐变
                topColor = Color.FromArgb(128, 128, 128);
                middleColor = Color.FromArgb(56, 56, 56);
                bottomColor = Color.FromArgb(128, 128, 128);
            }
            else
            {
                // 浅色模式颜色方案
                BackColor = MyMainForm.ButtonMain;
                ForeColor = MyMainForm.FormFore;

                // 浅色模式渐变颜色 - 三色渐变
                topColor = Color.FromArgb(255, 255, 255);
                middleColor = Color.FromArgb(230, 230, 230);
                bottomColor = Color.FromArgb(255, 255, 255);
            }
        }

        // 检测系统是否使用深色主题
        private bool IsDarkThemeEnabled()
        {
            try
            {
                // 通过注册表检测Windows主题设置
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("AppsUseLightTheme");
                        if (value != null && value is int)
                        {
                            return (int)value == 0;
                        }
                    }
                }
            }
            catch
            {
                // 如果无法检测，使用默认值
            }

            // 默认使用浅色模式
            return false;
        }

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
        public override string Text { get => base.Text; set => base.Text = value; }

        // 添加三色渐变属性
        [Browsable(true), Category("Appearance"), Description("渐变顶部颜色")]
        public Color TopColor
        {
            get => topColor;
            set { topColor = value; Refresh(); }
        }

        [Browsable(true), Category("Appearance"), Description("渐变中间颜色")]
        public Color MiddleColor
        {
            get => middleColor;
            set { middleColor = value; Refresh(); }
        }

        [Browsable(true), Category("Appearance"), Description("渐变底部颜色")]
        public Color BottomColor
        {
            get => bottomColor;
            set { bottomColor = value; Refresh(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // 创建三色渐变背景
            using (LinearGradientBrush brush = new LinearGradientBrush(
                ClientRectangle,
                Color.Empty,
                Color.Empty,
                LinearGradientMode.Vertical))
            {
                // 设置三色渐变
                ColorBlend colorBlend = new ColorBlend(3);
                colorBlend.Colors = new Color[] { TopColor, MiddleColor, BottomColor };
                colorBlend.Positions = new float[] { 0f, 0.5f, 1f };
                brush.InterpolationColors = colorBlend;

                e.Graphics.FillRectangle(brush, ClientRectangle);
            }

            // 绘制文本（保持原有逻辑）
            string txt = Text;
            int left = Height / 3;
            for (int i = Text.Length - 1; i >= 0; i--)
            {
                Size size = TextRenderer.MeasureText(txt, Font);
                if (size.Width < ClientSize.Width - 2 * left)
                {
                    using (Brush brush = new SolidBrush(ForeColor))
                    {
                        int top = (Height - size.Height) / 2;
                        e.Graphics.DrawString(txt, Font, brush, left, top);
                        break;
                    }
                }
                txt = Text.Substring(0, i) + "...";
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e); Refresh();
        }
        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e); Refresh();
        }
        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e); Refresh();
        }
        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e); Refresh();
        }
        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e); Refresh();
        }
    }
}