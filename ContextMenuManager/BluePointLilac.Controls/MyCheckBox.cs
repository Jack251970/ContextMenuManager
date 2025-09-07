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
        private Timer animationTimer;
        private double animationProgress = 0;
        private bool isAnimating = false;
        private bool animationDirection; // true: on->off, false: off->on
        private bool targetCheckedState;
        
        // 缓存静态图像，避免重复绘制
        private static readonly Image TurnOn = DrawStaticImage(true);
        private static readonly Image TurnOff = DrawStaticImage(false);
        
        public MyCheckBox()
        {
            // 启用双缓冲
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | 
                         ControlStyles.UserPaint | 
                         ControlStyles.OptimizedDoubleBuffer, true);
            
            Image = TurnOff;
            Cursor = Cursors.Hand;
            SizeMode = PictureBoxSizeMode.AutoSize;
            
            // 初始化动画计时器
            animationTimer = new Timer();
            animationTimer.Interval = 10; // 提高帧率到约100FPS
            animationTimer.Tick += AnimationTimer_Tick;
        }

        private bool? _Checked = null;
        public bool Checked
        {
            get => _Checked == true;
            set
            {
                if (_Checked == value) return;
                
                if (_Checked == null)
                {
                    // 首次设置，不执行动画
                    _Checked = value;
                    Image = SwitchImage(value);
                    return;
                }
                
                if (PreCheckChanging != null && !PreCheckChanging.Invoke())
                {
                    return;
                }
                
                CheckChanging?.Invoke();
                
                if (PreCheckChanged != null && !PreCheckChanged.Invoke())
                {
                    return;
                }
                
                // 设置目标状态和动画方向
                targetCheckedState = value;
                animationDirection = !value;
                
                // 如果正在动画，反转动画方向
                if (isAnimating)
                {
                    // 反转动画进度
                    animationProgress = 1 - animationProgress;
                }
                else
                {
                    // 开始新动画
                    animationProgress = 0;
                    isAnimating = true;
                    animationTimer.Start();
                }
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
        
        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            // 更新动画进度
            animationProgress += 0.12; // 增加动画速度
            
            if (animationProgress >= 1)
            {
                // 动画完成
                animationProgress = 1;
                isAnimating = false;
                animationTimer.Stop();
                
                // 更新实际状态
                _Checked = targetCheckedState;
                CheckChanged?.Invoke();
                
                // 使用静态图像以提高性能
                Image = SwitchImage(targetCheckedState);
            }
            else
            {
                // 重绘控件
                Image = DrawAnimatedImage(animationProgress, animationDirection);
            }
            
            Invalidate();
        }

        private static Image DrawStaticImage(bool value)
        {
            int w = 80.DpiZoom(), h = 40.DpiZoom();
            int r = h / 2, padding = 4.DpiZoom();
            var bitmap = new Bitmap(w, h);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // 背景渐变
                Color startColor, endColor;
                if (value)
                {
                    startColor = MyMainForm.MainColor;
                    endColor = Color.FromArgb(160, MyMainForm.MainColor.R, MyMainForm.MainColor.G, MyMainForm.MainColor.B);
                }
                else
                {
                    startColor = Color.FromArgb(200, 200, 200);
                    endColor = Color.FromArgb(160, 160, 160);
                }
                
                using (var bgPath = CreateRoundedRect(0, 0, w, h, r))
                using (var bgBrush = new LinearGradientBrush(new Rectangle(0, 0, w, h), startColor, endColor, 90f))
                {
                    g.FillPath(bgBrush, bgPath);
                }

                // 按钮位置
                int buttonX = value ? w - h + padding : padding;
                int buttonY = padding;

                // 按钮绘制（带阴影）
                using (var shadowPath = CreateRoundedRect(buttonX - 2, buttonY - 2, h - padding * 2 + 4, h - padding * 2 + 4, (h - padding * 2) / 2))
                using (var shadowBrush = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
                {
                    g.FillPath(shadowBrush, shadowPath);
                }

                using (var buttonPath = CreateRoundedRect(buttonX, buttonY, h - padding * 2, h - padding * 2, (h - padding * 2) / 2))
                using (var buttonBrush = new SolidBrush(Color.White))
                {
                    g.FillPath(buttonBrush, buttonPath);
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
        
        private static Image DrawAnimatedImage(double progress, bool direction)
        {
            int w = 80.DpiZoom(), h = 40.DpiZoom();
            int r = h / 2, padding = 4.DpiZoom();
            var bitmap = new Bitmap(w, h);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // 计算动画中间状态
                double easedProgress = EaseInOutCubic(progress);
                
                // 背景渐变 - 使用动画中间颜色
                Color startColor, endColor;
                if (direction) // 关闭方向
                {
                    startColor = InterpolateColor(
                        MyMainForm.MainColor,
                        Color.FromArgb(200, 200, 200),
                        easedProgress);
                    endColor = InterpolateColor(
                        Color.FromArgb(160, MyMainForm.MainColor.R, MyMainForm.MainColor.G, MyMainForm.MainColor.B),
                        Color.FromArgb(160, 160, 160),
                        easedProgress);
                }
                else // 开启方向
                {
                    startColor = InterpolateColor(
                        Color.FromArgb(200, 200, 200),
                        MyMainForm.MainColor,
                        easedProgress);
                    endColor = InterpolateColor(
                        Color.FromArgb(160, 160, 160),
                        Color.FromArgb(160, MyMainForm.MainColor.R, MyMainForm.MainColor.G, MyMainForm.MainColor.B),
                        easedProgress);
                }
                
                using (var bgPath = CreateRoundedRect(0, 0, w, h, r))
                using (var bgBrush = new LinearGradientBrush(new Rectangle(0, 0, w, h), startColor, endColor, 90f))
                {
                    g.FillPath(bgBrush, bgPath);
                }

                // 按钮位置计算 - 使用动画中间位置
                int startX = direction ? w - h + padding : padding;
                int endX = direction ? padding : w - h + padding;
                int buttonX = (int)(startX + (endX - startX) * easedProgress);
                int buttonY = padding;

                // 按钮绘制（带阴影）
                using (var shadowPath = CreateRoundedRect(buttonX - 2, buttonY - 2, h - padding * 2 + 4, h - padding * 2 + 4, (h - padding * 2) / 2))
                using (var shadowBrush = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
                {
                    g.FillPath(shadowBrush, shadowPath);
                }

                using (var buttonPath = CreateRoundedRect(buttonX, buttonY, h - padding * 2, h - padding * 2, (h - padding * 2) / 2))
                using (var buttonBrush = new SolidBrush(Color.White))
                {
                    g.FillPath(buttonBrush, buttonPath);
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
        
        // 缓动函数 - 使动画更加自然
        private static double EaseInOutCubic(double t)
        {
            return t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;
        }
        
        // 颜色插值函数
        private static Color InterpolateColor(Color start, Color end, double progress)
        {
            int r = (int)(start.R + (end.R - start.R) * progress);
            int g = (int)(start.G + (end.G - start.G) * progress);
            int b = (int)(start.B + (end.B - start.B) * progress);
            return Color.FromArgb(r, g, b);
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
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                animationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}