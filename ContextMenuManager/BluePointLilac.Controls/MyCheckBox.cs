using BluePointLilac.Methods;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    [DefaultProperty("Checked")]
    public class MyCheckBox : PictureBox
    {
        public MyCheckBox()
        {
            Image = TurnOff;
            Cursor = Cursors.Hand;
            SizeMode = PictureBoxSizeMode.AutoSize;
        }

        private bool? _Checked = null;
        public bool Checked
        {
            get => _Checked == true;
            set
            {
                if (_Checked == value) return;
                Image = SwitchImage(value);
                if (_Checked == null)
                {
                    _Checked = value;
                    return;
                }
                if (PreCheckChanging != null && !PreCheckChanging.Invoke())
                {
                    Image = SwitchImage(!value);
                    return;
                }
                CheckChanging?.Invoke();
                if (PreCheckChanged != null && !PreCheckChanged.Invoke())
                {
                    Image = SwitchImage(!value);
                    return;
                }
                _Checked = value;
                CheckChanged?.Invoke();
            }
        }

        public Func<bool> PreCheckChanging;
        public Func<bool> PreCheckChanged;
        public Action CheckChanging;
        public Action CheckChanged;

        public Image TurnOnImage { get; set; } = TurnOn;
        public Image TurnOffImage { get; set; } = TurnOff;

        private Image SwitchImage(bool value) => value ? TurnOnImage : TurnOffImage;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left) Checked = !Checked;
        }

        private static readonly Image TurnOn = DrawImage(true);
        private static readonly Image TurnOff = DrawImage(false);

        private static Image DrawImage(bool value)
        {
            int w = 80.DpiZoom(), h = 40.DpiZoom();
            int r = h / 2, padding = 4.DpiZoom();
            var bitmap = new Bitmap(w, h);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // 背景渐变
                using (var bgPath = CreateRoundedRect(0, 0, w, h, r))
                {
                    Color startColor = value ? MyMainForm.MainColor : Color.FromArgb(200, 200, 200);
                    Color endColor = value ? Color.FromArgb(160, MyMainForm.MainColor.R, MyMainForm.MainColor.G, MyMainForm.MainColor.B) :
                                             Color.FromArgb(160, 160, 160);
                    using (var bgBrush = new LinearGradientBrush(new Rectangle(0, 0, w, h), startColor, endColor, 90f))
                    {
                        g.FillPath(bgBrush, bgPath);
                    }
                }

                // 按钮位置计算
                int buttonX = value ? w - h + padding : padding;
                int buttonY = padding;

                // 按钮绘制（带阴影）
                using (var shadowPath = CreateRoundedRect(buttonX - 2, buttonY - 2, h - padding * 2 + 4, h - padding * 2 + 4, (h - padding * 2) / 2))
                using (var shadowBrush = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
                {
                    g.FillPath(shadowBrush, shadowPath);
                }

                using (var buttonPath = CreateRoundedRect(buttonX, buttonY, h - padding * 2, h - padding * 2, (h - padding * 2) / 2))
                {
                    using (var buttonBrush = new SolidBrush(Color.White))
                    {
                        g.FillPath(buttonBrush, buttonPath);
                    }
                }

                // 高光效果
                using (var highlightPath = CreateRoundedRect(buttonX + 2, buttonY + 2, (h - padding * 2) / 2, (h - padding * 2) / 2, (h - padding * 2) / 4))
                using (var highlightBrush = new SolidBrush(Color.FromArgb(100, 255, 255, 255)))
                {
                    g.FillPath(highlightBrush, highlightPath);
                }
            }
            return bitmap;
        }

        private static GraphicsPath CreateRoundedRect(float x, float y, float width, float height, float radius)
        {
            var path = new GraphicsPath();
            path.AddArc(x, y, radius * 2, radius * 2, 180, 90); // 左上角
            path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90); // 右上角
            path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90); // 右下角
            path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90); // 左下角
            path.CloseFigure();
            return path;
        }
    }
}