
using BluePointLilac.Methods;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    [DefaultProperty("Checked")]
    public class MyCheckBox : Control
    {
        private Timer animationTimer;
        private double animationProgress = 0;
        private bool isAnimating = false;
        private bool targetCheckedState;
        private bool currentCheckedState;

        // 预计算尺寸和位置
        private int WidthPx, HeightPx, RadiusPx, PaddingPx, ButtonSizePx;

        public MyCheckBox()
        {
            // 启用双缓冲
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.ResizeRedraw, true);

            // 计算尺寸
            HeightPx = 40.DpiZoom();
            WidthPx = 80.DpiZoom();
            RadiusPx = HeightPx / 2;
            PaddingPx = 4.DpiZoom();
            ButtonSizePx = HeightPx - PaddingPx * 2;

            // 设置控件大小
            this.Size = new Size(WidthPx, HeightPx);

            Cursor = Cursors.Hand;

            // 初始化动画计时器
            animationTimer = new Timer();
            animationTimer.Interval = 16; // 约60FPS
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
                    currentCheckedState = value;
                    Invalidate();
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

                // 设置目标状态
                targetCheckedState = value;

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

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left) Checked = !Checked;
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            // 检查控件是否已释放或不可见
            if (this.IsDisposed || !this.Visible)
            {
                animationTimer.Stop();
                isAnimating = false;
                return;
            }

            try
            {
                // 更新动画进度
                animationProgress += 0.10;

                if (animationProgress >= 1)
                {
                    // 动画完成
                    animationProgress = 1;
                    isAnimating = false;
                    animationTimer.Stop();

                    // 更新实际状态
                    currentCheckedState = targetCheckedState;
                    _Checked = currentCheckedState;

                    // 触发事件
                    CheckChanged?.Invoke();
                }

                // 请求重绘
                Invalidate();
            }
            catch
            {
                // 发生异常时停止动画
                animationTimer.Stop();
                isAnimating = false;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // 检查控件是否已释放
            if (this.IsDisposed) return;

            try
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // 计算动画中间状态
                double easedProgress = EaseInOutCubic(animationProgress);

                // 确定当前视觉状态
                bool visualState = isAnimating ?
                    (animationProgress > 0.5 ? targetCheckedState : currentCheckedState) :
                    currentCheckedState;

                // 绘制背景 - 使用三色渐变色
                using (var bgPath = CreateRoundedRect(0, 0, WidthPx, HeightPx, RadiusPx))
                {
                    if (visualState)
                    {
                        // 开启状态的三色渐变
                        Color topColor = Color.FromArgb(255, 235, 59);
                        Color middleColor = Color.FromArgb(255, 196, 0);
                        Color bottomColor = Color.FromArgb(255, 235, 59);

                        // 如果是动画状态，进行颜色插值
                        if (isAnimating)
                        {
                            Color targetTopColor, targetMiddleColor, targetBottomColor;

                            if (targetCheckedState)
                            {
                                targetTopColor = Color.FromArgb(255, 235, 59);
                                targetMiddleColor = Color.FromArgb(255, 196, 0);
                                targetBottomColor = Color.FromArgb(255, 235, 59);
                            }
                            else
                            {
                                targetTopColor = Color.FromArgb(255, 235, 59);
                                targetMiddleColor = Color.FromArgb(255, 196, 0);
                                targetBottomColor = Color.FromArgb(255, 235, 59);
                            }

                            topColor = InterpolateColor(topColor, targetTopColor, easedProgress);
                            middleColor = InterpolateColor(middleColor, targetMiddleColor, easedProgress);
                            bottomColor = InterpolateColor(bottomColor, targetBottomColor, easedProgress);
                        }

                        // 创建三色渐变
                        using (var bgBrush = new LinearGradientBrush(
                            new Rectangle(0, 0, WidthPx, HeightPx),
                            Color.Empty, Color.Empty,
                            90f))
                        {
                            var colorBlend = new ColorBlend(3);
                            colorBlend.Colors = new Color[] { topColor, middleColor, bottomColor };
                            colorBlend.Positions = new float[] { 0f, 0.5f, 1f };
                            bgBrush.InterpolationColors = colorBlend;

                            g.FillPath(bgBrush, bgPath);
                        }
                    }
                    else
                    {
                        // 关闭状态的三色渐变
                        Color topColor = Color.FromArgb(255, 255, 255);
                        Color middleColor = Color.FromArgb(230, 230, 230);
                        Color bottomColor = Color.FromArgb(255, 255, 255);

                        // 如果是动画状态，进行颜色插值
                        if (isAnimating)
                        {
                            Color targetTopColor, targetMiddleColor, targetBottomColor;

                            if (targetCheckedState)
                            {
                                targetTopColor = Color.FromArgb(255, 235, 59);
                                targetMiddleColor = Color.FromArgb(255, 196, 0);
                                targetBottomColor = Color.FromArgb(255, 235, 59);
                            }
                            else
                            {
                                targetTopColor = Color.FromArgb(255, 255, 255);
                                targetMiddleColor = Color.FromArgb(230, 230, 230);
                                targetBottomColor = Color.FromArgb(255, 255, 255);
                            }

                            topColor = InterpolateColor(topColor, targetTopColor, easedProgress);
                            middleColor = InterpolateColor(middleColor, targetMiddleColor, easedProgress);
                            bottomColor = InterpolateColor(bottomColor, targetBottomColor, easedProgress);
                        }

                        // 创建三色渐变
                        using (var bgBrush = new LinearGradientBrush(
                            new Rectangle(0, 0, WidthPx, HeightPx),
                            Color.Empty, Color.Empty,
                            90f))
                        {
                            var colorBlend = new ColorBlend(3);
                            colorBlend.Colors = new Color[] { topColor, middleColor, bottomColor };
                            colorBlend.Positions = new float[] { 0f, 0.5f, 1f };
                            bgBrush.InterpolationColors = colorBlend;

                            g.FillPath(bgBrush, bgPath);
                        }
                    }
                }

                // 按钮位置计算
                int startX = currentCheckedState ? (WidthPx - HeightPx + PaddingPx) : PaddingPx;
                int endX = targetCheckedState ? (WidthPx - HeightPx + PaddingPx) : PaddingPx;
                int buttonX = (int)(startX + (endX - startX) * easedProgress);
                int buttonY = PaddingPx;

                // 改进的阴影效果 - 多层阴影
                for (int i = 3; i > 0; i--)
                {
                    int shadowSize = i * 2;
                    int shadowOffset = i;
                    using (var shadowPath = CreateRoundedRect(
                        buttonX - shadowSize / 2 + shadowOffset / 2,
                        buttonY - shadowSize / 2 + shadowOffset,
                        ButtonSizePx + shadowSize,
                        ButtonSizePx + shadowSize,
                        (ButtonSizePx + shadowSize) / 2))
                    using (var shadowBrush = new SolidBrush(Color.FromArgb(20 / i, 0, 0, 0)))
                    {
                        g.FillPath(shadowBrush, shadowPath);
                    }
                }

                // 按钮绘制
                using (var buttonPath = CreateRoundedRect(buttonX, buttonY, ButtonSizePx, ButtonSizePx, ButtonSizePx / 2))
                using (var buttonBrush = new SolidBrush(Color.White))
                {
                    g.FillPath(buttonBrush, buttonPath);
                }

                // 高光效果
                using (var highlightPath = CreateRoundedRect(buttonX + 2, buttonY + 2, ButtonSizePx / 2, ButtonSizePx / 2, ButtonSizePx / 4))
                using (var highlightBrush = new SolidBrush(Color.FromArgb(100, 255, 255, 255)))
                {
                    g.FillPath(highlightBrush, highlightPath);
                }
            }
            catch
            {
                // 绘制过程中发生异常，忽略
            }
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
                // 停止并释放计时器
                if (animationTimer != null)
                {
                    animationTimer.Stop();
                    animationTimer.Dispose();
                    animationTimer = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}