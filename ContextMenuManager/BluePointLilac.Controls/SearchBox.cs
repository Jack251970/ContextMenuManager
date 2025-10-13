using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using Svg;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public class ModernSearchBox : Panel
    {
        private TextBox txtSearch;
        private bool focused, mouseOverIcon;
        private Bitmap cachedIcon;
        private Timer animTimer;
        private Rectangle iconRect;
        private Color lastIconColor = Color.Empty;
        private float borderWidth = 1.2f, targetWidth = 1.2f, iconScale = 1f, targetScale = 1f, glowAlpha, targetGlow;
        private Color borderColor, hoverColor, focusColor, bgColor, textColor, currentBorder, targetBorder;
        private readonly int borderRadius = 14;
        private readonly Color orange = Color.FromArgb(255, 107, 0), orangeL = Color.FromArgb(255, 145, 60), orangeD = Color.FromArgb(220, 85, 0);

        public event EventHandler SearchPerformed;
        public string SearchText { get => txtSearch.Text; set => txtSearch.Text = value; }

        public ModernSearchBox()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            InitComponent();
            InitAnimation();
            UpdateIconRect();
            Application.AddMessageFilter(new MouseFilter(this));
        }

        private void InitComponent()
        {
            Size = new Size(260.DpiZoom(), 40.DpiZoom());
            txtSearch = new TextBox
            {
                Location = new Point(16.DpiZoom(), 10.DpiZoom()),
                Size = new Size(200.DpiZoom(), 22.DpiZoom()),
                Font = new Font("Segoe UI", 9.5F),
                PlaceholderText = AppString.Other.SearchContent,
                BorderStyle = BorderStyle.None
            };
            txtSearch.SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            txtSearch.GotFocus += (s, e) => { focused = true; UpdateState(); };
            txtSearch.LostFocus += (s, e) => { focused = false; UpdateState(); };
            txtSearch.TextChanged += (s, e) => Invalidate();
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { SearchWithAnim(); e.Handled = e.SuppressKeyPress = true; } };

            Controls.Add(txtSearch);

            MouseEnter += (s, e) => UpdateState();
            MouseLeave += (s, e) => { mouseOverIcon = false; UpdateState(); };
            MouseMove += (s, e) => {
                bool was = mouseOverIcon;
                mouseOverIcon = iconRect.Contains(e.Location);
                if (was != mouseOverIcon) { Cursor = mouseOverIcon ? Cursors.Hand : Cursors.Default; UpdateState(); }
            };
            MouseClick += (s, e) => { if (iconRect.Contains(e.Location)) SearchWithAnim(); };

            txtSearch.MouseEnter += (s, e) => UpdateState();
            txtSearch.MouseLeave += (s, e) => UpdateState();

            InitColors();
        }

        private void InitAnimation()
        {
            animTimer = new Timer { Interval = 16 };
            animTimer.Tick += (s, e) => {
                bool update = false;
                if (Math.Abs(borderWidth - targetWidth) > 0.01f) { borderWidth = Lerp(borderWidth, targetWidth, 0.3f); update = true; }
                if (currentBorder != targetBorder) { currentBorder = ColorLerp(currentBorder, targetBorder, 0.25f); update = true; }
                if (Math.Abs(iconScale - targetScale) > 0.01f) { iconScale = Lerp(iconScale, targetScale, 0.4f); update = true; }
                if (Math.Abs(glowAlpha - targetGlow) > 0.01f) { glowAlpha = Lerp(glowAlpha, targetGlow, 0.2f); update = true; }
                if (update) Invalidate();
            };
            animTimer.Start();
            currentBorder = targetBorder = borderColor;
        }

        private float Lerp(float a, float b, float t) => a + (b - a) * t;
        private Color ColorLerp(Color c1, Color c2, float t) => Color.FromArgb((int)(c1.A + (c2.A - c1.A) * t), (int)(c1.R + (c2.R - c1.R) * t), (int)(c1.G + (c2.G - c1.G) * t), (int)(c1.B + (c2.B - c1.B) * t));

        private void InitColors()
        {
            bool dark = MyMainForm.IsDarkTheme();
            if (dark)
            {
                bgColor = Color.FromArgb(45, 45, 48); textColor = Color.FromArgb(245, 245, 245);
                borderColor = Color.FromArgb(70, 70, 75); hoverColor = orangeL; focusColor = orange;
            }
            else
            {
                bgColor = Color.FromArgb(250, 250, 252); textColor = Color.FromArgb(25, 25, 25);
                borderColor = Color.FromArgb(210, 210, 215); hoverColor = orangeL; focusColor = orange;
            }
            if (txtSearch != null) { txtSearch.ForeColor = textColor; txtSearch.BackColor = bgColor; }
            currentBorder = targetBorder = borderColor;
        }

        private void UpdateState()
        {
            if (focused) { targetWidth = 2.2f; targetGlow = 1f; targetBorder = focusColor; }
            else if (ClientRectangle.Contains(PointToClient(MousePosition)) || mouseOverIcon) { targetWidth = 1.8f; targetGlow = 0f; targetBorder = hoverColor; }
            else { targetWidth = 1.2f; targetGlow = 0f; targetBorder = borderColor; }
            targetScale = mouseOverIcon ? 1.1f : 1f;
            Invalidate();
        }

        private void UpdateIconRect()
        {
            int size = (int)(18.DpiZoom() * iconScale);
            int m = 12.DpiZoom();
            int y = (Height - size) / 2;
            int x = Math.Max(m, Width - size - m);
            iconRect = new Rectangle(x, y, size, size);
        }

        private void SearchWithAnim() { targetScale = 0.8f; System.Threading.Tasks.Task.Delay(100).ContinueWith(_ => { targetScale = 1f; SearchPerformed?.Invoke(this, EventArgs.Empty); }); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (!MyMainForm.IsDarkTheme())
                using (var p = CreateRoundPath(new Rectangle(1, 2, Width - 2, Height - 2), borderRadius.DpiZoom()))
                using (var b = new SolidBrush(Color.FromArgb(15, 0, 0, 0)))
                    g.FillPath(b, p);

            if (glowAlpha > 0.01f)
                using (var p = CreateRoundPath(new Rectangle(-2, -2, Width + 3, Height + 3), (borderRadius + 2).DpiZoom()))
                using (var b = new SolidBrush(Color.FromArgb((int)(glowAlpha * 80), 255, 145, 60)))
                    g.FillPath(b, p);

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = CreateRoundPath(rect, borderRadius.DpiZoom());
            FillGradient(g, path);
            DrawBorder(g, path, currentBorder, borderWidth);
            DrawIcon(g, iconRect);
        }

        private void FillGradient(Graphics g, GraphicsPath path)
        {
            var r = path.GetBounds();
            Color c1, c2;
            if (MyMainForm.IsDarkTheme()) { c1 = Color.FromArgb(50, 50, 53); c2 = Color.FromArgb(40, 40, 43); }
            else { c1 = Color.FromArgb(253, 253, 255); c2 = Color.FromArgb(247, 247, 249); }

            using var brush = new LinearGradientBrush(new PointF(r.X, r.Y), new PointF(r.X, r.Bottom), c1, c2);
            var blend = new ColorBlend { Positions = new[] { 0f, 0.5f, 1f }, Colors = new[] { c1, Color.FromArgb((c1.R + c2.R) / 2, (c1.G + c2.G) / 2, (c1.B + c2.B) / 2), c2 } };
            brush.InterpolationColors = blend;
            g.FillPath(brush, path);
        }

        private void DrawBorder(Graphics g, GraphicsPath path, Color color, float width)
        {
            using var pen = new Pen(color, width) { Alignment = PenAlignment.Center, LineJoin = LineJoin.Round };
            g.DrawPath(pen, path);
        }

        private void DrawIcon(Graphics g, Rectangle rect)
        {
            if (rect.Right > Width || rect.Bottom > Height) UpdateIconRect();
            var iconColor = mouseOverIcon ? orangeD : focused ? focusColor : ClientRectangle.Contains(PointToClient(MousePosition)) ? hoverColor : borderColor;

            if (cachedIcon == null || lastIconColor != iconColor || cachedIcon?.Width != rect.Width || cachedIcon?.Height != rect.Height)
            {
                cachedIcon?.Dispose();
                cachedIcon = CreateIcon(rect.Size, iconColor);
                lastIconColor = iconColor;
            }

            if (cachedIcon != null)
            {
                var scaled = new Rectangle(rect.X + (int)(rect.Width * (1 - iconScale) / 2), rect.Y + (int)(rect.Height * (1 - iconScale) / 2), (int)(rect.Width * iconScale), (int)(rect.Height * iconScale));
                g.DrawImage(cachedIcon, scaled);
            }
        }

        private Bitmap CreateIcon(Size size, Color color)
        {
            try
            {
                if (size.Width <= 0) size = new Size(18.DpiZoom(), 18.DpiZoom());
                var svg = new SvgDocument { Width = size.Width, Height = size.Height, ViewBox = new SvgViewBox(0, 0, 24, 24) };
                var path = new SvgPath
                {
                    PathData = SvgPathBuilder.Parse("M15.5 14h-.79l-.28-.27A6.471 6.471 0 0 0 16 9.5 6.5 6.5 0 1 0 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"),
                    Fill = new SvgColourServer(Color.Transparent),
                    Stroke = new SvgColourServer(color),
                    StrokeWidth = 1.8f,
                    StrokeLineCap = SvgStrokeLineCap.Round,
                    StrokeLineJoin = SvgStrokeLineJoin.Round
                };
                svg.Children.Add(path);
                return svg.Draw(size.Width, size.Height);
            }
            catch { return CreateFallbackIcon(size, color); }
        }

        private Bitmap CreateFallbackIcon(Size size, Color color)
        {
            if (size.Width < 16) size = new Size(16, size.Height);
            var bmp = new Bitmap(size.Width, size.Height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int m = Math.Max(2, size.Width / 8);
                int s = Math.Max(8, Math.Min(size.Width, size.Height) - m * 2);
                int x = (size.Width - s) / 2, y = (size.Height - s) / 2;
                using var pen = new Pen(color, Math.Max(1.6f, s / 10f)) { StartCap = LineCap.Round };
                int d = Math.Max(6, (int)(s * 0.65f));
                int cx = x + (s - d) / 2, cy = y + (s - d) / 2;
                g.DrawEllipse(pen, cx + pen.Width / 2, cy + pen.Width / 2, d - pen.Width, d - pen.Width);
                double rad = 45 * Math.PI / 180;
                float centerX = cx + d / 2f, centerY = cy + d / 2f, r = d / 2f;
                float sx = centerX + (float)(r * Math.Cos(rad)), sy = centerY + (float)(r * Math.Sin(rad));
                float ex = sx + (float)(Math.Max(4, s * 0.35f) * Math.Cos(rad)), ey = sy + (float)(Math.Max(4, s * 0.35f) * Math.Sin(rad));
                g.DrawLine(pen, sx, sy, ex, ey);
            }
            return bmp;
        }

        private GraphicsPath CreateRoundPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2f;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { animTimer?.Stop(); animTimer?.Dispose(); cachedIcon?.Dispose(); }
            base.Dispose(disposing);
        }

        public void FocusSearch() { txtSearch?.Focus(); txtSearch?.SelectAll(); }
        public void ClearSearch() { txtSearch.Text = string.Empty; }
        public void LoseFocus() { Parent?.Focus(); }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (txtSearch != null && !txtSearch.IsDisposed)
            {
                int iconArea = 50.DpiZoom();
                txtSearch.Width = Width - iconArea;
                txtSearch.Height = 22.DpiZoom();
                txtSearch.Location = new Point(12.DpiZoom(), (Height - txtSearch.Height) / 2);
            }
            cachedIcon?.Dispose(); cachedIcon = null;
            UpdateIconRect(); Invalidate();
        }

        private class MouseFilter : IMessageFilter
        {
            private readonly ModernSearchBox box;
            public MouseFilter(ModernSearchBox box) => this.box = box;
            public bool PreFilterMessage(ref Message m)
            {
                if ((m.Msg == 0x201 || m.Msg == 0x202) && !IsMouseIn(box, Control.MousePosition)) box.LoseFocus();
                return false;
            }
            private bool IsMouseIn(Control c, Point p)
            {
                if (c == null || c.IsDisposed) return false;
                Point client = c.PointToClient(p);
                if (c.ClientRectangle.Contains(client)) return true;
                foreach (Control child in c.Controls) if (!child.IsDisposed && IsMouseIn(child, p)) return true;
                return false;
            }
        }
    }
}