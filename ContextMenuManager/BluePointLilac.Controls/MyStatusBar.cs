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

        // 渐变色定义
        private Color topColor = Color.Empty;
        private Color middleColor = Color.Empty;
        private Color bottomColor = Color.Empty;

        // 主题模式标识
        private bool isDarkMode = false;

        public MyStatusBar()
        {
            Text = DefaultText;
            Height = 30.DpiZoom();
            Dock = DockStyle.Bottom;
            Font = SystemFonts.StatusFont;

            // 初始化系统主题
            CheckSystemTheme();

            // 监听主题变化事件
            DarkModeHelper.ThemeChanged += OnThemeChanged;
        }

        // 主题变化事件处理
        private void OnThemeChanged(object sender, EventArgs e)
        {
            CheckSystemTheme();
            Refresh();
        }

        // 检查系统主题
        private void CheckSystemTheme()
        {
            // 使用DarkModeHelper统一管理主题
            isDarkMode = DarkModeHelper.IsDarkTheme;

            if (isDarkMode)
            {
                // 深色模式颜色方案 - 使用渐变色
                BackColor = Color.FromArgb(40, 40, 40); // 备用背景色
                ForeColor = Color.LightGray;

                // 使用DarkModeHelper中的颜色
                topColor = DarkModeHelper.StatusBarGradientTop;
                middleColor = DarkModeHelper.StatusBarGradientMiddle;
                bottomColor = DarkModeHelper.StatusBarGradientBottom;
            }
            else
            {
                // 浅色模式颜色方案
                BackColor = DarkModeHelper.ButtonMain;
                ForeColor = DarkModeHelper.FormFore;

                // 使用DarkModeHelper中的颜色
                topColor = DarkModeHelper.StatusBarGradientTop;
                middleColor = DarkModeHelper.StatusBarGradientMiddle;
                bottomColor = DarkModeHelper.StatusBarGradientBottom;
            }
        }

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
        public override string Text { get => base.Text; set => base.Text = value; }

        // 渐变色属性
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
            // 绘制渐变色背景
            using (var brush = new LinearGradientBrush(
                ClientRectangle,
                Color.Empty,
                Color.Empty,
                LinearGradientMode.Vertical))
            {
                // 设置渐变色
                var colorBlend = new ColorBlend(3);
                colorBlend.Colors = new Color[] { TopColor, MiddleColor, BottomColor };
                colorBlend.Positions = new float[] { 0f, 0.5f, 1f };
                brush.InterpolationColors = colorBlend;

                e.Graphics.FillRectangle(brush, ClientRectangle);
            }

            // 绘制文本（带有省略号处理）
            var txt = Text;
            var left = Height / 3;
            for (var i = Text.Length - 1; i >= 0; i--)
            {
                var size = TextRenderer.MeasureText(txt, Font);
                if (size.Width < ClientSize.Width - 2 * left)
                {
                    using Brush brush = new SolidBrush(ForeColor);
                    var top = (Height - size.Height) / 2;
                    e.Graphics.DrawString(txt, Font, brush, left, top);
                    break;
                }
                txt = Text[..i] + "...";
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DarkModeHelper.ThemeChanged -= OnThemeChanged;
            }
            base.Dispose(disposing);
        }
    }
}