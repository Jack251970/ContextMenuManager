using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public class PictureButton : PictureBox
    {
        private Timer animationTimer;
        private float currentOpacity = 0f;
        private float targetOpacity = 0f;
        private const float ANIMATION_SPEED = 0.1f;

        public PictureButton(Image image)
        {
            BaseImage = image;
            SizeMode = PictureBoxSizeMode.AutoSize;
            Cursor = Cursors.Hand;

            // 监听主题变化
            DarkModeHelper.ThemeChanged += OnThemeChanged;

            // 初始化动画计时器
            animationTimer = new Timer();
            animationTimer.Interval = 16; // ~60 FPS
            animationTimer.Tick += AnimationTimer_Tick;
        }

        private Image baseImage;
        public Image BaseImage
        {
            get => baseImage;
            set
            {
                baseImage = value;
                // 初始状态为禁用效果
                Image = CreateDisabledImage(value);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            targetOpacity = 1f; // 目标为完全不透明
            animationTimer.Start();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            targetOpacity = 0f; // 目标为完全透明（禁用效果）
            animationTimer.Start();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) base.OnMouseDown(e);
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            // 逐步接近目标不透明度
            if (currentOpacity < targetOpacity)
            {
                currentOpacity += ANIMATION_SPEED;
                if (currentOpacity > targetOpacity) currentOpacity = targetOpacity;
            }
            else if (currentOpacity > targetOpacity)
            {
                currentOpacity -= ANIMATION_SPEED;
                if (currentOpacity < targetOpacity) currentOpacity = targetOpacity;
            }
            else
            {
                animationTimer.Stop();
                return;
            }

            // 创建混合图像
            Image normalImage = BaseImage;
            Image disabledImage = CreateDisabledImage(BaseImage);

            // 创建一个临时位图来绘制混合效果
            Bitmap mixedImage = new Bitmap(normalImage.Width, normalImage.Height);
            using (Graphics g = Graphics.FromImage(mixedImage))
            {
                // 先绘制禁用效果的图像
                g.DrawImage(disabledImage, 0, 0);

                // 然后根据当前不透明度绘制正常图像
                float opacity = Math.Max(0, Math.Min(1, currentOpacity));
                ColorMatrix matrix = new ColorMatrix();
                matrix.Matrix33 = opacity; // 设置透明度
                ImageAttributes attributes = new ImageAttributes();
                attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                g.DrawImage(
                    normalImage,
                    new Rectangle(0, 0, normalImage.Width, normalImage.Height),
                    0, 0, normalImage.Width, normalImage.Height,
                    GraphicsUnit.Pixel,
                    attributes
                );
            }

            // 更新显示的图像
            if (Image != null && Image != disabledImage && Image != baseImage)
                Image.Dispose();

            Image = mixedImage;

            // 清理资源
            disabledImage.Dispose();
        }

        private Image CreateDisabledImage(Image image)
        {
            return ToolStripRenderer.CreateDisabledImage(image);
        }
        
        // 主题变化事件处理
        private void OnThemeChanged(object sender, EventArgs e)
        {
            // 重新创建禁用效果的图像以适应主题变化
            if (baseImage != null)
            {
                Image = CreateDisabledImage(baseImage);
            }
        }

        // 添加资源清理
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DarkModeHelper.ThemeChanged -= OnThemeChanged;
                
                animationTimer?.Stop();
                animationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}