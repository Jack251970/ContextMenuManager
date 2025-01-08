using BluePointLilac.Methods;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public sealed class MyStatusBar : Panel
    {
        public static readonly string DefaultText = $"Ver: {Application.ProductVersion}    {Application.CompanyName}";

        public MyStatusBar()
        {
            Text = DefaultText;
            Height = 30.DpiZoom();
            Dock = DockStyle.Bottom;
            Font = SystemFonts.StatusFont;
            BackColor = MyMainForm.buttonMain;
            ForeColor = MyMainForm.foreMain;
        }

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
        public override string Text { get => base.Text; set => base.Text = value; }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            string txt = Text;
            int left = Height / 3;
            for(int i = Text.Length - 1; i >= 0; i--)
            {
                Size size = TextRenderer.MeasureText(txt, Font);
                if(size.Width < ClientSize.Width - 2 * left)
                {
                    using(Brush brush = new SolidBrush(ForeColor))
                    {
                        int top = (Height - size.Height) / 2;
                        e.Graphics.Clear(BackColor);
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