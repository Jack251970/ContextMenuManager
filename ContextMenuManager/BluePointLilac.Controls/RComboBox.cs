using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Svg;

namespace BluePointLilac.Controls
{
    public class RComboBox : ComboBox
    {
        private Color borderColor = Color.FromArgb(210, 210, 215);
        private Color hoverBorderColor = Color.FromArgb(255, 145, 60);
        private Color focusBorderColor = Color.FromArgb(255, 107, 0);
        private Color backgroundColor = Color.FromArgb(250, 250, 252);
        private Color arrowColor = Color.FromArgb(100, 100, 100);
        private Color hoverArrowColor = Color.FromArgb(255, 107, 0);
        private Color buttonColor = Color.FromArgb(250, 250, 252);

        private bool isHovered = false;
        private bool isFocused = false;
        private int borderRadius = 8;

        private Bitmap cachedArrowBitmap;
        private Color lastArrowColor = Color.Empty;
        private Rectangle arrowRect;

        private Timer animationTimer;
        private float currentBorderWidth = 1.2f;
        private float targetBorderWidth = 1.2f;
        private Color currentBorderColor;
        private Color targetBorderColor;
        private float arrowScale = 1.0f;
        private float targetArrowScale = 1.0f;

        private bool isPainting = false;

        public RComboBox()
        {
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            DrawMode = DrawMode.OwnerDrawFixed;
            DropDownStyle = ComboBoxStyle.DropDownList;
            IntegralHeight = false; // 防止下拉菜单高度问题

            BackColor = MyMainForm.ButtonMain;
            ForeColor = MyMainForm.FormFore;

            InitializeColors();
            InitializeAnimation();
            UpdateArrowRect();

            // 事件处理
            MouseEnter += (s, e) => { isHovered = true; UpdateAnimationTargets(); Invalidate(); };
            MouseLeave += (s, e) => { isHovered = false; UpdateAnimationTargets(); Invalidate(); };
            GotFocus += (s, e) => { isFocused = true; UpdateAnimationTargets(); Invalidate(); };
            LostFocus += (s, e) => { isFocused = false; UpdateAnimationTargets(); Invalidate(); };

            // 修复下拉菜单显示问题
            DropDown += OnDropDown;
        }

        [DefaultValue(typeof(Color), "210, 210, 215")]
        public Color BorderColor
        {
            get => borderColor;
            set { borderColor = value; Invalidate(); }
        }

        [DefaultValue(typeof(Color), "255, 255, 255")]
        public Color ButtonColor
        {
            get => buttonColor;
            set { buttonColor = backgroundColor = value; Invalidate(); }
        }

        [DefaultValue(typeof(Color), "100, 100, 100")]
        public Color ArrowColor
        {
            get => arrowColor;
            set { arrowColor = value; Invalidate(); }
        }

        [DefaultValue(8)]
        public int BorderRadius
        {
            get => borderRadius;
            set { borderRadius = value; Invalidate(); }
        }

        private void InitializeAnimation()
        {
            animationTimer = new Timer { Interval = 32 };
            animationTimer.Tick += (s, e) => UpdateAnimation();
            animationTimer.Start();
            currentBorderColor = targetBorderColor = borderColor;
        }

        private void UpdateAnimation()
        {
            if (isPainting || !Visible) return;

            bool needsUpdate = false;

            if (Math.Abs(currentBorderWidth - targetBorderWidth) > 0.05f)
            {
                currentBorderWidth += (targetBorderWidth - currentBorderWidth) * 0.2f;
                needsUpdate = true;
            }

            if (currentBorderColor != targetBorderColor)
            {
                currentBorderColor = ColorLerp(currentBorderColor, targetBorderColor, 0.15f);
                needsUpdate = true;
            }

            if (Math.Abs(arrowScale - targetArrowScale) > 0.05f)
            {
                arrowScale += (targetArrowScale - arrowScale) * 0.3f;
                needsUpdate = true;
            }

            if (needsUpdate) Invalidate();
        }

        private Color ColorLerp(Color c1, Color c2, float t) => Color.FromArgb(
            (int)(c1.R + (c2.R - c1.R) * t),
            (int)(c1.G + (c2.G - c1.G) * t),
            (int)(c1.B + (c2.B - c1.B) * t)
        );

        private void UpdateAnimationTargets()
        {
            targetBorderWidth = isFocused ? 2.2f : isHovered ? 1.8f : 1.2f;
            targetBorderColor = isFocused ? focusBorderColor : isHovered ? hoverBorderColor : borderColor;
            targetArrowScale = isHovered ? 1.1f : 1.0f;
        }

        private void InitializeColors()
        {
            borderColor = buttonColor = backgroundColor = MyMainForm.ButtonMain;
            arrowColor = ForeColor = MyMainForm.FormFore;

            if (MyMainForm.IsDarkTheme())
            {
                hoverBorderColor = Color.FromArgb(255, 145, 60);
                focusBorderColor = Color.FromArgb(255, 107, 0);
                hoverArrowColor = Color.FromArgb(255, 107, 0);
            }
            else
            {
                hoverBorderColor = Color.FromArgb(255, 145, 60);
                focusBorderColor = Color.FromArgb(255, 107, 0);
                hoverArrowColor = Color.FromArgb(255, 107, 0);
            }

            currentBorderColor = targetBorderColor = borderColor;
        }

