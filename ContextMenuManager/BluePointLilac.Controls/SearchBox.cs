using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using BluePointLilac.Methods;
using Svg;

namespace BluePointLilac.Controls
{
    public class ModernSearchBox : Panel
    {
        private TextBox searchTextBox;
        private bool isFocused = false;
        private bool isMouseOverIcon = false;
        private int borderRadius = 14;
        private Color borderColor, hoverBorderColor, focusBorderColor, backgroundColor, textColor;
        private Rectangle iconRect;
        private Bitmap cachedIconBitmap;
        private Color lastIconColor = Color.Empty;

        // 动画相关变量
        private Timer animationTimer;
        private float currentBorderWidth = 1.2f;
        private float targetBorderWidth = 1.2f;
        private Color currentBorderColor;
        private Color targetBorderColor;
        private float iconScale = 1.0f;
        private float targetIconScale = 1.0f;
        private float focusGlowAlpha = 0f;
        private float targetFocusGlowAlpha = 0f;

        private readonly Color orangePrimary = Color.FromArgb(255, 107, 0);
        private readonly Color orangeLight = Color.FromArgb(255, 145, 60);
        private readonly Color orangeDark = Color.FromArgb(220, 85, 0);
        private readonly Color subtleShadow = Color.FromArgb(15, 0, 0, 0);
        private readonly Color focusGlowColor = Color.FromArgb(80, 255, 145, 60);

        public event EventHandler SearchPerformed;

        public ModernSearchBox()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.ResizeRedraw |
                     ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);

