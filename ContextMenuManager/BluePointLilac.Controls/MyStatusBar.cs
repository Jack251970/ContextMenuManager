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

        // 氝樓ʊ伎膝曹扽俶
        private Color topColor = Color.Empty;
        private Color middleColor = Color.Empty;
        private Color bottomColor = Color.Empty;

        // 旮伎耀宒梓祩
        private bool isDarkMode = false;

        public MyStatusBar()
        {
            Text = DefaultText;
            Height = 30.DpiZoom();
            Dock = DockStyle.Bottom;
            Font = SystemFonts.StatusFont;

            // 潰聆炵苀翋枙甜扢离晇伎
            CheckSystemTheme();

            // 隆堐炵苀翋枙載蜊岈璃
            DarkModeHelper.ThemeChanged += OnThemeChanged;
        }

        // 炵苀翋枙載蜊岈璃揭燴
        private void OnThemeChanged(object sender, EventArgs e)
        {
            CheckSystemTheme();
            Refresh();
        }

        // 潰聆炵苀翋枙
        private void CheckSystemTheme()
        {
            // 使用DarkModeHelper统一管理主题
            isDarkMode = DarkModeHelper.IsDarkTheme;

            if (isDarkMode)
            {
                // 旮伎耀宒晇伎源偶 - 妏蚚ʊ伎膝曹
                BackColor = Color.FromArgb(40, 40, 40); // 笢潔伎覃釬峈掖劓伎
                ForeColor = Color.LightGray;

                // 使用DarkModeHelper中的颜色
                topColor = DarkModeHelper.StatusBarGradientTop;
                middleColor = DarkModeHelper.StatusBarGradientMiddle;
                bottomColor = DarkModeHelper.StatusBarGradientBottom;
            }
            else
            {
                // シ伎耀宒晇伎源偶
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

        // 氝樓ʊ伎膝曹扽俶
        [Browsable(true), Category("Appearance"), Description("膝曹階窒晇伎")]
        public Color TopColor
        {
            get => topColor;
            set { topColor = value; Refresh(); }
        }

        [Browsable(true), Category("Appearance"), Description("膝曹笢潔晇伎")]
        public Color MiddleColor
        {
            get => middleColor;
            set { middleColor = value; Refresh(); }
        }

        [Browsable(true), Category("Appearance"), Description("膝曹菁窒晇伎")]
        public Color BottomColor
        {
            get => bottomColor;
            set { bottomColor = value; Refresh(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // 斐膘ʊ伎膝曹掖劓
            using (LinearGradientBrush brush = new LinearGradientBrush(
                ClientRectangle,
                Color.Empty,
                Color.Empty,
                LinearGradientMode.Vertical))
            {
                // 扢离ʊ伎膝曹
                ColorBlend colorBlend = new ColorBlend(3);
                colorBlend.Colors = new Color[] { TopColor, MiddleColor, BottomColor };
                colorBlend.Positions = new float[] { 0f, 0.5f, 1f };
                brush.InterpolationColors = colorBlend;

                e.Graphics.FillRectangle(brush, ClientRectangle);
            }

            // 餅秶恅掛ㄗ悵厥埻衄軀憮ㄘ
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