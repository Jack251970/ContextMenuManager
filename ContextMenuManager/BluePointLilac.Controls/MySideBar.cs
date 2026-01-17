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
        private readonly Panel PnlSelected = new() { Enabled = false, BackColor = Color.Transparent };
        private readonly Panel PnlHovered = new() { Enabled = false, BackColor = Color.Transparent };
        private readonly Label LblSeparator = new() { Dock = DockStyle.Right, Width = 1 };
        private readonly Timer animationTimer = new() { Interval = 16 };

        private string[] itemNames;
        private int itemHeight = 36;
        private int selectIndex = -1;
        private int hoverIndex = -1;

        private int animationTargetIndex = -1;
        private int animationCurrentIndex = -1;
        private float animationProgress = 0f;
        private const float ANIMATION_SPEED = 0.25f;
        private bool isAnimating = false;

        public Color SelectedGradientColor1 { get; set; } = Color.FromArgb(255, 195, 0);
        public Color SelectedGradientColor2 { get; set; } = Color.FromArgb(255, 141, 26);
        public Color SelectedGradientColor3 { get; set; } = Color.FromArgb(255, 195, 0);
        public Color BackgroundGradientColor1 { get; set; } = Color.FromArgb(240, 240, 240);
        public Color BackgroundGradientColor2 { get; set; } = Color.FromArgb(220, 220, 220);
        public Color BackgroundGradientColor3 { get; set; } = Color.FromArgb(200, 200, 200);

        [Browsable(false)] public bool EnableAnimation { get; set; } = true;

        public string[] ItemNames
        {
            get => itemNames;
            set
            {
                itemNames = value;
                if (value != null && !IsFixedWidth)
                {
                    var maxWidth = 0;
                    foreach (var str in value)
                        maxWidth = Math.Max(maxWidth, GetItemWidth(str));
                    Width = maxWidth + 2 * HorizontalSpace;
                }
                PnlHovered.Width = PnlSelected.Width = Width;
                UpdateBackground();
                SelectedIndex = -1;
            }
        }

        public int ItemHeight { get => itemHeight; set => itemHeight = Math.Max(1, value); }
        public int TopSpace { get; set; } = 4.DpiZoom();
        public int HorizontalSpace { get; set; } = 20.DpiZoom();
        public bool IsFixedWidth { get; set; } = true;

        public Color SeparatorColor { get => LblSeparator.BackColor; set => LblSeparator.BackColor = value; }
        public Color SelectedBackColor { get => PnlSelected.BackColor; set => PnlSelected.BackColor = value; }
        public Color HoveredBackColor { get => PnlHovered.BackColor; set => PnlHovered.BackColor = value; }
        public Color SelectedForeColor { get; set; } = Color.Black;
        public Color HoveredForeColor { get; set; }

        private float VerticalSpace => (itemHeight - TextRenderer.MeasureText(" ", Font).Height) * 0.5F;

        public event EventHandler SelectIndexChanged;
        public event EventHandler HoverIndexChanged;

        public int SelectedIndex
        {
            get => selectIndex;
            set
            {
                if (selectIndex == value) return;
                if (EnableAnimation && value >= 0 && value < ItemNames?.Length && selectIndex >= 0 && !isAnimating)
                    StartAnimation(selectIndex, value);
                else
                    SetSelectedIndexDirectly(value);
            }
        }

        public int HoveredIndex
        {
            get => hoverIndex;
            set
            {
                if (hoverIndex == value) return;
                hoverIndex = value;
                RefreshItem(PnlHovered, value);
                HoverIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public MySideBar()
        {
            Dock = DockStyle.Left;
            MinimumSize = new Size(1, 1);
            BackgroundImageLayout = ImageLayout.None;
            DoubleBuffered = true;
            Font = new Font(SystemFonts.MenuFont.FontFamily, SystemFonts.MenuFont.Size + 1F);

            InitializeColors();
            Controls.AddRange(new Control[] { LblSeparator, PnlSelected, PnlHovered });

            SizeChanged += (sender, e) => UpdateBackground();
            PnlHovered.Paint += PaintHoveredItem;
            PnlSelected.Paint += PaintSelectedItem;
            animationTimer.Tick += AnimationTimer_Tick;

            DarkModeHelper.ThemeChanged += OnThemeChanged;
            SelectedIndex = -1;
        }

        private void StartAnimation(int fromIndex, int toIndex)
        {
            animationCurrentIndex = fromIndex;
            animationTargetIndex = toIndex;
            animationProgress = 0f;
            isAnimating = true;

            if (!animationTimer.Enabled)
                animationTimer.Start();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            animationProgress += ANIMATION_SPEED;

            if (animationProgress >= 1f)
                CompleteAnimation();
            else
                UpdateAnimationFrame();
        }

        private void UpdateAnimationFrame()
        {
            if (animationCurrentIndex < 0 || animationTargetIndex < 0) return;

            var easedProgress = EaseOutCubic(animationProgress);
            var startTop = GetItemTop(animationCurrentIndex);
            var targetTop = GetItemTop(animationTargetIndex);
            var currentTop = Math.Max(0, Math.Min(
                (int)(startTop + (targetTop - startTop) * easedProgress), Height - ItemHeight));

            PnlSelected.Top = currentTop;
            PnlSelected.Height = ItemHeight;

            if (hoverIndex == selectIndex)
            {
                PnlHovered.Top = PnlSelected.Top;
                PnlHovered.Height = ItemHeight;
            }

            Invalidate();
            Update();
        }

        private void CompleteAnimation()
        {
            animationProgress = 1f;
            isAnimating = false;
            animationTimer.Stop();

            SetSelectedIndexDirectly(animationTargetIndex);
            PnlSelected.Top = GetItemTop(selectIndex);

            if (hoverIndex == selectIndex)
                PnlHovered.Top = PnlSelected.Top;

            Refresh();
        }

        private void SetSelectedIndexDirectly(int value)
        {
            selectIndex = value;
            RefreshItem(PnlSelected, value);
            HoveredIndex = value;
            SelectIndexChanged?.Invoke(this, EventArgs.Empty);
        }

        private int GetItemTop(int index)
        {
            return TopSpace + index * ItemHeight;
        }

        private float EaseOutCubic(float t)
        {
            return 1 - (float)Math.Pow(1 - t, 3);
        }

        public void BeginUpdate()
        {
            SuspendLayout();
        }

        public void EndUpdate() { ResumeLayout(true); UpdateBackground(); }
        public int GetItemWidth(string str)
        {
            return TextRenderer.MeasureText(str, Font).Width + 2 * HorizontalSpace;
        }

        public void StopAnimation()
        {
            if (isAnimating)
            {
                animationTimer.Stop();
                isAnimating = false;
                SetSelectedIndexDirectly(animationTargetIndex);
            }
        }

        public void SmoothScrollTo(int index)
        {
            if (index >= 0 && index < ItemNames?.Length)
                SelectedIndex = index;
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

        private void InitializeColors()
        {
            BackColor = DarkModeHelper.SideBarBackground;
            ForeColor = DarkModeHelper.FormFore;
            HoveredBackColor = DarkModeHelper.SideBarHovered;
            SeparatorColor = DarkModeHelper.SideBarSeparator;
            PnlSelected.BackColor = Color.Transparent;

            BackgroundGradientColor1 = DarkModeHelper.ToolBarGradientTop;
            BackgroundGradientColor2 = DarkModeHelper.ToolBarGradientMiddle;
            BackgroundGradientColor3 = DarkModeHelper.ToolBarGradientBottom;
        }

        private void UpdateBackground()
        {
            if (ItemNames == null) return;
            var bgWidth = Math.Max(1, Width);
            var bgHeight = ItemNames.Length == 0 ? Math.Max(1, Height) :
                Math.Max(Height, Math.Max(0, ItemHeight) * ItemNames.Length);

            try
            {
                var oldBackground = BackgroundImage;
                BackgroundImage = new Bitmap(bgWidth, bgHeight);
                oldBackground?.Dispose();

                using var g = Graphics.FromImage(BackgroundImage);
                DrawBackgroundGradient(g, bgWidth, bgHeight);
                if (ItemNames.Length > 0 && ItemHeight > 0 && Width > 0 && Height > 0)
                {
                    DrawTextItems(g);
                    DrawSeparators(g);
                }
            }
            catch (ArgumentException)
            {
                BackgroundImage?.Dispose();
                BackgroundImage = null;
            }
        }

        private void DrawBackgroundGradient(Graphics g, int width, int height)
        {
            using var brush = new LinearGradientBrush(new Rectangle(0, 0, width, height), Color.Empty, Color.Empty, 0f);
            brush.InterpolationColors = new ColorBlend
            {
                Colors = new[] { BackgroundGradientColor1, BackgroundGradientColor2, BackgroundGradientColor3 },
                Positions = new[] { 0f, 0.5f, 1f }
            };
            g.FillRectangle(brush, new Rectangle(0, 0, width, height));
        }

        private void DrawTextItems(Graphics g)
        {
            if (ItemNames == null || ItemNames.Length == 0) return;
            using var textBrush = new SolidBrush(ForeColor);
            for (var i = 0; i < ItemNames.Length; i++)
            {
                var item = ItemNames[i];
                if (string.IsNullOrEmpty(item)) continue;
                var yPos = TopSpace + i * ItemHeight + VerticalSpace;
                g.DrawString(item, Font, textBrush, new PointF(HorizontalSpace, yPos));
            }
        }

        private void DrawSeparators(Graphics g)
        {
            using var pen = new Pen(SeparatorColor);
            for (var i = 0; i < ItemNames.Length; i++)
            {
                if (ItemNames[i] != null) continue;
                var yPos = TopSpace + (i + 0.5F) * ItemHeight;
                g.DrawLine(pen, HorizontalSpace, yPos, Width - HorizontalSpace, yPos);
            }
        }

        private int CalculateItemIndex(int yPos)
        {
            if (ItemNames == null || ItemHeight <= 0) return -1;
            var index = (yPos - TopSpace) / ItemHeight;
            if (index < 0 || index >= ItemNames.Length) return -1;
            if (string.IsNullOrEmpty(ItemNames[index])) return -1;
            return index;
        }

        private void RefreshItem(Panel panel, int index)
        {
            if (index < 0 || index >= ItemNames?.Length)
            {
                panel.Top = -ItemHeight;
                panel.Text = null;
            }
            else
            {
                var actualTop = Math.Max(0, Math.Min(TopSpace + index * ItemHeight, Height - ItemHeight));
                panel.Top = actualTop;
                panel.Text = ItemNames[index];
            }
            panel.Height = ItemHeight;
            panel.Refresh();
        }

        private void PaintHoveredItem(object sender, PaintEventArgs e)
        {
            var ctr = (Control)sender;
            if (string.IsNullOrEmpty(ctr.Text)) return;
            e.Graphics.FillRectangle(new SolidBrush(HoveredBackColor), new Rectangle(0, 0, ctr.Width, ctr.Height));
            DrawItemText(e, ctr, HoveredForeColor);
        }

        private void PaintSelectedItem(object sender, PaintEventArgs e)
        {
            var ctr = (Control)sender;
            if (string.IsNullOrEmpty(ctr.Text)) return;

            using (var brush = new LinearGradientBrush(new Rectangle(0, 0, ctr.Width, ctr.Height), Color.Empty, Color.Empty, 0f))
            {
                brush.InterpolationColors = new ColorBlend
                {
                    Colors = new[] { SelectedGradientColor1, SelectedGradientColor2, SelectedGradientColor3 },
                    Positions = new[] { 0f, 0.5f, 1f }
                };
                e.Graphics.FillRectangle(brush, new Rectangle(0, 0, ctr.Width, ctr.Height));
            }
            DrawItemText(e, ctr, SelectedForeColor);
        }

        private void DrawItemText(PaintEventArgs e, Control ctr, Color textColor)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            using var brush = new SolidBrush(textColor == Color.Empty ? ForeColor : textColor);
            e.Graphics.DrawString(ctr.Text, Font, brush, new PointF(HorizontalSpace, VerticalSpace));
        }

        private void ShowItem(Panel panel, MouseEventArgs e)
        {
            if (ItemNames == null) return;
            var index = CalculateItemIndex(e.Y);
            var isValid = index != -1 && index != SelectedIndex;
            Cursor = isValid ? Cursors.Hand : Cursors.Default;

            if (isValid)
            {
                if (panel == PnlSelected)
                {
                    if (isAnimating) StopAnimation();
                    SelectedIndex = index;
                }
                else HoveredIndex = index;
            }
            else HoveredIndex = SelectedIndex;
        }

        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); ShowItem(PnlHovered, e); }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) ShowItem(PnlSelected, e); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); HoveredIndex = SelectedIndex; }
        protected override void OnBackColorChanged(EventArgs e) { base.OnBackColorChanged(e); InitializeColors(); UpdateBackground(); }
        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            base.SetBoundsCore(x, y, Math.Max(1, width), Math.Max(1, height), specified);
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            InitializeColors();
            UpdateBackground();
        }
    }
}