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
        private bool mouseOverDropDown = false;
        private bool focused = false;
        private Timer animTimer;
        private float borderWidth = 1.2f, targetWidth = 1.2f;
        private Color currentBorder, targetBorder;
        private readonly int borderRadius = 14;

        [DefaultValue(typeof(Color), "255, 145, 60")]
        public Color HoverColor { get; set; } = Color.FromArgb(255, 145, 60);

        [DefaultValue(typeof(Color), "255, 107, 0")]
        public Color FocusColor { get; set; } = Color.FromArgb(255, 107, 0);

        [DefaultValue(typeof(Color), "100, 100, 100")]
        public Color ArrowColor { get; set; } = Color.FromArgb(100, 100, 100);

        [DefaultValue(true)]
        public bool AutoSize { get; set; } = true;

        [DefaultValue(120)]
        public int MinWidth { get; set; } = 120;

        [DefaultValue(400)]
        public int MaxWidth { get; set; } = 400;

        [DefaultValue(50)]
        public int Padding { get; set; } = 50;

        public RComboBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.ResizeRedraw |
                    ControlStyles.OptimizedDoubleBuffer, true);

            DrawMode = DrawMode.OwnerDrawFixed;
            DropDownStyle = ComboBoxStyle.DropDownList;
            FlatStyle = FlatStyle.Flat;
            Height = 32.DpiZoom();

            InitAnimation();
            UpdateColors();

            DarkModeHelper.ThemeChanged += OnThemeChanged;

            GotFocus += (s, e) => { focused = true; UpdateState(); };
            LostFocus += (s, e) => { focused = false; UpdateState(); };
            MouseEnter += (s, e) => UpdateState();
            MouseLeave += (s, e) => { mouseOverDropDown = false; UpdateState(); };
            MouseMove += (s, e) => UpdateDropDownHoverState(e.Location);

            SelectedIndexChanged += (s, e) => { if (AutoSize) AdjustWidth(); };
            TextChanged += (s, e) => { if (AutoSize) AdjustWidth(); };
        }

        private void InitAnimation()
        {
            animTimer = new Timer { Interval = 16 };
            animTimer.Tick += (s, e) => {
                bool update = false;
                if (Math.Abs(borderWidth - targetWidth) > 0.01f)
                {
                    borderWidth += (targetWidth - borderWidth) * 0.3f;
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
            currentBorder = targetBorder = DarkModeHelper.ComboBoxBorder;
        }

        private Color ColorLerp(Color c1, Color c2, float t) =>
            Color.FromArgb(
                (int)(c1.A + (c2.A - c1.A) * t),
                (int)(c1.R + (c2.R - c1.R) * t),
                (int)(c1.G + (c2.G - c1.G) * t),
                (int)(c1.B + (c2.B - c1.B) * t)
            );

        public void UpdateColors()
        {
            if (IsDisposed) return;

            SafeInvoke(() => 
            {
                BackColor = DarkModeHelper.ComboBoxBack;
                ForeColor = DarkModeHelper.ComboBoxFore;
                currentBorder = targetBorder = DarkModeHelper.ComboBoxBorder;
                ArrowColor = DarkModeHelper.ComboBoxArrow;
            });
        }

        private void UpdateState()
        {
            if (focused)
            {
                targetWidth = 2.2f;
                targetBorder = FocusColor;
            }
            else if (mouseOverDropDown || ClientRectangle.Contains(PointToClient(MousePosition)))
            {
                targetWidth = 1.8f;
                targetBorder = HoverColor;
            }
            else
            {
                targetWidth = 1.2f;
                targetBorder = DarkModeHelper.ComboBoxBorder;
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

        private void AdjustWidth()
        {
            if (!AutoSize || DesignMode) return;

            string text = SelectedItem?.ToString() ?? Text;
            if (string.IsNullOrEmpty(text)) return;

            using (var g = CreateGraphics())
            {
                SizeF textSize = g.MeasureString(text, Font);
                int requiredWidth = (int)textSize.Width + Padding.DpiZoom();
                int newWidth = Math.Max(MinWidth.DpiZoom(), Math.Min(MaxWidth.DpiZoom(), requiredWidth));

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

            if (!DarkModeHelper.IsDarkTheme)
            {
                using (var p = DarkModeHelper.CreateRoundedRectanglePath(new Rectangle(1, 2, Width - 2, Height - 2), borderRadius.DpiZoom()))
                using (var b = new SolidBrush(Color.FromArgb(15, 0, 0, 0)))
                    g.FillPath(b, p);
            }

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = DarkModeHelper.CreateRoundedRectanglePath(rect, borderRadius.DpiZoom()))
            {
                FillGradient(g, path);
                DrawBorder(g, path, currentBorder, borderWidth);
            }

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

            using (var brush = new LinearGradientBrush(new PointF(r.X, r.Y), new PointF(r.X, r.Bottom), c1, c2))
            {
                brush.InterpolationColors = new ColorBlend
                {
                    Positions = new[] { 0f, 0.5f, 1f },
                    Colors = new[] { c1, Color.FromArgb((c1.R + c2.R) / 2, (c1.G + c2.G) / 2, (c1.B + c2.B) / 2), c2 }
                };
                g.FillPath(brush, path);
            }
        }

        private void DrawBorder(Graphics g, GraphicsPath path, Color color, float width)
        {
            using (var pen = new Pen(color, width) { Alignment = PenAlignment.Center, LineJoin = LineJoin.Round })
                g.DrawPath(pen, path);
        }

        private void DrawTextAndArrow(Graphics g)
        {
            if (SelectedItem != null || !string.IsNullOrEmpty(Text))
            {
                string text = SelectedItem?.ToString() ?? Text;
                using (var brush = new SolidBrush(ForeColor))
                {
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

            DrawDropdownArrow(g);
        }

        private void DrawDropdownArrow(Graphics g)
        {
            var dropDownRect = GetDropDownButtonRect();
            var currentArrowColor = mouseOverDropDown ? FocusColor :
                                  focused ? FocusColor :
                                  ArrowColor;

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
                g.FillPolygon(brush, arrowPoints);
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                e.Graphics.FillRectangle(new SolidBrush(SystemColors.Highlight), e.Bounds);
            else
                e.Graphics.FillRectangle(new SolidBrush(BackColor), e.Bounds);

            string text = GetItemText(Items[e.Index]);
            using (var brush = new SolidBrush((e.State & DrawItemState.Selected) == DrawItemState.Selected ? SystemColors.HighlightText : ForeColor))
            {
                var rect = e.Bounds;
                rect.X += 4;
                e.Graphics.DrawString(text, Font, brush, rect, new StringFormat { LineAlignment = StringAlignment.Center });
            }

            if ((e.State & DrawItemState.Focus) == DrawItemState.Focus)
                e.DrawFocusRectangle();
        }

        private void SafeInvoke(Action action)
        {
            if (IsDisposed || !IsHandleCreated) return;
            if (InvokeRequired)
            {
                try { Invoke(action); }
                catch { /* 忽略调用异常 */ }
            }
            else action();
        }

        private void OnThemeChanged(object sender, EventArgs e) => SafeInvoke(() => { UpdateColors(); Invalidate(); });

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

        protected override void OnResize(EventArgs e) { base.OnResize(e); Invalidate(); }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!DesignMode && AutoSize) AdjustWidth();
        }

        protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); if (AutoSize) AdjustWidth(); }
    }
}