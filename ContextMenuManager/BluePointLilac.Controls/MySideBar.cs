using BluePointLilac.Methods;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public sealed class MySideBar : Panel
    {
        // 控件实例
        private readonly Panel PnlSelected = new Panel
        {
            Enabled = false,
            BackColor = Color.Transparent
        };

        private readonly Panel PnlHovered = new Panel
        {
            Enabled = false,
            BackColor = Color.Transparent
        };

        private readonly Label LblSeparator = new Label
        {
            Dock = DockStyle.Right,
            Width = 1
        };

        // 私有字段
        private string[] itemNames;
        private int itemHeight = 30;
        private int selectIndex = -1;
        private int hoverIndex = -1;
        private Color baseColor;
        private Color highlightColor;
        private Color separatorColor;
        private Color selectedBackColor;
        private Color hoveredBackColor;

        // 黄色系三色渐变属性
        public Color SelectedGradientColor1 { get; set; } = Color.FromArgb(255, 235, 59);   // 亮黄色
        public Color SelectedGradientColor2 { get; set; } = Color.FromArgb(255, 196, 0); // 浅黄色
        public Color SelectedGradientColor3 { get; set; } = Color.FromArgb(255, 235, 59);   // 亮黄色

        // 新增：侧边栏背景三色渐变属性
        public Color BackgroundGradientColor1 { get; set; } = Color.FromArgb(240, 240, 240); // 浅灰色
        public Color BackgroundGradientColor2 { get; set; } = Color.FromArgb(220, 220, 220); // 中灰色
        public Color BackgroundGradientColor3 { get; set; } = Color.FromArgb(200, 200, 200); // 深灰色

        // 公共属性
        public string[] ItemNames
        {
            get => itemNames;
            set
            {
                itemNames = value;
                if (value != null && !IsFixedWidth)
                {
                    int maxWidth = 0;
                    Array.ForEach(value, str =>
                        maxWidth = Math.Max(maxWidth, GetItemWidth(str)));
                    Width = maxWidth + 2 * HorizontalSpace;
                }
                PnlHovered.Width = PnlSelected.Width = Width;
                UpdateBackground();
                SelectedIndex = -1;
            }
        }

        public int ItemHeight
        {
            get => itemHeight;
            set => itemHeight = Math.Max(1, value);
        }

        public int TopSpace { get; set; } = 2.DpiZoom();
        public int HorizontalSpace { get; set; } = 20.DpiZoom();
        public bool IsFixedWidth { get; set; } = true;

        public Color SeparatorColor
        {
            get => separatorColor;
            set
            {
                separatorColor = value;
                LblSeparator.BackColor = value;
            }
        }

        public Color SelectedBackColor
        {
            get => selectedBackColor;
            set
            {
                selectedBackColor = value;
                PnlSelected.BackColor = value;
            }
        }

        public Color HoveredBackColor
        {
            get => hoveredBackColor;
            set
            {
                hoveredBackColor = value;
                PnlHovered.BackColor = value;
            }
        }

        public Color SelectedForeColor { get; set; } = Color.Black;
        public Color HoveredForeColor { get; set; }

        // 计算属性
        private float VerticalSpace => (itemHeight - TextHeight) * 0.5F;
        private int TextHeight => TextRenderer.MeasureText(" ", Font).Height;
        private bool IsDarkTheme => ControlPaint.Light(BackColor).GetBrightness() < 0.5f;

        // 事件
        public event EventHandler SelectIndexChanged;
        public event EventHandler HoverIndexChanged;

        public int SelectedIndex
        {
            get => selectIndex;
            set
            {
                if (selectIndex == value) return;
                selectIndex = value;
                RefreshItem(PnlSelected, value);
                HoveredIndex = value;
                SelectIndexChanged?.Invoke(this, EventArgs.Empty);
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

        // 构造函数
        public MySideBar()
        {
            // 基础布局设置
            Dock = DockStyle.Left;
            MinimumSize = new Size(1, 1);
            BackgroundImageLayout = ImageLayout.None;
            DoubleBuffered = true;

            // 字体设置
            Font = new Font(SystemFonts.MenuFont.FontFamily,
                SystemFonts.MenuFont.Size + 1F);

            // 动态颜色初始化
            InitializeColors();

            // 控件组装
            Controls.AddRange(new Control[] { LblSeparator, PnlSelected, PnlHovered });

            // 事件绑定
            SizeChanged += (sender, e) => UpdateBackground();
            PnlHovered.Paint += PaintHoveredItem;
            PnlSelected.Paint += PaintSelectedItem;

            // 初始状态
            SelectedIndex = -1;
        }

        // 公共方法
        public void BeginUpdate() => SuspendLayout();

        public void EndUpdate()
        {
            ResumeLayout(true);
            UpdateBackground();
        }

        public int GetItemWidth(string str)
        {
            return TextRenderer.MeasureText(str, Font).Width + 2 * HorizontalSpace;
        }

        // 私有方法
        private void InitializeColors()
        {
            // 计算主题相关颜色
            baseColor = IsDarkTheme ? Color.FromArgb(26, 26, 26) : SystemColors.Control;
            highlightColor = IsDarkTheme ? Color.FromArgb(0, 102, 204) : SystemColors.Highlight;

            // 设置颜色
            BackColor = baseColor;
            ForeColor = IsDarkTheme ? Color.WhiteSmoke : SystemColors.ControlText;

            HoveredBackColor = IsDarkTheme ? Color.FromArgb(51, 51, 51) : Color.FromArgb(230, 230, 230);
            SeparatorColor = IsDarkTheme ? Color.FromArgb(64, 64, 64) : Color.FromArgb(200, 200, 200);

            // 选中状态使用渐变，不设置单一背景色
            PnlSelected.BackColor = Color.Transparent;

            // 设置背景渐变颜色
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

        private int CalculateValidBackgroundHeight()
        {
            int minHeight = Math.Max(1, Height);
            if (ItemNames == null || ItemNames.Length == 0) return minHeight;

            int itemsHeight = Math.Max(0, ItemHeight) * ItemNames.Length;
            return Math.Max(minHeight, itemsHeight);
        }

        private bool ShouldDrawItems()
        {
            return ItemNames != null
                && ItemNames.Length > 0
                && ItemHeight > 0
                && Width > 0
                && Height > 0;
        }

        // 修改：使用三色渐变绘制背景
        private void DrawBackgroundGradient(Graphics g, int width, int height)
        {
            using (var brush = new LinearGradientBrush(
                new Rectangle(0, 0, width, height),
                Color.Empty, Color.Empty, 0f))
            {
                // 设置三色渐变
                var blend = new ColorBlend
                {
                    Colors = new[] { BackgroundGradientColor1, BackgroundGradientColor2, BackgroundGradientColor3 },
                    Positions = new[] { 0f, 0.5f, 1f }
                };
                brush.InterpolationColors = blend;

                // 填充渐变背景
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
                    g.DrawString(item, Font, textBrush,
                        new PointF(HorizontalSpace, yPos));
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
                    g.DrawLine(pen, HorizontalSpace, yPos,
                        Width - HorizontalSpace, yPos);
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

        // 悬停状态绘制方法
        private void PaintHoveredItem(object sender, PaintEventArgs e)
        {
            var ctr = (Control)sender;
            if (string.IsNullOrEmpty(ctr.Text)) return;

            // 悬停状态使用单色背景
            e.Graphics.FillRectangle(new SolidBrush(HoveredBackColor),
                new Rectangle(0, 0, ctr.Width, ctr.Height));

            DrawItemText(e, ctr, HoveredForeColor);
        }

        // 选中状态使用黄色系三色渐变
        private void PaintSelectedItem(object sender, PaintEventArgs e)
        {
            var ctr = (Control)sender;
            if (string.IsNullOrEmpty(ctr.Text)) return;

            // 创建三色渐变
            using (var brush = new LinearGradientBrush(
                new Rectangle(0, 0, ctr.Width, ctr.Height),
                Color.Empty, Color.Empty, 0f))
            {
                // 设置黄色系三色渐变
                var blend = new ColorBlend
                {
                    Colors = new[] { SelectedGradientColor1, SelectedGradientColor2, SelectedGradientColor3 },
                    Positions = new[] { 0f, 0.5f, 1f }
                };
                brush.InterpolationColors = blend;

                // 填充渐变背景
                e.Graphics.FillRectangle(brush, new Rectangle(0, 0, ctr.Width, ctr.Height));
            }

            DrawItemText(e, ctr, SelectedForeColor);
        }

        // 提取文本绘制逻辑
        private void DrawItemText(PaintEventArgs e, Control ctr, Color textColor)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 如果未指定文字颜色，则使用默认前景色
            Color useColor = textColor == Color.Empty ? ForeColor : textColor;

            using (var brush = new SolidBrush(useColor))
            {
                e.Graphics.DrawString(ctr.Text, Font, brush,
                    new PointF(HorizontalSpace, VerticalSpace));
            }
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
                    SelectedIndex = index;
                else
                    HoveredIndex = index;
            }
            else
            {
                HoveredIndex = SelectedIndex;
            }
        }

        // 事件处理
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            ShowItem(PnlHovered, e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
                ShowItem(PnlSelected, e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            HoveredIndex = SelectedIndex;
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);
            InitializeColors();
            UpdateBackground();
        }

        protected override void SetBoundsCore(int x, int y,
            int width, int height, BoundsSpecified specified)
        {
            base.SetBoundsCore(x, y,
                Math.Max(1, width),
                Math.Max(1, height),
                specified);
        }
    }
}