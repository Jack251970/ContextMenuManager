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
        private int minWidth = 120; // 最小宽度
        private int maxWidth = 400; // 最大宽度
        private int padding = 50;   // 文本两侧的额外空间（箭头+边距）

        public RComboBox()
        {
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            DrawMode = DrawMode.OwnerDrawFixed;
            DropDownStyle = ComboBoxStyle.DropDownList;
            IntegralHeight = false;

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

            // 文本变化时调整宽度
            SelectedIndexChanged += (s, e) => AdjustWidth();
            TextChanged += (s, e) => AdjustWidth();

            DropDown += OnDropDown;

            // 在句柄创建后调整宽度
            HandleCreated += (s, e) => AdjustWidth();
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

        [DefaultValue(120)]
        public int MinWidth
        {
            get => minWidth;
            set { minWidth = value; AdjustWidth(); }
        }

        [DefaultValue(400)]
        public int MaxWidth
        {
            get => maxWidth;
            set { maxWidth = value; AdjustWidth(); }
        }

        [DefaultValue(50)]
        public int TextPadding
        {
            get => padding;
            set { padding = value; AdjustWidth(); }
        }

        // 修复：移除对 Items 属性的重写，改用事件监听
        public new void AddItem(object item)
        {
            base.Items.Add(item);
            AdjustWidth();
        }

        public new void AddRange(object[] items)
        {
            base.Items.AddRange(items);
            AdjustWidth();
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

        // 自适应宽度方法
        private void AdjustWidth()
        {
            // 检查控件是否已经创建了窗口句柄
            if (Parent == null || isPainting || !IsHandleCreated) return;

            using (var g = CreateGraphics())
            {
                // 计算当前选中文本的宽度
                string currentText = SelectedItem?.ToString() ?? Text;
                float textWidth = 0;

                if (!string.IsNullOrEmpty(currentText))
                {
                    var size = g.MeasureString(currentText, Font);
                    textWidth = size.Width;
                }

                // 计算所有项目中最大文本宽度
                float maxTextWidth = textWidth;
                // 修复：使用 base.Items 而不是 Items
                foreach (var item in base.Items)
                {
                    string itemText = item.ToString();
                    var itemSize = g.MeasureString(itemText, Font);
                    if (itemSize.Width > maxTextWidth)
                        maxTextWidth = itemSize.Width;
                }

                // 计算新宽度（文本宽度 + 箭头空间 + 边距）
                int newWidth = (int)Math.Ceiling(maxTextWidth) + padding;

                // 限制在最小和最大宽度之间
                newWidth = Math.Max(minWidth, Math.Min(maxWidth, newWidth));

                // 如果宽度有变化，更新控件宽度
                if (Width != newWidth)
                {
                    Width = newWidth;
                    UpdateArrowRect();
                    Invalidate();

                    // 通知父容器可能需要重新布局
                    Parent?.PerformLayout();
                }
            }
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
                // 修复：使用 base.Items 而不是 Items
                var text = SelectedItem?.ToString() ?? (base.Items.Count > 0 ? base.Items[0].ToString() : "");
                if (!string.IsNullOrEmpty(text))
                {
                    using (var brush = new SolidBrush(ForeColor))
                    {
                        var textRect = new Rectangle(12, 0, Width - arrowRect.Width - 20, Height);
                        var format = new StringFormat
                        {
                            LineAlignment = StringAlignment.Center,
                            Trimming = StringTrimming.EllipsisCharacter,
                            FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.NoClip
                        };

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
                var format = new StringFormat
                {
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.NoClip
                };
                // 修复：使用 base.Items 而不是 Items
                e.Graphics.DrawString(GetItemText(base.Items[e.Index]), Font, brush, textRect, format);
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

        private void OnDropDown(object sender, EventArgs e)
        {
            // 只有在句柄已创建时才调用 BeginInvoke
            if (IsHandleCreated)
            {
                BeginInvoke(new Action(() => {
                    if (DroppedDown)
                    {
                        Invalidate();
                    }
                }));
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_LBUTTONDOWN = 0x201;
            const int WM_LBUTTONUP = 0x202;

            if (m.Msg == WM_LBUTTONDOWN)
            {
                if (!DroppedDown)
                {
                    DroppedDown = true;
                    return;
                }
            }
            else if (m.Msg == WM_LBUTTONUP)
            {
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

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            // 父容器变化时调整宽度 - 只在句柄已创建时调用
            if (Parent != null && IsHandleCreated)
            {
                AdjustWidth();
            }
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            // 字体变化时重新计算宽度 - 只在句柄已创建时调用
            if (IsHandleCreated)
            {
                AdjustWidth();
            }
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