            InitializeComponent();
            InitializeAnimation();
            UpdateIconRect();
            Application.AddMessageFilter(new GlobalMouseMessageFilter(this));
        }

        public string SearchText
        {
            get => searchTextBox.Text;
            set => searchTextBox.Text = value;
        }

        private void InitializeAnimation()
        {
            animationTimer = new Timer
            {
                Interval = 16 // ~60 FPS
            };
            animationTimer.Tick += (s, e) => UpdateAnimation();
            animationTimer.Start();

            // 初始化动画状态
            currentBorderColor = borderColor;
            targetBorderColor = borderColor;
        }

        private void UpdateAnimation()
        {
            bool needsInvalidate = false;

            // 边框宽度动画
            if (Math.Abs(currentBorderWidth - targetBorderWidth) > 0.01f)
            {
                currentBorderWidth = Lerp(currentBorderWidth, targetBorderWidth, 0.3f);
                needsInvalidate = true;
            }

            // 边框颜色动画
            if (currentBorderColor != targetBorderColor)
            {
                currentBorderColor = ColorLerp(currentBorderColor, targetBorderColor, 0.25f);
                needsInvalidate = true;
            }

            // 图标缩放动画
            if (Math.Abs(iconScale - targetIconScale) > 0.01f)
            {
                iconScale = Lerp(iconScale, targetIconScale, 0.4f);
                needsInvalidate = true;
            }

            // 焦点光晕动画
            if (Math.Abs(focusGlowAlpha - targetFocusGlowAlpha) > 0.01f)
            {
                focusGlowAlpha = Lerp(focusGlowAlpha, targetFocusGlowAlpha, 0.2f);
                needsInvalidate = true;
            }

            if (needsInvalidate)
            {
                Invalidate();
            }
        }

        private float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        private Color ColorLerp(Color color1, Color color2, float t)
        {
            var r = (int)(color1.R + (color2.R - color1.R) * t);
            var g = (int)(color1.G + (color2.G - color1.G) * t);
            var b = (int)(color1.B + (color2.B - color1.B) * t);
            var a = (int)(color1.A + (color2.A - color1.A) * t);
            return Color.FromArgb(a, r, g, b);
        }

        private void InitializeComponent()
        {
            Size = new Size(260.DpiZoom(), 40.DpiZoom());

            searchTextBox = new TextBox
            {
                Location = new Point(16.DpiZoom(), 10.DpiZoom()),
                Size = new Size(200.DpiZoom(), 22.DpiZoom()),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                PlaceholderText = "搜索内容...",
                BorderStyle = BorderStyle.None
            };

            searchTextBox.SetStyle(ControlStyles.SupportsTransparentBackColor |
                                 ControlStyles.OptimizedDoubleBuffer |
                                 ControlStyles.AllPaintingInWmPaint, true);

            searchTextBox.GotFocus += (s, e) =>
            {
                isFocused = true;
                UpdateAnimationTargets();
                Invalidate();
            };
            searchTextBox.LostFocus += (s, e) =>
            {
                isFocused = false;
                UpdateAnimationTargets();
                Invalidate();
            };
            searchTextBox.TextChanged += (s, e) => Invalidate();
            searchTextBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    PerformSearchWithAnimation();
                    e.Handled = e.SuppressKeyPress = true;
                }
            };

            Controls.Add(searchTextBox);

            MouseEnter += (s, e) =>
            {
                UpdateAnimationTargets();
                Invalidate();
            };
            MouseLeave += (s, e) =>
            {
                isMouseOverIcon = false;
                UpdateAnimationTargets();
                Invalidate();
            };
            MouseMove += (s, e) =>
            {
                bool wasOverIcon = isMouseOverIcon;
                isMouseOverIcon = iconRect.Contains(e.Location);
                if (wasOverIcon != isMouseOverIcon)
                {
                    Cursor = isMouseOverIcon ? Cursors.Hand : Cursors.Default;
                    UpdateAnimationTargets();
                    Invalidate();
                }
            };
            MouseClick += (s, e) =>
            {
                if (iconRect.Contains(e.Location) && e.Button == MouseButtons.Left)
                    PerformSearchWithAnimation();
            };

            searchTextBox.MouseEnter += (s, e) =>
            {
                UpdateAnimationTargets();
                Invalidate();
            };
            searchTextBox.MouseLeave += (s, e) =>
            {
                UpdateAnimationTargets();
                Invalidate();
            };

            InitializeColors();
        }

        private void UpdateAnimationTargets()
        {
            // 更新边框宽度目标值
            if (isFocused)
            {
                targetBorderWidth = 2.2f;
                targetFocusGlowAlpha = 1.0f;
            }
            else if (ClientRectangle.Contains(PointToClient(MousePosition)) || isMouseOverIcon)
            {
                targetBorderWidth = 1.8f;
                targetFocusGlowAlpha = 0f;
            }
            else
            {
                targetBorderWidth = 1.2f;
                targetFocusGlowAlpha = 0f;
            }

            // 更新边框颜色目标值
            if (isFocused)
            {
                targetBorderColor = focusBorderColor;
            }
            else if (ClientRectangle.Contains(PointToClient(MousePosition)) || isMouseOverIcon)
            {
                targetBorderColor = hoverBorderColor;
            }
            else
            {
                targetBorderColor = borderColor;
            }

            // 更新图标缩放目标值
            if (isMouseOverIcon)
            {
                targetIconScale = 1.1f;
            }
            else
            {
                targetIconScale = 1.0f;
            }
        }

        private void InitializeColors()
        {
            if (MyMainForm.IsDarkTheme())
            {
                backgroundColor = Color.FromArgb(45, 45, 48);
                textColor = Color.FromArgb(245, 245, 245);
                borderColor = Color.FromArgb(70, 70, 75);
                hoverBorderColor = orangeLight;
                focusBorderColor = orangePrimary;
            }
            else
            {
                backgroundColor = Color.FromArgb(250, 250, 252);
                textColor = Color.FromArgb(25, 25, 25);
                borderColor = Color.FromArgb(210, 210, 215);
                hoverBorderColor = orangeLight;
                focusBorderColor = orangePrimary;
            }

            if (searchTextBox != null)
            {
                searchTextBox.ForeColor = textColor;
                searchTextBox.BackColor = backgroundColor;
            }

            // 初始化动画颜色
            currentBorderColor = borderColor;
            targetBorderColor = borderColor;
        }

        private void UpdateIconRect()
        {
            int iconSize = (int)(18.DpiZoom() * iconScale);
            int margin = 12.DpiZoom();
            int y = (Height - iconSize) / 2;
            int x = Math.Max(margin, Width - iconSize - margin);
            iconRect = new Rectangle(x, y, iconSize, iconSize);
        }

        private void PerformSearch() => SearchPerformed?.Invoke(this, EventArgs.Empty);

        private async void PerformSearchWithAnimation()
        {
            // 图标点击动画
            targetIconScale = 0.8f;
            await System.Threading.Tasks.Task.Delay(100);
            targetIconScale = 1.0f;

            PerformSearch();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            if (!MyMainForm.IsDarkTheme())
            {
                using (var shadowPath = CreateRoundedRectanglePath(new Rectangle(1, 2, Width - 2, Height - 2), borderRadius.DpiZoom()))
                using (var shadowBrush = new SolidBrush(subtleShadow))
                    g.FillPath(shadowBrush, shadowPath);
            }

            // 绘制焦点光晕效果
            if (focusGlowAlpha > 0.01f)
            {
                using (var glowPath = CreateRoundedRectanglePath(new Rectangle(-2, -2, Width + 3, Height + 3), (borderRadius + 2).DpiZoom()))
                using (var glowBrush = new SolidBrush(Color.FromArgb((int)(focusGlowAlpha * 80), focusGlowColor)))
                {
                    g.FillPath(glowBrush, glowPath);
                }
            }

            var drawRect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = CreateRoundedRectanglePath(drawRect, borderRadius.DpiZoom()))
            {
                FillGradientBackground(g, path);
                DrawRefinedBorder(g, path, currentBorderColor, currentBorderWidth);
            }

            DrawSearchIcon(g, iconRect);
        }

        private void FillGradientBackground(Graphics g, GraphicsPath path)
        {
            var rect = path.GetBounds();
            Color color1, color2;

            if (MyMainForm.IsDarkTheme())
            {
                color1 = Color.FromArgb(50, 50, 53);
                color2 = Color.FromArgb(40, 40, 43);
            }
            else
            {
                color1 = Color.FromArgb(253, 253, 255);
                color2 = Color.FromArgb(247, 247, 249);
            }

            using (var brush = new LinearGradientBrush(new PointF(rect.X, rect.Y), new PointF(rect.X, rect.Bottom), color1, color2))
            {
                var blend = new ColorBlend
                {
                    Positions = new[] { 0f, 0.5f, 1f },
                    Colors = new[] { color1, Color.FromArgb((color1.R + color2.R) / 2, (color1.G + color2.G) / 2, (color1.B + color2.B) / 2), color2 }
                };
                brush.InterpolationColors = blend;
                g.FillPath(brush, path);
            }
        }

        private void DrawRefinedBorder(Graphics g, GraphicsPath path, Color borderColor, float borderWidth)
        {
            using (var pen = new Pen(borderColor, borderWidth))
            {
                pen.Alignment = PenAlignment.Center;
                pen.LineJoin = LineJoin.Round;
                g.DrawPath(pen, path);
            }
        }

        private void DrawSearchIcon(Graphics g, Rectangle iconRect)
        {
            if (iconRect.Right > Width || iconRect.Bottom > Height || iconRect.Width <= 0 || iconRect.Height <= 0)
                UpdateIconRect();

            var iconColor = isMouseOverIcon ? orangeDark :
                           isFocused ? focusBorderColor :
                           ClientRectangle.Contains(PointToClient(MousePosition)) ? hoverBorderColor :
                           borderColor;

            if (cachedIconBitmap == null || lastIconColor != iconColor ||
                cachedIconBitmap?.Width != iconRect.Width || cachedIconBitmap?.Height != iconRect.Height)
            {
                cachedIconBitmap?.Dispose();
                cachedIconBitmap = GenerateModernSearchIcon(iconRect.Size, iconColor);
                lastIconColor = iconColor;
            }

            if (cachedIconBitmap != null)
            {
                // 应用图标缩放变换
                var scaledRect = new Rectangle(
                    iconRect.X + (int)(iconRect.Width * (1 - iconScale) / 2),
                    iconRect.Y + (int)(iconRect.Height * (1 - iconScale) / 2),
                    (int)(iconRect.Width * iconScale),
                    (int)(iconRect.Height * iconScale)
                );
                g.DrawImage(cachedIconBitmap, scaledRect);
            }
        }

        private Bitmap GenerateModernSearchIcon(Size size, Color color)
        {
            try
            {
                if (size.Width <= 0 || size.Height <= 0)
                    size = new Size(18.DpiZoom(), 18.DpiZoom());

                var svgDocument = new SvgDocument
                {
                    Width = size.Width,
                    Height = size.Height,
                    ViewBox = new SvgViewBox(0, 0, 24, 24)
                };

                var searchPath = new SvgPath
                {
                    PathData = SvgPathBuilder.Parse("M15.5 14h-.79l-.28-.27A6.471 6.471 0 0 0 16 9.5 6.5 6.5 0 1 0 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"),
                    Fill = new SvgColourServer(Color.Transparent),
                    Stroke = new SvgColourServer(color),
                    StrokeWidth = 1.8f,
                    StrokeLineCap = SvgStrokeLineCap.Round,
                    StrokeLineJoin = SvgStrokeLineJoin.Round
                };

                svgDocument.Children.Add(searchPath);
                return svgDocument.Draw(size.Width, size.Height);
            }
            catch
            {
                return GenerateModernFallbackIcon(size, color);
            }
        }

        private Bitmap GenerateModernFallbackIcon(Size size, Color color)
        {
            if (size.Width < 16) size = new Size(16, size.Height);
            if (size.Height < 16) size = new Size(size.Width, 16);

            var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int margin = Math.Max(2, size.Width / 8);
                int drawSize = Math.Min(size.Width, size.Height) - margin * 2;
                if (drawSize < 8) drawSize = 8;

                int x = (size.Width - drawSize) / 2;
                int y = (size.Height - drawSize) / 2;

                using (var pen = new Pen(color, Math.Max(1.6f, drawSize / 10f)))
                {
                    pen.StartCap = LineCap.Round;
                    int circleDiameter = (int)(drawSize * 0.65f);
                    if (circleDiameter < 6) circleDiameter = 6;

                    int circleX = x + (drawSize - circleDiameter) / 2;
                    int circleY = y + (drawSize - circleDiameter) / 2;

                    RectangleF circleRect = new RectangleF(circleX + pen.Width / 2, circleY + pen.Width / 2,
                                                         circleDiameter - pen.Width, circleDiameter - pen.Width);
                    g.DrawEllipse(pen, circleRect);

                    float angle = 45f;
                    float handleLength = drawSize * 0.35f;
                    if (handleLength < 4) handleLength = 4;

                    double radian = angle * Math.PI / 180;
                    float centerX = circleX + circleDiameter / 2f;
                    float centerY = circleY + circleDiameter / 2f;
                    float radius = circleDiameter / 2f;

                    float startX = centerX + (float)(radius * Math.Cos(radian));
                    float startY = centerY + (float)(radius * Math.Sin(radian));
                    float endX = startX + (float)(handleLength * Math.Cos(radian));
                    float endY = startY + (float)(handleLength * Math.Sin(radian));

                    g.DrawLine(pen, startX, startY, endX, endY);
                }
            }
            return bitmap;
        }

        private GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            float diameter = radius * 2f;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);

            path.CloseFigure();
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                animationTimer?.Stop();
                animationTimer?.Dispose();
                cachedIconBitmap?.Dispose();
            }
            base.Dispose(disposing);
        }

        public void FocusSearch() { searchTextBox?.Focus(); searchTextBox?.SelectAll(); }
        public void ClearSearch() { searchTextBox.Text = string.Empty; }
        public void LoseFocus() { Parent?.Focus(); }
        public TextBox GetTextBox() { return searchTextBox; }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (searchTextBox != null && !searchTextBox.IsDisposed)
            {
                int iconAreaWidth = 50.DpiZoom();
                searchTextBox.Width = Width - iconAreaWidth;
                searchTextBox.Height = 22.DpiZoom();
                int textBoxY = (Height - searchTextBox.Height) / 2;
                searchTextBox.Location = new Point(12.DpiZoom(), textBoxY);
            }

            cachedIconBitmap?.Dispose();
            cachedIconBitmap = null;
            UpdateIconRect();
            Invalidate();
        }

        private class GlobalMouseMessageFilter : IMessageFilter
        {
            private readonly ModernSearchBox searchBox;
            public GlobalMouseMessageFilter(ModernSearchBox searchBox) => this.searchBox = searchBox;

            public bool PreFilterMessage(ref Message m)
            {
                if ((m.Msg == 0x201 || m.Msg == 0x202) && !searchBox.IsMouseInside(searchBox, Control.MousePosition))
                    searchBox.LoseFocus();
                return false;
            }
        }

        private bool IsMouseInside(Control control, Point screenPoint)
        {
            if (control == null || control.IsDisposed) return false;
            Point clientPoint = control.PointToClient(screenPoint);

            if (control.ClientRectangle.Contains(clientPoint)) return true;
            foreach (Control child in control.Controls)
                if (!child.IsDisposed && IsMouseInside(child, screenPoint)) return true;

            return false;
        }
    }
}