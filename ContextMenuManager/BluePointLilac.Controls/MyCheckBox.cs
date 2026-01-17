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
        private readonly Timer animationTimer = new() { Interval = 16 };
        private double animationProgress = 0;
        private bool isAnimating = false;
        private bool targetCheckedState;
        private bool currentCheckedState;

        private readonly int WidthPx, HeightPx, RadiusPx, PaddingPx, ButtonSizePx;

        public MyCheckBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            HeightPx = 40.DpiZoom();
            WidthPx = 80.DpiZoom();
            RadiusPx = HeightPx / 2;
            PaddingPx = 4.DpiZoom();
            ButtonSizePx = HeightPx - PaddingPx * 2;

            Size = new Size(WidthPx, HeightPx);
            Cursor = Cursors.Hand;

            animationTimer.Tick += AnimationTimer_Tick;
            DarkModeHelper.ThemeChanged += OnThemeChanged;
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
                    _Checked = value;
                    currentCheckedState = value;
                    Invalidate();
                    return;
                }

                if (PreCheckChanging != null && !PreCheckChanging.Invoke()) return;
                CheckChanging?.Invoke();
                if (PreCheckChanged != null && !PreCheckChanged.Invoke()) return;

                targetCheckedState = value;
                if (isAnimating)
                    animationProgress = 1 - animationProgress;
                else
                {
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
            if (IsDisposed || !Visible)
            {
                animationTimer.Stop();
                isAnimating = false;
                return;
            }

            try
            {
                animationProgress += 0.10;
                if (animationProgress >= 1)
                {
                    animationProgress = 1;
                    isAnimating = false;
                    animationTimer.Stop();
                    currentCheckedState = targetCheckedState;
                    _Checked = currentCheckedState;
                    CheckChanged?.Invoke();
                }
                Invalidate();
            }
            catch
            {
                animationTimer.Stop();
                isAnimating = false;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (IsDisposed) return;

            try
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var easedProgress = EaseInOutCubic(animationProgress);
                var visualState = isAnimating ?
                    (animationProgress > 0.5 ? targetCheckedState : currentCheckedState) :
                    currentCheckedState;

                DrawBackground(g, visualState, easedProgress);
                DrawButton(g, easedProgress);
            }
            catch { /* 忽略绘制异常 */ }
        }

        private void DrawBackground(Graphics g, bool visualState, double easedProgress)
        {
            using var bgPath = CreateRoundedRect(0, 0, WidthPx, HeightPx, RadiusPx);
            Color topColor, middleColor, bottomColor;

            if (visualState)
            {
                topColor = Color.FromArgb(255, 195, 0);
                middleColor = Color.FromArgb(255, 141, 26);
                bottomColor = Color.FromArgb(255, 195, 0);
            }
            else
            {
                topColor = Color.FromArgb(200, 200, 200);
                middleColor = Color.FromArgb(150, 150, 150);
                bottomColor = Color.FromArgb(200, 200, 200);
            }

            if (isAnimating)
            {
                var targetColors = GetTargetColors(targetCheckedState);
                topColor = InterpolateColor(topColor, targetColors.Top, easedProgress);
                middleColor = InterpolateColor(middleColor, targetColors.Middle, easedProgress);
                bottomColor = InterpolateColor(bottomColor, targetColors.Bottom, easedProgress);
            }

            using var bgBrush = new LinearGradientBrush(new Rectangle(0, 0, WidthPx, HeightPx),
                Color.Empty, Color.Empty, 90f);
            bgBrush.InterpolationColors = new ColorBlend
            {
                Colors = new[] { topColor, middleColor, bottomColor },
                Positions = new[] { 0f, 0.5f, 1f }
            };
            g.FillPath(bgBrush, bgPath);
        }

        private void DrawButton(Graphics g, double easedProgress)
        {
            var startX = currentCheckedState ? (WidthPx - HeightPx + PaddingPx) : PaddingPx;
            var endX = targetCheckedState ? (WidthPx - HeightPx + PaddingPx) : PaddingPx;
            var buttonX = (int)(startX + (endX - startX) * easedProgress);
            var buttonY = PaddingPx;

            for (var i = 3; i > 0; i--)
            {
                var shadowSize = i * 2;
                var shadowOffset = i;
                using var shadowPath = CreateRoundedRect(
                    buttonX - shadowSize / 2 + shadowOffset / 2,
                    buttonY - shadowSize / 2 + shadowOffset,
                    ButtonSizePx + shadowSize,
                    ButtonSizePx + shadowSize,
                    (ButtonSizePx + shadowSize) / 2);
                using var shadowBrush = new SolidBrush(Color.FromArgb(20 / i, 0, 0, 0));
                g.FillPath(shadowBrush, shadowPath);
            }

            using (var buttonPath = CreateRoundedRect(buttonX, buttonY, ButtonSizePx, ButtonSizePx, ButtonSizePx / 2))
            using (var buttonBrush = new SolidBrush(Color.White))
            {
                g.FillPath(buttonBrush, buttonPath);
            }

            using var highlightPath = CreateRoundedRect(buttonX + 2, buttonY + 2, ButtonSizePx / 2, ButtonSizePx / 2, ButtonSizePx / 4);
            using var highlightBrush = new SolidBrush(Color.FromArgb(100, 255, 255, 255));
            g.FillPath(highlightBrush, highlightPath);
        }

        private (Color Top, Color Middle, Color Bottom) GetTargetColors(bool targetState)
        {
            if (targetState)
                return (Color.FromArgb(255, 195, 0), Color.FromArgb(255, 141, 26), Color.FromArgb(255, 195, 0));
            else
                return (Color.FromArgb(200, 200, 200), Color.FromArgb(150, 150, 150), Color.FromArgb(200, 200, 200));
        }

        private static double EaseInOutCubic(double t)
        {
            return t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;
        }

        private static Color InterpolateColor(Color start, Color end, double progress)
        {
            return Color.FromArgb(
                (int)(start.R + (end.R - start.R) * progress),
                (int)(start.G + (end.G - start.G) * progress),
                (int)(start.B + (end.B - start.B) * progress));
        }

        private static GraphicsPath CreateRoundedRect(float x, float y, float width, float height, float radius)
        {
            var path = new GraphicsPath();
            path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
            path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);
            path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            Invalidate();
        }

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