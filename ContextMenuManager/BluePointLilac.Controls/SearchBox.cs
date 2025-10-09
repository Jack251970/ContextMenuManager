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

        private readonly Color orangePrimary = Color.FromArgb(255, 107, 0);
        private readonly Color orangeLight = Color.FromArgb(255, 145, 60);
        private readonly Color orangeDark = Color.FromArgb(220, 85, 0);
        private readonly Color subtleShadow = Color.FromArgb(15, 0, 0, 0);

        public event EventHandler SearchPerformed;

        public ModernSearchBox()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.ResizeRedraw |
                     ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);

            InitializeComponent();
            UpdateIconRect();
            Application.AddMessageFilter(new GlobalMouseMessageFilter(this));
        }

        public string SearchText
        {
            get => searchTextBox.Text;
            set => searchTextBox.Text = value;
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

            searchTextBox.GotFocus += (s, e) => { isFocused = true; Invalidate(); };
            searchTextBox.LostFocus += (s, e) => { isFocused = false; Invalidate(); };
            searchTextBox.TextChanged += (s, e) => Invalidate();
            searchTextBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    PerformSearch();
                    e.Handled = e.SuppressKeyPress = true;
                }
            };

            Controls.Add(searchTextBox);

            MouseEnter += (s, e) => Invalidate();
            MouseLeave += (s, e) => { isMouseOverIcon = false; Invalidate(); };
            MouseMove += (s, e) =>
            {
                bool wasOverIcon = isMouseOverIcon;
                isMouseOverIcon = iconRect.Contains(e.Location);
                if (wasOverIcon != isMouseOverIcon)
                {
                    Cursor = isMouseOverIcon ? Cursors.Hand : Cursors.Default;
                    Invalidate();
                }
            };
            MouseClick += (s, e) =>
            {
                if (iconRect.Contains(e.Location) && e.Button == MouseButtons.Left)
                    PerformSearch();
            };

            searchTextBox.MouseEnter += (s, e) => Invalidate();
            searchTextBox.MouseLeave += (s, e) => Invalidate();

            InitializeColors();
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
        }

        private void UpdateIconRect()
        {
            int iconSize = 18.DpiZoom();
            int margin = 12.DpiZoom();
            int y = (Height - iconSize) / 2;
            int x = Math.Max(margin, Width - iconSize - margin);
            iconRect = new Rectangle(x, y, iconSize, iconSize);
        }

        private void PerformSearch() => SearchPerformed?.Invoke(this, EventArgs.Empty);

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

            Color currentBorderColor = borderColor;
            float borderWidth = 1.2f;

            if (isFocused)
            {
                currentBorderColor = focusBorderColor;
                borderWidth = 2.2f;
            }
            else if (ClientRectangle.Contains(PointToClient(MousePosition)) || isMouseOverIcon)
            {
                currentBorderColor = hoverBorderColor;
                borderWidth = 1.8f;
            }

            var drawRect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = CreateRoundedRectanglePath(drawRect, borderRadius.DpiZoom()))
            {
                FillGradientBackground(g, path);
                DrawRefinedBorder(g, path, currentBorderColor, borderWidth);
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
                g.DrawImage(cachedIconBitmap, iconRect);
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
            if (disposing) cachedIconBitmap?.Dispose();
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