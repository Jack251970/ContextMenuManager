using BluePointLilac.Methods;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.ComponentModel;

namespace BluePointLilac.Controls
{
    public sealed class MySideBar : Panel
    {
        private readonly Panel PnlSelected = new Panel { Enabled = false, BackColor = Color.Transparent };
        private readonly Panel PnlHovered = new Panel { Enabled = false, BackColor = Color.Transparent };
        private readonly Label LblSeparator = new Label { Dock = DockStyle.Right, Width = 1 };

        // 动画相关变量
        private readonly Timer animationTimer = new Timer { Interval = 16 }; // ~60 FPS
        private int animationTargetIndex = -1;
        private int animationCurrentIndex = -1;
        private float animationProgress = 0f;
        private const float ANIMATION_SPEED = 0.25f;
        private bool isAnimating = false;

        private string[] itemNames;
        private int itemHeight = 36;
        private int selectIndex = -1;
        private int hoverIndex = -1;

        public Color SelectedGradientColor1 { get; set; } = Color.FromArgb(255, 195, 0);
        public Color SelectedGradientColor2 { get; set; } = Color.FromArgb(255, 141, 26);
        public Color SelectedGradientColor3 { get; set; } = Color.FromArgb(255, 195, 0);
        public Color BackgroundGradientColor1 { get; set; } = Color.FromArgb(240, 240, 240);
        public Color BackgroundGradientColor2 { get; set; } = Color.FromArgb(220, 220, 220);
        public Color BackgroundGradientColor3 { get; set; } = Color.FromArgb(200, 200, 200);

        [Browsable(false)]
        public bool EnableAnimation { get; set; } = true;

        [Browsable(false)]
        public int AnimationDuration
        {
            get => (int)(1000f / ANIMATION_SPEED / 16f);
            set => animationTimer.Interval = Math.Max(1, 1000 / Math.Max(1, value));
        }

