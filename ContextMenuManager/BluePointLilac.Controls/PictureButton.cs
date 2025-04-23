using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public class PictureButton : PictureBox
    {
        private readonly Timer timer = new Timer { Interval = 16 };
        private float targetOpacity = 1f;
        private float currentOpacity = 0.4f; // 初始透明度设为禁用状态
        private Image originalImage;
        private Image disabledImage; // 缓存禁用图像

        public PictureButton(Image image)
        {
            BaseImage = image;
            SizeMode = PictureBoxSizeMode.AutoSize;
            Cursor = Cursors.Hand;
            timer.Tick += Timer_Tick;
        }

        private Image baseImage;
        public Image BaseImage
        {
            get => baseImage;
            set
            {
                baseImage = value;
                originalImage = value; // 始终保留原始正常图像
                disabledImage = ToolStripRenderer.CreateDisabledImage(value); // 预生成禁用图像
                Image = ApplyOpacity(disabledImage, currentOpacity, true);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            const float speed = 0.1f;
            currentOpacity += (targetOpacity - currentOpacity) * speed;

            bool reachTarget = Math.Abs(currentOpacity - targetOpacity) < 0.01f;
            if (reachTarget)
            {
                currentOpacity = targetOpacity;
                timer.Stop();
            }

            // 使用if-else逻辑替代switch表达式
            if (currentOpacity == 1.0f)
            {
                Image = originalImage;
            }
            else if (currentOpacity == 0.4f)
            {
                Image = disabledImage;
            }
            else
            {
                Image = ApplyOpacity(originalImage, currentOpacity);
            }

            // 仅实际变化时触发重绘
            if (!reachTarget) Refresh();
        }

        private static readonly ImageAttributes Opacity40Attr = CreateOpacityAttributes(0.4f);
        private static readonly ImageAttributes Opacity100Attr = CreateOpacityAttributes(1f);

        private static ImageAttributes CreateOpacityAttributes(float opacity)
        {
            var matrix = new ColorMatrix { Matrix33 = opacity };
            var attributes = new ImageAttributes();
            attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            return attributes;
        }

        private Image ApplyOpacity(Image source, float opacity, bool useCache = false)
        {
            if (source == null) return null;

            // 缓存常用透明度图像
            if (useCache && opacity == 1f) return originalImage;
            if (useCache && opacity == 0.4f) return disabledImage;

            var newImage = new Bitmap(source.Width, source.Height);
            using (var g = Graphics.FromImage(newImage))
            {
                var attributes = opacity == 1.0f ? Opacity100Attr :
                                 opacity == 0.4f ? Opacity40Attr :
                                 CreateOpacityAttributes(opacity);

                g.DrawImage(source, 
                    new Rectangle(0, 0, newImage.Width, newImage.Height),
                    0, 0, source.Width, source.Height,
                    GraphicsUnit.Pixel, attributes);
            }
            return newImage;
        }

        private void StartAnimation(float target)
        {
            if (timer.Enabled) timer.Stop();
            targetOpacity = target;
            timer.Start();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            currentOpacity = 0.4f; // 强制从禁用状态透明度开始
            StartAnimation(1f); 
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            currentOpacity = 1f; // 强制从正常状态透明度开始
            StartAnimation(0.4f);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer?.Dispose();
                originalImage?.Dispose();
                disabledImage?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}