        private void UpdateArrowRect()
        {
            int size = 16, margin = 8;
            arrowRect = new Rectangle(Width - size - margin, (Height - size) / 2, size, size);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (isPainting) return;
            isPainting = true;

            try
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // 清除背景
                using (var brush = new SolidBrush(Parent?.BackColor ?? SystemColors.Control))
                    g.FillRectangle(brush, ClientRectangle);

                // 绘制背景和边框
                using (var path = CreateRoundedRectanglePath(ClientRectangle, borderRadius))
                using (var brush = new SolidBrush(backgroundColor))
                    g.FillPath(brush, path);

                using (var path = CreateRoundedRectanglePath(new Rectangle(0, 0, Width - 1, Height - 1), borderRadius))
                using (var pen = new Pen(currentBorderColor, currentBorderWidth))
                    g.DrawPath(pen, path);

                // 绘制文本
                var text = SelectedItem?.ToString() ?? (Items.Count > 0 ? Items[0].ToString() : "");
                if (!string.IsNullOrEmpty(text))
                {
                    using (var brush = new SolidBrush(ForeColor))
                    {
                        var textRect = new Rectangle(12, 0, Width - arrowRect.Width - 20, Height);
                        var format = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
                        g.DrawString(text, Font, brush, textRect, format);
                    }
                }

                // 绘制箭头
                DrawArrowIcon(g, arrowRect);
            }
            finally { isPainting = false; }
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            var backColor = isSelected ? Color.FromArgb(255, 235, 225) : BackColor;
            var textColor = isSelected ? Color.FromArgb(255, 107, 0) : ForeColor;

            // 绘制背景
            e.Graphics.FillRectangle(new SolidBrush(backColor), e.Bounds);

            // 绘制文本
            using (var brush = new SolidBrush(textColor))
            {
                var textRect = new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 16, e.Bounds.Height);
                var format = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
                e.Graphics.DrawString(GetItemText(Items[e.Index]), Font, brush, textRect, format);
            }

            // 绘制焦点框
            if ((e.State & DrawItemState.Focus) == DrawItemState.Focus)
                e.DrawFocusRectangle();
        }

        private void DrawArrowIcon(Graphics g, Rectangle iconRect)
        {
            if (iconRect.Width <= 0) UpdateArrowRect();

            var color = isHovered || isFocused ? hoverArrowColor : arrowColor;

            if (cachedArrowBitmap == null || lastArrowColor != color)
            {
                cachedArrowBitmap?.Dispose();
                cachedArrowBitmap = GenerateArrowIcon(iconRect.Size, color);
                lastArrowColor = color;
            }

            if (cachedArrowBitmap != null)
            {
                var rect = new Rectangle(
                    iconRect.X + (int)(iconRect.Width * (1 - arrowScale) / 2),
                    iconRect.Y + (int)(iconRect.Height * (1 - arrowScale) / 2),
                    (int)(iconRect.Width * arrowScale),
                    (int)(iconRect.Height * arrowScale)
                );
                g.DrawImage(cachedArrowBitmap, rect);
            }
        }

        private Bitmap GenerateArrowIcon(Size size, Color color)
        {
            try
            {
                var svg = new SvgDocument { Width = size.Width, Height = size.Height, ViewBox = new SvgViewBox(0, 0, 24, 24) };
                svg.Children.Add(new SvgPath
                {
                    PathData = SvgPathBuilder.Parse("M7 10l5 5 5-5z"),
                    Fill = new SvgColourServer(color)
                });
                return svg.Draw(size.Width, size.Height);
            }
            catch
            {
                return GenerateFallbackArrowIcon(size, color);
            }
        }

        private Bitmap GenerateFallbackArrowIcon(Size size, Color color)
        {
            var bmp = new Bitmap(size.Width, size.Height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var points = new[] {
                    new Point(size.Width / 4, size.Height / 3),
                    new Point(size.Width * 3 / 4, size.Height / 3),
                    new Point(size.Width / 2, size.Height * 2 / 3)
                };
                g.FillPolygon(new SolidBrush(color), points);
            }
            return bmp;
        }

        private GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            float diam = radius * 2f;
            if (diam > rect.Width) diam = rect.Width;
            if (diam > rect.Height) diam = rect.Height;

            path.AddArc(rect.X, rect.Y, diam, diam, 180, 90);
            path.AddArc(rect.Right - diam, rect.Y, diam, diam, 270, 90);
            path.AddArc(rect.Right - diam, rect.Bottom - diam, diam, diam, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diam, diam, diam, 90, 90);
            path.CloseFigure();
            return path;
        }

        // 修复下拉菜单显示问题的关键方法
        private void OnDropDown(object sender, EventArgs e)
        {
            // 确保下拉列表正常显示
            BeginInvoke(new Action(() => {
                if (DroppedDown)
                {
                    // 强制重绘以确保下拉列表正确显示
                    Invalidate();
                }
            }));
        }

        // 重写 WndProc 以确保鼠标事件正确处理
        protected override void WndProc(ref Message m)
        {
            const int WM_LBUTTONDOWN = 0x201;
            const int WM_LBUTTONUP = 0x202;

            if (m.Msg == WM_LBUTTONDOWN)
            {
                // 确保点击时下拉列表正常显示
                if (!DroppedDown)
                {
                    DroppedDown = true;
                    return;
                }
            }
            else if (m.Msg == WM_LBUTTONUP)
            {
                // 允许正常的鼠标释放处理
                base.WndProc(ref m);
                return;
            }

            base.WndProc(ref m);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateArrowRect();
            cachedArrowBitmap?.Dispose();
            cachedArrowBitmap = null;
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                animationTimer?.Stop();
                animationTimer?.Dispose();
                cachedArrowBitmap?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}