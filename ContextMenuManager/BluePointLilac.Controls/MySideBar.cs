using BluePointLilac.Methods;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public sealed class MySideBar : Panel
    {
        private readonly Timer animTimer = new() { Interval = 16 };
        private string[] itemNames;
        private int itemHeight = 36, selectIndex = -1, hoverIndex = -1;
        private int animTarget = -1, animCurrent = -1;
        private float animProgress = 0f, curSelTop = -1;
        private bool isAnimating = false;

        public Color SelectedGradientColor1 { get; set; } = Color.FromArgb(255, 195, 0);
        public Color SelectedGradientColor2 { get; set; } = Color.FromArgb(255, 141, 26);
        public Color SelectedGradientColor3 { get; set; } = Color.FromArgb(255, 195, 0);
        public Color BackgroundGradientColor1 { get; set; } = Color.FromArgb(240, 240, 240);
        public Color BackgroundGradientColor2 { get; set; } = Color.FromArgb(220, 220, 220);
        public Color BackgroundGradientColor3 { get; set; } = Color.FromArgb(200, 200, 200);

        [Browsable(false)] public bool EnableAnimation { get; set; } = true;
        public int ItemHeight { get => itemHeight; set => itemHeight = Math.Max(1, value); }
        public int TopSpace { get; set; } = 4.DpiZoom();
        public int HorizontalSpace { get; set; } = 20.DpiZoom();
        public bool IsFixedWidth { get; set; } = true;

        public Color SeparatorColor { get; set; }
        public Color SelectedBackColor { get; set; } = Color.Transparent;
        public Color HoveredBackColor { get; set; }
        public Color SelectedForeColor { get; set; } = Color.Black;
        public Color HoveredForeColor { get; set; }

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
                    var maxW = 0;
                    foreach (var s in value) if (s != null) maxW = Math.Max(maxW, TextRenderer.MeasureText(s, Font).Width);
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
            set { if (hoverIndex != value) { hoverIndex = value; Invalidate(); HoverIndexChanged?.Invoke(this, EventArgs.Empty); } }
        }

        public MySideBar()
        {
            Dock = DockStyle.Left; MinimumSize = new Size(1, 1); BackgroundImageLayout = ImageLayout.None; DoubleBuffered = true;
            Font = new Font(SystemFonts.MenuFont.FontFamily, SystemFonts.MenuFont.Size + 1F);
            InitializeColors();
            SizeChanged += (s, e) => UpdateBackground();
            animTimer.Tick += (s, e) =>
            {
                animProgress += 0.25f;
                if (animProgress >= 1f) { isAnimating = false; animTimer.Stop(); SetSelectedIndexDirectly(animTarget); }
                else
                {
                    float t = 1 - (float)Math.Pow(1 - animProgress, 3);
                    float start = TopSpace + animCurrent * ItemHeight, target = TopSpace + animTarget * ItemHeight;
                    curSelTop = Math.Max(0, Math.Min(start + (target - start) * t, Height - ItemHeight));
                    Invalidate();
                }
            };
            DarkModeHelper.ThemeChanged += OnThemeChanged;
            SelectedIndex = -1;
        }

        private void OnThemeChanged(object sender, EventArgs e) { InitializeColors(); UpdateBackground(); }

        private void InitializeColors()
        {
            BackColor = DarkModeHelper.SideBarBackground; ForeColor = DarkModeHelper.FormFore;
            HoveredBackColor = DarkModeHelper.SideBarHovered; SeparatorColor = DarkModeHelper.SideBarSeparator;
            BackgroundGradientColor1 = DarkModeHelper.ToolBarGradientTop;
            BackgroundGradientColor2 = DarkModeHelper.ToolBarGradientMiddle;
            BackgroundGradientColor3 = DarkModeHelper.ToolBarGradientBottom;
        }

        private void StartAnimation(int from, int to)
        {
            animCurrent = from; animTarget = to; animProgress = 0f; isAnimating = true;
            if (!animTimer.Enabled) animTimer.Start();
        }

        private void SetSelectedIndexDirectly(int val)
        {
            selectIndex = val;
            curSelTop = (val >= 0 && ItemNames != null && val < ItemNames.Length) ? TopSpace + val * ItemHeight : -ItemHeight;
            HoveredIndex = val; Invalidate(); SelectIndexChanged?.Invoke(this, EventArgs.Empty);
        }

        public void StopAnimation() { if (isAnimating) { animTimer.Stop(); isAnimating = false; SetSelectedIndexDirectly(animTarget); } }
        public void SmoothScrollTo(int idx) { if (idx >= 0 && idx < ItemNames?.Length) SelectedIndex = idx; }
        public int GetItemWidth(string str) => TextRenderer.MeasureText(str, Font).Width + 2 * HorizontalSpace;
        public void BeginUpdate() => SuspendLayout();
        public void EndUpdate() { ResumeLayout(true); UpdateBackground(); }

        private void UpdateBackground()
        {
            if (ItemNames == null) return;
            int w = Math.Max(1, Width), h = ItemNames.Length == 0 ? Math.Max(1, Height) : Math.Max(Height, Math.Max(0, ItemHeight) * ItemNames.Length);
            try
            {
                var old = BackgroundImage; BackgroundImage = new Bitmap(w, h); old?.Dispose();
                using var g = Graphics.FromImage(BackgroundImage);
                using (var b = new LinearGradientBrush(new Rectangle(0, 0, w, h), Color.Empty, Color.Empty, 0f) { InterpolationColors = new ColorBlend { Colors = new[] { BackgroundGradientColor1, BackgroundGradientColor2, BackgroundGradientColor3 }, Positions = new[] { 0f, 0.5f, 1f } } })
                    g.FillRectangle(b, new Rectangle(0, 0, w, h));
                
                using var tb = new SolidBrush(ForeColor); using var p = new Pen(SeparatorColor);
                float vSpace = (ItemHeight - TextRenderer.MeasureText(" ", Font).Height) * 0.5f;
                for (int i = 0; i < ItemNames.Length; i++)
                {
                    if (ItemNames[i] == null) g.DrawLine(p, HorizontalSpace, TopSpace + (i + 0.5f) * ItemHeight, Width - HorizontalSpace, TopSpace + (i + 0.5f) * ItemHeight);
                    else if (ItemNames[i].Length > 0) g.DrawString(ItemNames[i], Font, tb, HorizontalSpace, TopSpace + i * ItemHeight + vSpace);
                }
            }
            catch { BackgroundImage?.Dispose(); BackgroundImage = null; }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (ItemNames == null) return;
            float vSpace = (ItemHeight - TextRenderer.MeasureText(" ", Font).Height) * 0.5f;
            void DrawItem(int idx, Color back, Color fore, float y)
            {
                if (idx < 0 || idx >= ItemNames.Length) return;
                var r = new RectangleF(0, y, Width, ItemHeight);
                if (back == Color.Transparent)
                {
                    using var b = new LinearGradientBrush(r, Color.Empty, Color.Empty, 0f) { InterpolationColors = new ColorBlend { Colors = new[] { SelectedGradientColor1, SelectedGradientColor2, SelectedGradientColor3 }, Positions = new[] { 0f, 0.5f, 1f } } };
                    e.Graphics.FillRectangle(b, r);
                }
                else { using var b = new SolidBrush(back); e.Graphics.FillRectangle(b, r); }
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                using var fb = new SolidBrush(fore == Color.Empty ? ForeColor : fore);
                e.Graphics.DrawString(ItemNames[idx], Font, fb, HorizontalSpace, y + vSpace);
            }

            if (hoverIndex >= 0 && hoverIndex != selectIndex) DrawItem(hoverIndex, HoveredBackColor, HoveredForeColor, TopSpace + hoverIndex * ItemHeight);
            if (selectIndex >= 0) DrawItem(selectIndex, Color.Transparent, SelectedForeColor, curSelTop);
            using (var p = new Pen(SeparatorColor)) e.Graphics.DrawLine(p, Width - 1, 0, Width - 1, Height);
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
        protected override void Dispose(bool disposing) { if (disposing) { DarkModeHelper.ThemeChanged -= OnThemeChanged; animTimer?.Dispose(); Font?.Dispose(); } base.Dispose(disposing); }
    }
}
