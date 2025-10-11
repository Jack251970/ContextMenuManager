using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public class MyButton : Button
    {
        private ButtonState _buttonState = ButtonState.Normal;
        private ButtonState _targetState = ButtonState.Normal;
        private Timer _animationTimer;
        private float _animationProgress = 0f;
        private const int AnimationDuration = 200; // 动画持续时间（毫秒）

        // 当前显示的颜色（用于动画插值）
        private Color[] _currentColors = new Color[3];

        // 起始颜色（动画开始时）
        private Color[] _startColors = new Color[3];

        // 目标颜色（根据状态）
        private Color[] _targetColors = new Color[3];

        // 橘色主题的三色渐变
        private Color[] _normalColors = new Color[]
        {
            Color.FromArgb(255, 195, 0),   // 顶部
            Color.FromArgb(255, 140, 26),   // 中间
            Color.FromArgb(255, 195, 0)     // 底部
        };

        private Color[] _hoverColors = new Color[]
        {
            Color.FromArgb(255, 210, 0),   // 顶部
            Color.FromArgb(255, 160, 26),   // 中间
            Color.FromArgb(255, 210, 0)     // 底部
        };

        private Color[] _pressedColors = new Color[]
        {
            Color.FromArgb(255, 170, 0),   // 顶部
            Color.FromArgb(255, 110, 26),   // 中间
            Color.FromArgb(255, 170, 0)     // 底部
        };

        private Color[] _disabledColors = new Color[]
        {
            Color.FromArgb(100, 140, 180),
            Color.FromArgb(80, 120, 160),
            Color.FromArgb(60, 100, 140)
        };

        public MyButton()
        {
            // 设置按钮的基本属性
            FlatStyle = FlatStyle.Flat;
            ForeColor = Color.White;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            Margin = new Padding(4);
            Padding = new Padding(6, 3, 6, 3);
            UseVisualStyleBackColor = false;

            // 设置尺寸相关属性
            MinimumSize = new Size(75, 27);
            AutoSize = true;

            // 初始化颜色
            Array.Copy(_normalColors, _currentColors, 3);
            Array.Copy(_normalColors, _startColors, 3);
            Array.Copy(_normalColors, _targetColors, 3);

            // 初始化动画计时器
            _animationTimer = new Timer();
            _animationTimer.Interval = 16; // ~60fps
            _animationTimer.Tick += AnimationTimer_Tick;

            // 禁用默认的视觉效果
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        // 圆角半径属性
        public int CornerRadius { get; set; } = 8;

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 根据按钮状态设置文本颜色
            if (!Enabled)
            {
                ForeColor = Color.FromArgb(180, 180, 180);
            }
            else
            {
                ForeColor = Color.White;
            }

            // 创建圆角矩形路径
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            GraphicsPath path = CreateRoundedRectanglePath(rect, CornerRadius);

            // 绘制三色垂直渐变背景
            using (LinearGradientBrush brush = new LinearGradientBrush(
                rect, Color.Empty, Color.Empty, LinearGradientMode.Vertical))
            {
                ColorBlend colorBlend = new ColorBlend(3);
                colorBlend.Colors = _currentColors;
                colorBlend.Positions = new float[] { 0f, 0.5f, 1f };
                brush.InterpolationColors = colorBlend;

                g.FillPath(brush, path);
            }

            // 绘制边框
            using (Pen pen = new Pen(_currentColors[2], 1))
            {
                g.DrawPath(pen, path);
            }

            // 绘制文本
            TextRenderer.DrawText(g, Text, Font, rect, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            path.Dispose();
        }

        private GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();

            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            int diameter = radius * 2;
            Rectangle arcRect = new Rectangle(rect.Location, new Size(diameter, diameter));

            // 左上角
            path.AddArc(arcRect, 180, 90);

            // 右上角
            arcRect.X = rect.Right - diameter;
            path.AddArc(arcRect, 270, 90);

            // 右下角
            arcRect.Y = rect.Bottom - diameter;
            path.AddArc(arcRect, 0, 90);

            // 左下角
            arcRect.X = rect.Left;
            path.AddArc(arcRect, 90, 90);

            path.CloseFigure();
            return path;
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            _animationProgress += (float)_animationTimer.Interval / AnimationDuration;

            if (_animationProgress >= 1f)
            {
                _animationProgress = 1f;
                _animationTimer.Stop();
                // 确保最终颜色准确
                Array.Copy(_targetColors, _currentColors, 3);
            }
            else
            {
                // 使用缓动函数使动画更自然
                float easedProgress = EaseOutCubic(_animationProgress);

                // 插值计算当前颜色
                for (int i = 0; i < 3; i++)
                {
                    _currentColors[i] = InterpolateColor(_startColors[i], _targetColors[i], easedProgress);
                }
            }

            Invalidate();
        }

        // 缓动函数 - 使动画更自然
        private float EaseOutCubic(float t)
        {
            return (float)(1 - Math.Pow(1 - t, 3));
        }

        private Color InterpolateColor(Color from, Color to, float progress)
        {
            int r = (int)(from.R + (to.R - from.R) * progress);
            int g = (int)(from.G + (to.G - from.G) * progress);
            int b = (int)(from.B + (to.B - from.B) * progress);
            int a = (int)(from.A + (to.A - from.A) * progress);

            // 确保颜色值在有效范围内
            r = Math.Max(0, Math.Min(255, r));
            g = Math.Max(0, Math.Min(255, g));
            b = Math.Max(0, Math.Min(255, b));
            a = Math.Max(0, Math.Min(255, a));

            return Color.FromArgb(a, r, g, b);
        }

        private void StartAnimationToState(ButtonState newState)
        {
            // 保存当前颜色作为动画起点
            Array.Copy(_currentColors, _startColors, 3);

            _targetState = newState;

            // 设置目标颜色
            if (!Enabled)
            {
                Array.Copy(_disabledColors, _targetColors, 3);
            }
            else
            {
                switch (newState)
                {
                    case ButtonState.Normal:
                        Array.Copy(_normalColors, _targetColors, 3);
                        break;
                    case ButtonState.Hover:
                        Array.Copy(_hoverColors, _targetColors, 3);
                        break;
                    case ButtonState.Pressed:
                        Array.Copy(_pressedColors, _targetColors, 3);
                        break;
                }
            }

            _animationProgress = 0f;

            // 确保计时器运行
            if (!_animationTimer.Enabled)
            {
                _animationTimer.Start();
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _buttonState = ButtonState.Hover;
            StartAnimationToState(ButtonState.Hover);
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _buttonState = ButtonState.Normal;
            StartAnimationToState(ButtonState.Normal);
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _buttonState = ButtonState.Pressed;
                StartAnimationToState(ButtonState.Pressed);
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // 判断鼠标是否仍在按钮上方
                Point clientPos = PointToClient(MousePosition);
                if (ClientRectangle.Contains(clientPos))
                {
                    _buttonState = ButtonState.Hover;
                    StartAnimationToState(ButtonState.Hover);
                }
                else
                {
                    _buttonState = ButtonState.Normal;
                    StartAnimationToState(ButtonState.Normal);
                }
            }
            base.OnMouseUp(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            // 立即更新颜色，不等待动画
            if (!Enabled)
            {
                Array.Copy(_disabledColors, _currentColors, 3);
                Array.Copy(_disabledColors, _targetColors, 3);
            }
            else
            {
                Array.Copy(_normalColors, _currentColors, 3);
                Array.Copy(_normalColors, _targetColors, 3);
            }

            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer?.Stop();
                _animationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        protected enum ButtonState
        {
            Normal,
            Hover,
            Pressed
        }
    }
}