        public string[] ItemNames
        {
            get => itemNames;
            set
            {
                itemNames = value;
                if (value != null && !IsFixedWidth)
                {
                    int maxWidth = 0;
                    Array.ForEach(value, str => maxWidth = Math.Max(maxWidth, GetItemWidth(str)));
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

        private float VerticalSpace => (itemHeight - TextHeight) * 0.5F;
        private int TextHeight => TextRenderer.MeasureText(" ", Font).Height;
        private bool IsDarkTheme => ControlPaint.Light(BackColor).GetBrightness() < 0.5f;

        public event EventHandler SelectIndexChanged;
        public event EventHandler HoverIndexChanged;

        public int SelectedIndex
        {
            get => selectIndex;
            set
            {
                if (selectIndex == value) return;

                if (EnableAnimation && value >= 0 && value < ItemNames?.Length && selectIndex >= 0 && !isAnimating)
                {
                    // 启动动画
                    StartAnimation(selectIndex, value);
                }
                else
                {
                    // 无动画直接设置
                    SetSelectedIndexDirectly(value);
                }
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

            // 设置动画计时器
            animationTimer.Tick += AnimationTimer_Tick;

            SelectedIndex = -1;
        }

        private void StartAnimation(int fromIndex, int toIndex)
        {
            animationCurrentIndex = fromIndex;
            animationTargetIndex = toIndex;
            animationProgress = 0f;
            isAnimating = true;

            // 预计算动画参数
            PrecalculateAnimationParameters();

            if (!animationTimer.Enabled)
                animationTimer.Start();
        }

        private void PrecalculateAnimationParameters()
        {
            // 预先计算动画路径，优化性能
            // 这里可以添加任何预计算逻辑
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            animationProgress += ANIMATION_SPEED;

            if (animationProgress >= 1f)
            {
                // 动画完成
                CompleteAnimation();
            }
            else
            {
                // 更新动画状态
                UpdateAnimationFrame();
            }
        }

        private void UpdateAnimationFrame()
        {
            if (animationCurrentIndex < 0 || animationTargetIndex < 0) return;

            // 使用多种缓动函数选项
            float easedProgress = GetEasedProgress(animationProgress);

            // 计算当前动画位置
            int startTop = GetItemTop(animationCurrentIndex);
            int targetTop = GetItemTop(animationTargetIndex);
            int currentTop = (int)(startTop + (targetTop - startTop) * easedProgress);

            // 确保位置在有效范围内
            currentTop = Math.Max(0, Math.Min(currentTop, Height - ItemHeight));

            UpdatePanelPositions(currentTop);

            // 强制重绘以获得更流畅的动画
            Invalidate();
            Update();
        }

        private void UpdatePanelPositions(int top)
        {
            PnlSelected.Top = top;
            PnlSelected.Height = ItemHeight;

            // 添加弹性效果
            if (animationProgress > 0.8f)
            {
                // 在动画结束时添加轻微的弹性效果
                float overshoot = (animationProgress - 0.8f) * 5f;
                int overshootAmount = (int)(Math.Sin(overshoot * Math.PI) * 2);
                PnlSelected.Top = top + overshootAmount;
            }

            // 更新悬停面板位置与选中面板同步（如果用户没有悬停在其他项目上）
            if (hoverIndex == selectIndex)
            {
                PnlHovered.Top = PnlSelected.Top;
                PnlHovered.Height = ItemHeight;
            }
        }

        private void CompleteAnimation()
        {
            animationProgress = 1f;
            isAnimating = false;
            animationTimer.Stop();

            SetSelectedIndexDirectly(animationTargetIndex);

            // 最终位置调整，确保准确对齐
            PnlSelected.Top = GetItemTop(selectIndex);
            if (hoverIndex == selectIndex)
            {
                PnlHovered.Top = PnlSelected.Top;
            }

            // 最终重绘确保界面正确
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

        private float GetEasedProgress(float progress)
        {
            // 提供多种缓动函数选择
            return EaseOutCubic(progress); // 你可以尝试不同的缓动函数
        }

        // 多种缓动函数选项
        private float EaseInOutCubic(float t)
        {
            return t < 0.5f ? 4 * t * t * t : 1 - (float)Math.Pow(-2 * t + 2, 3) / 2;
        }

        private float EaseOutCubic(float t)
        {
            return 1 - (float)Math.Pow(1 - t, 3);
        }

        private float EaseOutElastic(float t)
        {
            float c4 = (2 * (float)Math.PI) / 3;
            return t == 0 ? 0 :
                   t == 1 ? 1 :
                   (float)Math.Pow(2, -10 * t) * (float)Math.Sin((t * 10 - 0.75) * c4) + 1;
        }

        private float EaseOutBack(float t)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1;
            return 1 + c3 * (float)Math.Pow(t - 1, 3) + c1 * (float)Math.Pow(t - 1, 2);
        }

        public void BeginUpdate() => SuspendLayout();
        public void EndUpdate() { ResumeLayout(true); UpdateBackground(); }
        public int GetItemWidth(string str) => TextRenderer.MeasureText(str, Font).Width + 2 * HorizontalSpace;

        // 新增方法：立即停止动画
        public void StopAnimation()
        {
            if (isAnimating)
            {
                animationTimer.Stop();
                isAnimating = false;
                SetSelectedIndexDirectly(animationTargetIndex);
            }
        }

        // 新增方法：平滑滚动到指定索引
        public void SmoothScrollTo(int index)
        {
            if (index >= 0 && index < ItemNames?.Length)
            {
                SelectedIndex = index;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                animationTimer?.Stop();
                animationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeColors()
        {
            Color baseColor = IsDarkTheme ? Color.FromArgb(26, 26, 26) : SystemColors.Control;
            BackColor = baseColor;
            ForeColor = IsDarkTheme ? Color.WhiteSmoke : SystemColors.ControlText;
            HoveredBackColor = IsDarkTheme ? Color.FromArgb(51, 51, 51) : Color.FromArgb(230, 230, 230);
            SeparatorColor = IsDarkTheme ? Color.FromArgb(64, 64, 64) : Color.FromArgb(200, 200, 200);
            PnlSelected.BackColor = Color.Transparent;

            if (IsDarkTheme)
            {
                BackgroundGradientColor1 = Color.FromArgb(128, 128, 128);
                BackgroundGradientColor2 = Color.FromArgb(56, 56, 56);
                BackgroundGradientColor3 = Color.FromArgb(128, 128, 128);
            }
            else
            {
                BackgroundGradientColor1 = Color.FromArgb(255, 255, 255);
                BackgroundGradientColor2 = Color.FromArgb(230, 230, 230);
                BackgroundGradientColor3 = Color.FromArgb(255, 255, 255);
            }
        }

        private void UpdateBackground()
        {
            if (ItemNames == null) return;
            int bgWidth = Math.Max(1, Width);
            int bgHeight = CalculateValidBackgroundHeight();
            if (bgWidth <= 0 || bgHeight <= 0) return;

            try
            {
                var oldBackground = BackgroundImage;
                BackgroundImage = new Bitmap(bgWidth, bgHeight);
                oldBackground?.Dispose();

                using (var g = Graphics.FromImage(BackgroundImage))
                {
                    DrawBackgroundGradient(g, bgWidth, bgHeight);
                    if (ShouldDrawItems())
                    {
                        DrawTextItems(g);
                        DrawSeparators(g);
                    }
                }
            }
            catch (ArgumentException)
            {
                BackgroundImage?.Dispose();
                BackgroundImage = null;
            }
        }

        private int CalculateValidBackgroundHeight() => ItemNames == null || ItemNames.Length == 0 ?
            Math.Max(1, Height) : Math.Max(Height, Math.Max(0, ItemHeight) * ItemNames.Length);

        private bool ShouldDrawItems() => ItemNames != null && ItemNames.Length > 0 && ItemHeight > 0 && Width > 0 && Height > 0;

        private void DrawBackgroundGradient(Graphics g, int width, int height)
        {
            using (var brush = new LinearGradientBrush(new Rectangle(0, 0, width, height), Color.Empty, Color.Empty, 0f))
            {
                var blend = new ColorBlend
                {
                    Colors = new[] { BackgroundGradientColor1, BackgroundGradientColor2, BackgroundGradientColor3 },
                    Positions = new[] { 0f, 0.5f, 1f }
                };
                brush.InterpolationColors = blend;
                g.FillRectangle(brush, new Rectangle(0, 0, width, height));
            }
        }

        private void DrawTextItems(Graphics g)
        {
            if (ItemNames == null || ItemNames.Length == 0) return;
            using (var textBrush = new SolidBrush(ForeColor))
            {
                for (int i = 0; i < ItemNames.Length; i++)
                {
                    var item = ItemNames[i];
                    if (string.IsNullOrEmpty(item)) continue;
                    float yPos = TopSpace + i * ItemHeight + VerticalSpace;
                    g.DrawString(item, Font, textBrush, new PointF(HorizontalSpace, yPos));
                }
            }
        }

        private void DrawSeparators(Graphics g)
        {
            using (var pen = new Pen(SeparatorColor))
            {
                for (int i = 0; i < ItemNames.Length; i++)
                {
                    if (ItemNames[i] != null) continue;
                    float yPos = TopSpace + (i + 0.5F) * ItemHeight;
                    g.DrawLine(pen, HorizontalSpace, yPos, Width - HorizontalSpace, yPos);
                }
            }
        }

        private int CalculateItemIndex(int yPos)
        {
            if (ItemNames == null || ItemHeight <= 0) return -1;
            int relativeY = yPos - TopSpace;
            int index = relativeY / ItemHeight;
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
                int actualTop = TopSpace + index * ItemHeight;
                actualTop = Math.Max(0, Math.Min(actualTop, Height - ItemHeight));
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
                var blend = new ColorBlend
                {
                    Colors = new[] { SelectedGradientColor1, SelectedGradientColor2, SelectedGradientColor3 },
                    Positions = new[] { 0f, 0.5f, 1f }
                };
                brush.InterpolationColors = blend;
                e.Graphics.FillRectangle(brush, new Rectangle(0, 0, ctr.Width, ctr.Height));
            }
            DrawItemText(e, ctr, SelectedForeColor);
        }

        private void DrawItemText(PaintEventArgs e, Control ctr, Color textColor)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            Color useColor = textColor == Color.Empty ? ForeColor : textColor;
            using (var brush = new SolidBrush(useColor))
                e.Graphics.DrawString(ctr.Text, Font, brush, new PointF(HorizontalSpace, VerticalSpace));
        }

        private void ShowItem(Panel panel, MouseEventArgs e)
        {
            if (ItemNames == null) return;
            int index = CalculateItemIndex(e.Y);
            bool isValid = index != -1 && index != SelectedIndex;
            Cursor = isValid ? Cursors.Hand : Cursors.Default;
            if (isValid)
            {
                if (panel == PnlSelected)
                {
                    // 如果点击时正在动画，先停止动画
                    if (isAnimating)
                    {
                        StopAnimation();
                    }
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
        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified) =>
            base.SetBoundsCore(x, y, Math.Max(1, width), Math.Max(1, height), specified);
    }
}