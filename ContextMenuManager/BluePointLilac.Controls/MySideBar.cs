using BluePointLilac.Methods;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public sealed class MySideBar : Panel
    {
        private const float AnimationSpeed = 0.25f;
        private readonly Timer animTimer = new() { Interval = 16 };
        private string[] itemNames;
        private int itemHeight = 36, selectIndex = -1, hoverIndex = -1;
        private int animTarget = -1, animCurrent = -1;
        private float animProgress = 0f, curSelTop = -36;
        private bool isAnimating = false;
        private Font ownedFont;

        public Color SelectedGradientColor1 { get; set; } = Color.FromArgb(255, 255, 160, 60);
        public Color SelectedGradientColor2 { get; set; } = Color.FromArgb(255, 255, 120, 40);
        public Color SelectedGradientColor3 { get; set; } = Color.FromArgb(255, 255, 160, 60);
        public Color BackgroundGradientColor1 { get; set; } = Color.FromArgb(240, 240, 240);
        public Color BackgroundGradientColor2 { get; set; } = Color.FromArgb(220, 220, 220);
        public Color BackgroundGradientColor3 { get; set; } = Color.FromArgb(200, 200, 200);

        [Browsable(false)] public bool EnableAnimation { get; set; } = true;
        public int ItemHeight { get => itemHeight; set => itemHeight = Math.Max(1, value); }
        public int TopSpace { get; set; } = 4.DpiZoom();
        public int HorizontalSpace { get; set; } = 20.DpiZoom();
        public int ItemMargin { get; set; } = 4.DpiZoom();
        public int CornerRadius { get; set; } = 6.DpiZoom();
        public bool IsFixedWidth { get; set; } = true;

        public Color SeparatorColor { get; set; }
        public Color SelectedBackColor { get; set; } = Color.Transparent;
        public Color HoveredBackColor { get; set; }
        public Color SelectedForeColor { get; set; } = Color.White;
        public Color HoveredForeColor { get; set; }
        public Color RightBorderColor { get; set; }

        public event EventHandler SelectIndexChanged;
        public event EventHandler HoverIndexChanged;

        public string[] ItemNames
        {
            get => itemNames;
            set
            {
                itemNames = value;
                if (value != null && !IsFixedWidth)
                {
                    var maxW = value.Where(s => s != null)
                        .Select(s => TextRenderer.MeasureText(s, Font).Width)
                        .DefaultIfEmpty(0)
                        .Max();
                    Width = maxW + 2 * HorizontalSpace;
                }
                UpdateBackground();
                SelectedIndex = -1;
            }
        }

        public int SelectedIndex
        {
            get => selectIndex;
            set
            {
                if (selectIndex == value) return;
                if (EnableAnimation && value >= 0 && value < ItemNames?.Length && selectIndex >= 0 && !isAnimating)
                    StartAnimation(selectIndex, value);
                else SetSelectedIndexDirectly(value);
            }
        }

        public int HoveredIndex
        {
            get => hoverIndex;
            set
            {
                if (hoverIndex == value) return;
                int oldIdx = hoverIndex;
                hoverIndex = value;
                InvalidateItem(oldIdx);
                InvalidateItem(hoverIndex);
                HoverIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private Rectangle GetItemRect(int idx)
        {
            if (idx < 0 || ItemNames == null || idx >= ItemNames.Length) return Rectangle.Empty;
            return new Rectangle(0, TopSpace + idx * ItemHeight, Width, ItemHeight);
        }

        private void InvalidateItem(int idx) => Invalidate(GetItemRect(idx));

        public MySideBar()
        {
            Dock = DockStyle.Left;
            MinimumSize = new Size(1, 1);
            BackgroundImageLayout = ImageLayout.None;
            DoubleBuffered = true;
            ownedFont = new Font(SystemFonts.MenuFont.FontFamily, SystemFonts.MenuFont.Size + 1F);
            Font = ownedFont;
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            InitializeColors();
            SizeChanged += (s, e) => UpdateBackground();
            animTimer.Tick += AnimationTimer_Tick;
            DarkModeHelper.ThemeChanged += OnThemeChanged;
            SelectedIndex = -1;
        }

        private void OnThemeChanged(object sender, EventArgs e) { InitializeColors(); UpdateBackground(); }

        public void UpdateThemeColors() { InitializeColors(); UpdateBackground(); Invalidate(); }

        private void InitializeColors()
        {
            BackColor = DarkModeHelper.IsDwmCompositionEnabled ? Color.Transparent : DarkModeHelper.SideBarBackground;
            ForeColor = Color.White;
            HoveredBackColor = DarkModeHelper.SideBarHovered;
            HoveredForeColor = Color.White;
            SeparatorColor = DarkModeHelper.SideBarSeparator;
            RightBorderColor = DarkModeHelper.SideBarSeparator;
            BackgroundGradientColor1 = DarkModeHelper.ToolBarGradientTop;
            BackgroundGradientColor2 = DarkModeHelper.ToolBarGradientMiddle;
            BackgroundGradientColor3 = DarkModeHelper.ToolBarGradientBottom;
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            animProgress += AnimationSpeed;
            if (animProgress >= 1f)
                CompleteAnimation();
            else
                UpdateAnimationFrame();
        }

        private void UpdateAnimationFrame()
        {
            float easedProgress = CalculateEasedProgress(animProgress);
            float startY = TopSpace + animCurrent * (float)ItemHeight;
            float targetY = TopSpace + animTarget * (float)ItemHeight;
            float oldTop = curSelTop;
            curSelTop = Math.Max(0, Math.Min(startY + (targetY - startY) * easedProgress, Height - ItemHeight));
            InvalidateAnimationRegion(oldTop, curSelTop);
        }

        private void CompleteAnimation()
        {
            isAnimating = false;
            animTimer.Stop();
            SetSelectedIndexDirectly(animTarget);
        }

        private static float CalculateEasedProgress(float progress)
        {
            return 1 - (float)Math.Pow(1 - progress, 3);
        }

        private void InvalidateAnimationRegion(float oldTop, float newTop)
        {
            int minY = Math.Max(0, (int)Math.Min(oldTop, newTop));
            int maxY = (int)Math.Max(oldTop, newTop) + ItemHeight;
            Invalidate(new Rectangle(0, minY, Width, maxY - minY));
        }

        private void StartAnimation(int from, int to)
        {
            animCurrent = from; animTarget = to; animProgress = 0f; isAnimating = true;
            if (!animTimer.Enabled) animTimer.Start();
        }

        private void SetSelectedIndexDirectly(int val)
        {
            int oldIdx = selectIndex;
            selectIndex = val;
            curSelTop = (val >= 0 && ItemNames != null && val < ItemNames.Length) ? TopSpace + val * ItemHeight : -ItemHeight;
            InvalidateItem(oldIdx);
            InvalidateItem(val);
            HoveredIndex = val;
            SelectIndexChanged?.Invoke(this, EventArgs.Empty);
        }

        public void StopAnimation() { if (isAnimating) { animTimer.Stop(); isAnimating = false; SetSelectedIndexDirectly(animTarget); } }
        public void SmoothScrollTo(int idx) { if (idx >= 0 && idx < ItemNames?.Length) SelectedIndex = idx; }
        public int GetItemWidth(string str) => TextRenderer.MeasureText(str, Font).Width + 2 * HorizontalSpace;
        public void BeginUpdate() => SuspendLayout();
        public void EndUpdate()
        {
            ResumeLayout(true);
            UpdateBackground();
        }

        private void UpdateBackground()
        {
            if (ItemNames == null) return;

            if (DarkModeHelper.IsDwmCompositionEnabled)
            {
                BackgroundImage?.Dispose();
                BackgroundImage = null;
                return;
            }

            int w = Math.Max(1, Width), h = ItemNames.Length == 0 ? Math.Max(1, Height) : Math.Max(Height, Math.Max(0, ItemHeight) * ItemNames.Length);
            try
            {
                var old = BackgroundImage; BackgroundImage = new Bitmap(w, h); old?.Dispose();
                using var g = Graphics.FromImage(BackgroundImage);
                DrawBackgroundGradient(g, w, h);
                DrawTextItemsAndSeparators(g);
            }
            catch (ArgumentException) { BackgroundImage?.Dispose(); BackgroundImage = null; }
        }

        private void DrawBackgroundGradient(Graphics g, int w, int h)
        {
            if (DarkModeHelper.IsDwmCompositionEnabled)
            {
                g.Clear(Color.Transparent);
                return;
            }

            var color1 = BackgroundGradientColor1;
            var color2 = BackgroundGradientColor2;
            var color3 = BackgroundGradientColor3;

            using var b = new LinearGradientBrush(new Rectangle(0, 0, w, h), Color.Empty, Color.Empty, 0f)
            {
                InterpolationColors = new ColorBlend
                {
                    Colors = new[] { color1, color2, color3 },
                    Positions = new[] { 0f, 0.5f, 1f }
                }
            };
            g.FillRectangle(b, new Rectangle(0, 0, w, h));
        }

        private void DrawTextItemsAndSeparators(Graphics g)
        {
            using var textBrush = new SolidBrush(ForeColor);
            using var separatorPen = new Pen(SeparatorColor);
            float vSpace = (ItemHeight - TextRenderer.MeasureText(" ", Font).Height) * 0.5f;
            for (int i = 0; i < ItemNames.Length; i++)
            {
                float y = TopSpace + i * ItemHeight;
                if (ItemNames[i] == null)
                    DrawSeparator(g, separatorPen, i);
                else if (ItemNames[i].Length > 0)
                    g.DrawString(ItemNames[i], Font, textBrush, HorizontalSpace, y + vSpace);
            }
        }

        private void DrawSeparator(Graphics g, Pen pen, int index)
        {
            float y = TopSpace + (index + 0.5f) * ItemHeight;
            int margin = ItemMargin + 8.DpiZoom();
            using var brush = new SolidBrush(SeparatorColor);
            g.FillRectangle(brush, margin, y - 1, Width - 2 * margin, 2);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (DarkModeHelper.IsDwmCompositionEnabled)
            {
                e.Graphics.Clear(Color.Transparent);
            }
            else
            {
                base.OnPaintBackground(e);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (ItemNames == null) return;
            float vSpace = (ItemHeight - TextRenderer.MeasureText(" ", Font).Height) * 0.5f;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            if (DarkModeHelper.IsDwmCompositionEnabled)
            {
                // DWM模式下强制使用白色文字
                using var textBrush = new SolidBrush(Color.White);
                for (int i = 0; i < ItemNames.Length; i++)
                {
                    float y = TopSpace + i * ItemHeight;
                    if (ItemNames[i] == null)
                    {
                        int margin = ItemMargin + 8.DpiZoom();
                        using var brush = new SolidBrush(SeparatorColor);
                        e.Graphics.FillRectangle(brush, margin, y + ItemHeight / 2 - 1, Width - 2 * margin, 2);
                    }
                    else if (ItemNames[i].Length > 0)
                    {
                        e.Graphics.DrawString(ItemNames[i], Font, textBrush, HorizontalSpace, y + vSpace);
                    }
                }
            }

            void DrawItem(int idx, Color back, Color fore, float y)
            {
                if (idx < 0 || idx >= ItemNames.Length || string.IsNullOrEmpty(ItemNames[idx])) return;
                var itemRect = new RectangleF(ItemMargin, y + 2, Width - 2 * ItemMargin, ItemHeight - 4);
                if (itemRect.Width <= 0 || itemRect.Height <= 0) return;

                using var path = GetRoundedRectPath(itemRect, CornerRadius);
                if (back == Color.Transparent)
                {
                    using var b = new LinearGradientBrush(itemRect, Color.Empty, Color.Empty, 90f)
                    {
                        InterpolationColors = new ColorBlend
                        {
                            Colors = new[] { SelectedGradientColor1, SelectedGradientColor2, SelectedGradientColor3 },
                            Positions = new[] { 0f, 0.5f, 1f }
                        }
                    };
                    e.Graphics.FillPath(b, path);
                }
                else
                {
                    using var b = new SolidBrush(back);
                    e.Graphics.FillPath(b, path);
                }
                // DWM模式下强制使用白色文字
                using var fb = new SolidBrush(DarkModeHelper.IsDwmCompositionEnabled ? Color.White : (fore == Color.Empty ? ForeColor : fore));
                var textRect = new RectangleF(ItemMargin + HorizontalSpace - ItemMargin, y + 2 + vSpace, Width - 2 * HorizontalSpace, ItemHeight - 4 - 2 * vSpace);
                e.Graphics.DrawString(ItemNames[idx], Font, fb, HorizontalSpace, y + 2 + vSpace);
            }

            if (hoverIndex >= 0 && hoverIndex != selectIndex)
            {
                float hoverY = TopSpace + (float)hoverIndex * ItemHeight;
                DrawItem(hoverIndex, HoveredBackColor, HoveredForeColor, hoverY);
            }
            if (selectIndex >= 0) DrawItem(selectIndex, Color.Transparent, SelectedForeColor, curSelTop);
            using var p = new Pen(RightBorderColor);
            e.Graphics.DrawLine(p, Width - 1, 0, Width - 1, Height);
        }

        private GraphicsPath GetRoundedRectPath(RectangleF rect, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0) { path.AddRectangle(rect); return path; }
            float r = radius;
            path.AddArc(rect.X, rect.Y, r * 2, r * 2, 180, 90);
            path.AddArc(rect.Right - r * 2, rect.Y, r * 2, r * 2, 270, 90);
            path.AddArc(rect.Right - r * 2, rect.Bottom - r * 2, r * 2, r * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - r * 2, r * 2, r * 2, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (ItemNames == null) return;
            int idx = (e.Y - TopSpace) / ItemHeight;
            bool valid = idx >= 0 && idx < ItemNames.Length && !string.IsNullOrEmpty(ItemNames[idx]) && idx != SelectedIndex;
            Cursor = valid ? Cursors.Hand : Cursors.Default;
            HoveredIndex = valid ? idx : SelectedIndex;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || ItemNames == null) return;
            int idx = (e.Y - TopSpace) / ItemHeight;
            if (idx >= 0 && idx < ItemNames.Length && !string.IsNullOrEmpty(ItemNames[idx]) && idx != SelectedIndex)
            {
                if (isAnimating) StopAnimation();
                SelectedIndex = idx;
            }
        }

        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); HoveredIndex = SelectedIndex; }
        protected override void OnBackColorChanged(EventArgs e) { base.OnBackColorChanged(e); InitializeColors(); UpdateBackground(); }
        protected override void SetBoundsCore(int x, int y, int w, int h, BoundsSpecified s) => base.SetBoundsCore(x, y, Math.Max(1, w), Math.Max(1, h), s);
        protected override void Dispose(bool disposing) { if (disposing) { DarkModeHelper.ThemeChanged -= OnThemeChanged; animTimer?.Dispose(); ownedFont?.Dispose(); } base.Dispose(disposing); }
    }
}
