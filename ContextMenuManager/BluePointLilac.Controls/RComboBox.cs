using BluePointLilac.Methods;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public class RComboBox : ComboBox
    {
        private Color borderColor = Color.FromArgb(210, 210, 215);
        private Color hoverColor = Color.FromArgb(255, 145, 60);
        private Color focusColor = Color.FromArgb(255, 107, 0);
        private Color bgColor = Color.FromArgb(250, 250, 252);
        private Color textColor = Color.FromArgb(25, 25, 25);
        private Color arrowColor = Color.FromArgb(100, 100, 100);

        private bool mouseOverDropDown = false;
        private bool focused = false;
        private Timer animTimer;
        private float borderWidth = 1.2f, targetWidth = 1.2f;
        private Color currentBorder, targetBorder;
        private readonly int borderRadius = 14;

        // 自适应宽度相关属性
        private bool autoSize = true;
        private int minWidth = 120;
        private int maxWidth = 400;
        private int padding = 50; // 文本区域和下拉箭头之间的间距

        [DefaultValue(typeof(Color), "210, 210, 215")]
        public Color BorderColor
        {
            get { return borderColor; }
            set
            {
                if (borderColor != value)
                {
                    borderColor = value;
                    Invalidate();
                }
            }
        }

        [DefaultValue(typeof(Color), "255, 145, 60")]
        public Color HoverColor
        {
            get { return hoverColor; }
            set
            {
                if (hoverColor != value)
                {
                    hoverColor = value;
                    Invalidate();
                }
            }
        }

        [DefaultValue(typeof(Color), "255, 107, 0")]
        public Color FocusColor
        {
            get { return focusColor; }
            set
            {
                if (focusColor != value)
                {
                    focusColor = value;
                    Invalidate();
                }
            }
        }

        [DefaultValue(typeof(Color), "100, 100, 100")]
        public Color ArrowColor
        {
            get { return arrowColor; }
            set
            {
                if (arrowColor != value)
                {
                    arrowColor = value;
                    Invalidate();
                }
            }
        }

        [DefaultValue(true)]
        public bool AutoSize
        {
            get { return autoSize; }
            set
            {
                if (autoSize != value)
                {
                    autoSize = value;
                    if (autoSize) AdjustWidth();
                }
            }
        }

        [DefaultValue(120)]
        public int MinWidth
        {
            get { return minWidth; }
            set
            {
                if (minWidth != value)
                {
                    minWidth = value;
                    if (autoSize) AdjustWidth();
                }
            }
        }

        [DefaultValue(400)]
        public int MaxWidth
        {
            get { return maxWidth; }
            set
            {
                if (maxWidth != value)
                {
                    maxWidth = value;
                    if (autoSize) AdjustWidth();
                }
            }
        }

        [DefaultValue(50)]
        public int Padding
        {
            get { return padding; }
            set
            {
                if (padding != value)
                {
                    padding = value;
                    if (autoSize) AdjustWidth();
                }
            }
        }

        public RComboBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.ResizeRedraw |
                    ControlStyles.OptimizedDoubleBuffer, true);

            DrawMode = DrawMode.OwnerDrawFixed;
            DropDownStyle = ComboBoxStyle.DropDownList;
            FlatStyle = FlatStyle.Flat;

            // 设置合适的高度
            Height = 32.DpiZoom();

            InitAnimation();
            UpdateColors();

            // 监听主题变化
            DarkModeHelper.ThemeChanged += OnThemeChanged;

            // Events for state tracking
            GotFocus += (s, e) => { focused = true; UpdateState(); };
            LostFocus += (s, e) => { focused = false; UpdateState(); };
            MouseEnter += (s, e) => UpdateState();
            MouseLeave += (s, e) => { mouseOverDropDown = false; UpdateState(); };
            MouseMove += (s, e) => UpdateDropDownHoverState(e.Location);

            // 文本变化时调整宽度
            SelectedIndexChanged += (s, e) => { if (autoSize) AdjustWidth(); };
            TextChanged += (s, e) => { if (autoSize) AdjustWidth(); };
        }

        private void InitAnimation()
        {
            animTimer = new Timer { Interval = 16 };
            animTimer.Tick += (s, e) => {
                bool update = false;
                if (Math.Abs(borderWidth - targetWidth) > 0.01f)
                {
                    borderWidth = Lerp(borderWidth, targetWidth, 0.3f);
                    update = true;
                }
                if (currentBorder != targetBorder)
                {
                    currentBorder = ColorLerp(currentBorder, targetBorder, 0.25f);
                    update = true;
                }
                if (update) Invalidate();
            };
            animTimer.Start();
            currentBorder = targetBorder = borderColor;
        }

        private float Lerp(float a, float b, float t) => a + (b - a) * t;

        private Color ColorLerp(Color c1, Color c2, float t) =>
            Color.FromArgb(
                (int)(c1.A + (c2.A - c1.A) * t),
                (int)(c1.R + (c2.R - c1.R) * t),
                (int)(c1.G + (c2.G - c1.G) * t),
                (int)(c1.B + (c2.B - c1.B) * t)
            );

        /// <summary>
        /// 安全设置控件属性，确保在UI线程上执行
        /// </summary>
        private void SafeSetProperty(Action action)
        {
            if (IsDisposed || !IsHandleCreated) return;
            
            if (InvokeRequired)
            {
                try
                {
                    Invoke(action);
                }
                catch (ObjectDisposedException)
                {
                    // 控件已释放，忽略
                }
                catch (InvalidOperationException)
                {
                    // 句柄未创建或已销毁，忽略
                }
            }
            else
            {
                try
                {
                    action();
                }
                catch (ObjectDisposedException)
                {
                    // 控件已释放，忽略
                }
                catch (InvalidOperationException)
                {
                    // 句柄未创建或已销毁，忽略
                }
            }
        }

        /// <summary>
        /// 更新控件颜色，线程安全版本
        /// </summary>
        public void UpdateColors()
        {
            if (IsDisposed) return;
            
            // 使用DarkModeHelper统一管理颜色
            bgColor = DarkModeHelper.ComboBoxBack;
            textColor = DarkModeHelper.ComboBoxFore;
            borderColor = DarkModeHelper.ComboBoxBorder;
            arrowColor = DarkModeHelper.ComboBoxArrow;
            
            // 保持悬停和焦点颜色不变
            hoverColor = Color.FromArgb(255, 145, 60);
            focusColor = Color.FromArgb(255, 107, 0);
            
            // 安全设置属性
            SafeSetProperty(() => 
            {
                BackColor = bgColor;
                ForeColor = textColor;
                currentBorder = targetBorder = borderColor;
            });
        }

        private void UpdateState()
        {
            if (focused)
            {
                targetWidth = 2.2f;
                targetBorder = focusColor;
            }
            else if (mouseOverDropDown || ClientRectangle.Contains(PointToClient(MousePosition)))
            {
                targetWidth = 1.8f;
                targetBorder = hoverColor;
            }
            else
            {
                targetWidth = 1.2f;
                targetBorder = borderColor;
            }
            Invalidate();
        }

        private void UpdateDropDownHoverState(Point location)
        {
            var dropDownRect = GetDropDownButtonRect();
            bool wasHovered = mouseOverDropDown;
            mouseOverDropDown = dropDownRect.Contains(location);
            if (wasHovered != mouseOverDropDown)
            {
                Cursor = mouseOverDropDown ? Cursors.Hand : Cursors.Default;
                UpdateState();
            }
        }

        private Rectangle GetDropDownButtonRect()
        {
            var clientRect = ClientRectangle;
            var dropDownButtonWidth = SystemInformation.HorizontalScrollBarArrowWidth + 8;
            var dropDownRect = new Rectangle(
                clientRect.Right - dropDownButtonWidth,
                clientRect.Top,
                dropDownButtonWidth,
                clientRect.Height
            );

            if (RightToLeft == RightToLeft.Yes)
            {
                dropDownRect.X = clientRect.Left;
            }

            return dropDownRect;
        }

        // 自适应宽度功能
        private void AdjustWidth()
        {
            if (!autoSize || DesignMode) return;

            string text = SelectedItem?.ToString() ?? Text;
            if (string.IsNullOrEmpty(text)) return;

            using (var g = CreateGraphics())
            {
                // 测量文本宽度
                SizeF textSize = g.MeasureString(text, Font);

                // 计算需要的宽度：文本宽度 + 左右边距 + 下拉箭头区域
                int requiredWidth = (int)textSize.Width + padding.DpiZoom();

                // 应用最小和最大宽度限制
                int newWidth = Math.Max(minWidth.DpiZoom(), Math.Min(maxWidth.DpiZoom(), requiredWidth));

                // 只有当宽度确实改变时才更新
                if (Width != newWidth)
                {
                    Width = newWidth;
                    Invalidate();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 使用与SearchBox相同的技术绘制圆角背景
            if (!DarkModeHelper.IsDarkTheme)
            {
                using (var p = CreateRoundPath(new Rectangle(1, 2, Width - 2, Height - 2), borderRadius.DpiZoom()))
                using (var b = new SolidBrush(Color.FromArgb(15, 0, 0, 0)))
                    g.FillPath(b, p);
            }

            // 绘制背景
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = CreateRoundPath(rect, borderRadius.DpiZoom()))
            {
                FillGradient(g, path);
                DrawBorder(g, path, currentBorder, borderWidth);
            }

            // 绘制文本和下拉箭头
            DrawTextAndArrow(g);
        }

        private void FillGradient(Graphics g, GraphicsPath path)
        {
            var r = path.GetBounds();
            Color c1, c2;
            if (DarkModeHelper.IsDarkTheme)
            {
                c1 = Color.FromArgb(50, 50, 53);
                c2 = Color.FromArgb(40, 40, 43);
            }
            else
            {
                c1 = Color.FromArgb(253, 253, 255);
                c2 = Color.FromArgb(247, 247, 249);
            }

            using (var brush = new LinearGradientBrush(
                new PointF(r.X, r.Y),
                new PointF(r.X, r.Bottom),
                c1, c2))
            {
                var blend = new ColorBlend
                {
                    Positions = new[] { 0f, 0.5f, 1f },
                    Colors = new[] { c1, Color.FromArgb((c1.R + c2.R) / 2, (c1.G + c2.G) / 2, (c1.B + c2.B) / 2), c2 }
                };
                brush.InterpolationColors = blend;
                g.FillPath(brush, path);
            }
        }

        private void DrawBorder(Graphics g, GraphicsPath path, Color color, float width)
        {
            using (var pen = new Pen(color, width)
            {
                Alignment = PenAlignment.Center,
                LineJoin = LineJoin.Round
            })
            {
                g.DrawPath(pen, path);
            }
        }

        private void DrawTextAndArrow(Graphics g)
        {
            // 绘制文本
            if (SelectedItem != null || !string.IsNullOrEmpty(Text))
            {
                string text = SelectedItem?.ToString() ?? Text;
                using (var brush = new SolidBrush(ForeColor))
                {
                    // 增加文本区域的内边距，避免内容被裁剪
                    var textRect = new Rectangle(
                        16.DpiZoom(),
                        4.DpiZoom(),
                        Width - GetDropDownButtonRect().Width - 20.DpiZoom(),
                        Height - 8.DpiZoom()
                    );
                    var format = new StringFormat
                    {
                        LineAlignment = StringAlignment.Center,
                        Alignment = StringAlignment.Near,
                        Trimming = StringTrimming.EllipsisCharacter
                    };
                    g.DrawString(text, Font, brush, textRect, format);
                }
            }

            // 绘制下拉箭头
            DrawDropdownArrow(g);
        }

        private void DrawDropdownArrow(Graphics g)
        {
            var dropDownRect = GetDropDownButtonRect();
            var currentArrowColor = mouseOverDropDown ? focusColor :
                                  focused ? focusColor :
                                  arrowColor;

            // 创建箭头路径
            var middle = new Point(
                dropDownRect.Left + dropDownRect.Width / 2,
                dropDownRect.Top + dropDownRect.Height / 2
            );

            int arrowSize = 6.DpiZoom();
            var arrowPoints = new Point[]
            {
                new Point(middle.X - arrowSize, middle.Y - arrowSize / 2),
                new Point(middle.X + arrowSize, middle.Y - arrowSize / 2),
                new Point(middle.X, middle.Y + arrowSize / 2)
            };

            using (var brush = new SolidBrush(currentArrowColor))
            {
                g.FillPolygon(brush, arrowPoints);
            }
        }

        private GraphicsPath CreateRoundPath(Rectangle rect, int radius)
        {
            return DarkModeHelper.CreateRoundedRectanglePath(rect, radius);
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            // 绘制背景
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                e.Graphics.FillRectangle(new SolidBrush(SystemColors.Highlight), e.Bounds);
            }
            else
            {
                e.Graphics.FillRectangle(new SolidBrush(BackColor), e.Bounds);
            }

            // 绘制项目文本
            string text = GetItemText(Items[e.Index]);
            using (var brush = new SolidBrush((e.State & DrawItemState.Selected) == DrawItemState.Selected ? SystemColors.HighlightText : ForeColor))
            {
                var rect = e.Bounds;
                rect.X += 4;
                e.Graphics.DrawString(text, Font, brush, rect,
                    new StringFormat { LineAlignment = StringAlignment.Center });
            }

            // 绘制焦点矩形
            if ((e.State & DrawItemState.Focus) == DrawItemState.Focus)
            {
                e.DrawFocusRectangle();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DarkModeHelper.ThemeChanged -= OnThemeChanged;
                animTimer?.Stop();
                animTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!DesignMode && autoSize)
            {
                // 初始调整宽度
                AdjustWidth();
            }
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            if (autoSize) AdjustWidth();
        }
        
        // 主题变化事件处理
        private void OnThemeChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            
            // 确保在UI线程上执行
            if (InvokeRequired)
            {
                try
                {
                    Invoke(new Action<object, EventArgs>(OnThemeChanged), sender, e);
                }
                catch (ObjectDisposedException)
                {
                    // 控件已释放，忽略
                    return;
                }
                catch (InvalidOperationException)
                {
                    // 句柄未创建或已销毁，忽略
                    return;
                }
                return;
            }
            
            // 检查控件是否已释放
            if (IsDisposed) return;
            
            UpdateColors();
            Invalidate();
        }

        // 关键修复：移除设置Region的代码，因为它会导致内容被裁剪
        // 我们只通过绘制来实现圆角效果，而不改变控件的实际区域
    }
}