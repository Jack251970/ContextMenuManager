using BluePointLilac.Methods;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public sealed class MySideBar : Panel
    {
        public MySideBar()
        {
            // 基础布局设置
            Dock = DockStyle.Left;
            ItemHeight = 30.DpiZoom();
            ItemNames = Array.Empty<string>();
            MinimumSize = new Size(1, 1);
            BackgroundImageLayout = ImageLayout.None;
            DoubleBuffered = true; // 启用双缓冲防止闪烁
            
            // 字体设置
            Font = new Font(SystemFonts.MenuFont.FontFamily, 
                SystemFonts.MenuFont.Size + 1F);
            
            // 动态颜色初始化
            InitializeColors();
            
            // 控件组装
            Controls.AddRange(new Control[] { LblSeparator, PnlSelected, PnlHovered });

            // 新增尺寸变化处理
            SizeChanged += (sender, e) => UpdateBackground();

            // 事件绑定
            PnlHovered.Paint += PaintItem;
            PnlSelected.Paint += PaintItem;
            PnlSelected.BackColor = SelectedBackColor;
            PnlHovered.BackColor = HoveredBackColor;

            // 初始状态
            SelectedIndex = -1;
        }

        private void UpdateBackground()
        {
            if (ItemNames == null) return;

            int bgWidth = Math.Max(1, Width);
            int bgHeight = CalculateValidBackgroundHeight();

            if (bgWidth <= 0 || bgHeight <= 0) return;

            try
            {
                BackgroundImage = new Bitmap(bgWidth, bgHeight);
                using (var g = Graphics.FromImage(BackgroundImage))
                {
                    DrawGradientBackground(g, bgWidth, bgHeight); // 正确传递三个参数
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


        private void DrawGradientBackground(Graphics g, int width, int height)
        {
            using (var brush = new LinearGradientBrush(
                new Rectangle(0, 0, width, height),
                IsDarkTheme ? Color.FromArgb(38, 38, 38) : Color.FromArgb(240, 240, 240), // 浅色起始
                IsDarkTheme ? Color.FromArgb(13, 13, 13) : Color.FromArgb(200, 200, 200), // 浅色结束
                0f))
            {
                g.FillRectangle(brush, new Rectangle(0, 0, width, height));
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


        #region 主题管理系统
        private bool IsDarkTheme => ControlPaint.Light(BackColor).GetBrightness() < 0.5f;
        
        private Color BaseColor => IsDarkTheme ? 
            Color.FromArgb(26, 26, 26) :  // 深色模式基础黑
            SystemColors.Control;
        
        private Color HighlightColor => IsDarkTheme ? 
            Color.FromArgb(0, 102, 204) : // 深色模式强调色
            SystemColors.Highlight;
        #endregion
        #region 修复2：增强颜色绑定逻辑
        private void InitializeColors()
        {
            BackColor = BaseColor;
            ForeColor = IsDarkTheme ? Color.WhiteSmoke : SystemColors.ControlText;

            // 确保颜色绑定生效
            PnlSelected.BackColor = SelectedBackColor = HighlightColor;
            PnlHovered.BackColor = HoveredBackColor = IsDarkTheme ?
                Color.FromArgb(51, 51, 51) :
                Color.FromArgb(230, 230, 230);

            LblSeparator.BackColor = SeparatorColor = IsDarkTheme ?
                Color.FromArgb(64, 64, 64) :
                Color.FromArgb(200, 200, 200);
        }
        #endregion

        #region 属性声明
        private string[] itemNames;
        public string[] ItemNames
        {
            get => itemNames;
            set
            {
                itemNames = value;
                if(value != null && !IsFixedWidth)
                {
                    int maxWidth = 0;
                    Array.ForEach(value, str => 
                        maxWidth = Math.Max(maxWidth, GetItemWidth(str)));
                    Width = maxWidth + 2 * HorizontalSpace;
                }
                PnlHovered.Width = PnlSelected.Width = Width;
                PaintItems();
                SelectedIndex = -1;
            }
        }

        #region 增强尺寸校验
        private int itemHeight = 30;
        public int ItemHeight
        {
            get => itemHeight;
            set => itemHeight = Math.Max(1, value); // 强制最小高度为1
        }

        protected override void SetBoundsCore(int x, int y,
            int width, int height, BoundsSpecified specified)
        {
            // 强制有效尺寸设置
            base.SetBoundsCore(x, y,
                Math.Max(1, width),
                Math.Max(1, height),
                specified);
        }
        #endregion


        public int TopSpace { get; set; } = 2.DpiZoom();
        public int HorizontalSpace { get; set; } = 20.DpiZoom();
        private float VerticalSpace => (itemHeight - TextHeight) * 0.5F;
        private int TextHeight => TextRenderer.MeasureText(" ", Font).Height;
        public bool IsFixedWidth { get; set; } = true;

        public Color SeparatorColor { get; set; }
        public Color SelectedBackColor { get; set; }
        public Color HoveredBackColor { get; set; }
        public Color SelectedForeColor { get; set; }
        public Color HoveredForeColor { get; set; }
        #endregion

        #region 控件实例
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
        #endregion

        #region 核心绘制逻辑
        public int GetItemWidth(string str)
        {
            return TextRenderer.MeasureText(str, Font).Width + 2 * HorizontalSpace;
        }

        private void PaintItems()
        {
        UpdateBackground();
        }

        private void DrawGradientBackground(Graphics g)
        {
            using(var brush = new LinearGradientBrush(
                new Rectangle(0, 0, Width, Height),
                IsDarkTheme ? Color.FromArgb(38, 38, 38) : Color.AliceBlue,
                IsDarkTheme ? Color.FromArgb(13, 13, 13) : Color.LightSteelBlue,
                0f))
            {
                g.FillRectangle(brush, ClientRectangle);
            }
        }


        private void DrawTextItems(Graphics g)
        {
            // 添加双重验证
            if (ItemNames == null || ItemNames.Length == 0) return; // 关键修复4

            using (var textBrush = new SolidBrush(ForeColor))
            {
                for (int i = 0; i < ItemNames.Length; i++)
                {
                    // 添加元素级空值检查
                    var item = ItemNames[i];
                    if (string.IsNullOrEmpty(item)) continue; // 关键修复5

                    float yPos = TopSpace + i * ItemHeight + VerticalSpace;
                    g.DrawString(item, Font, textBrush,
                        new PointF(HorizontalSpace, yPos));
                }
            }
        }

        private void DrawSeparators(Graphics g)
        {
            using(var pen = new Pen(SeparatorColor))
            {
                for(int i = 0; i < ItemNames.Length; i++)
                {
                    if(ItemNames[i] != null) continue;
                    
                    float yPos = TopSpace + (i + 0.5F) * ItemHeight;
                    g.DrawLine(pen, HorizontalSpace, yPos, 
                        Width - HorizontalSpace, yPos);
                }
            }
        }
        #endregion
        private int CalculateItemIndex(int yPos)
        {
            // 修正索引计算逻辑（关键修复4）
            if (ItemNames == null || ItemHeight <= 0) return -1;

            int relativeY = yPos - TopSpace;
            int index = relativeY / ItemHeight;

            // 添加范围验证
            if (index < 0 || index >= ItemNames.Length) return -1;
            if (string.IsNullOrEmpty(ItemNames[index])) return -1;

            return index;
        }
        #region 修复选中高度异常的关键修改
        private void RefreshItem(Panel panel, int index)
        {
            if (index < 0 || index >= ItemNames?.Length)
            {
                panel.Top = -ItemHeight;
                panel.Text = null;
            }
            else
            {
                // 修正定位计算（关键修复1）
                int actualTop = TopSpace + index * ItemHeight;

                // 添加边界检查（关键修复2）
                actualTop = Math.Max(0, Math.Min(actualTop, Height - ItemHeight));

                panel.Top = actualTop;
                panel.Text = ItemNames[index];
            }
            panel.Height = ItemHeight; // 强制保持正确高度（关键修复3）
            panel.Refresh();
        }

        #region 修复3：优化选中状态绘制逻辑
        private void PaintItem(object sender, PaintEventArgs e)
        {
            var ctr = (Control)sender;
            if (string.IsNullOrEmpty(ctr.Text)) return;

            // 先绘制背景色再绘制文字
            e.Graphics.FillRectangle(new SolidBrush(ctr.BackColor),
                new Rectangle(0, 0, ctr.Width, ctr.Height));

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint =
                System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (var brush = new SolidBrush(ctr.ForeColor))
            {
                e.Graphics.DrawString(ctr.Text, Font, brush,
                    new PointF(HorizontalSpace, VerticalSpace));
            }
        }
        #endregion

        #region 修正鼠标事件处理
        private void ShowItem(Panel panel, MouseEventArgs e)
        {
            if (ItemNames == null) return;

            // 使用修正后的计算方法（关键修复5）
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
        #endregion

        #region 事件处理
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            ShowItem(PnlHovered, e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if(e.Button == MouseButtons.Left) 
                ShowItem(PnlSelected, e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            HoveredIndex = SelectedIndex;
        }
        #endregion

        #region 状态管理系统
        public event EventHandler SelectIndexChanged;
        public event EventHandler HoverIndexChanged;

        private int selectIndex;
        public int SelectedIndex
        {
            get => selectIndex;
            set
            {
                if(selectIndex == value) return;
                selectIndex = value;
                RefreshItem(PnlSelected, value);
                HoveredIndex = value;
                SelectIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private int hoverIndex;
        public int HoveredIndex
        {
            get => hoverIndex;
            set
            {
                if(hoverIndex == value) return;
                hoverIndex = value;
                RefreshItem(PnlHovered, value);
                HoverIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        #endregion

        
        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);
            InitializeColors();
            PaintItems();
        }

        public void BeginUpdate() => SuspendLayout();
        public void EndUpdate()
        {
            ResumeLayout(true);
            PaintItems();
        }

    }
    #endregion
}