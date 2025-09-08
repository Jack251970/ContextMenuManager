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

            // 初始化动画计时器
            InitializeAnimationTimer();

            // 初始状态
            SelectedIndex = -1;
        }

        #region 动画实现
        private Timer AnimationTimer;
        private int AnimationTargetTop;
        private int AnimationCurrentTop;
        private const int AnimationSteps = 10;
        private int CurrentStep;

        private void InitializeAnimationTimer()
        {
            AnimationTimer = new Timer();
            AnimationTimer.Interval = 15; // 约60FPS
            AnimationTimer.Tick += AnimationTimer_Tick;
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (CurrentStep >= AnimationSteps)
            {
                AnimationTimer.Stop();
                PnlSelected.Top = AnimationTargetTop;
                return;
            }

            // 使用缓动函数使动画更平滑
            double progress = (double)CurrentStep / AnimationSteps;
            double easedProgress = 1 - Math.Pow(1 - progress, 2); // 缓出效果

            int newTop = AnimationCurrentTop + (int)((AnimationTargetTop - AnimationCurrentTop) * easedProgress);
            PnlSelected.Top = newTop;

            CurrentStep++;
        }

        private void StartSelectionAnimation(int targetTop)
        {
            if (AnimationTimer.Enabled)
                AnimationTimer.Stop();

            AnimationCurrentTop = PnlSelected.Top;
            AnimationTargetTop = targetTop;
            CurrentStep = 0;
            AnimationTimer.Start();
        }
        #endregion

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
                    DrawGradientBackground(g, bgWidth, bgHeight);
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
                IsDarkTheme ? Color.FromArgb(38, 38, 38) : Color.FromArgb(240, 240, 240),
                IsDarkTheme ? Color.FromArgb(13, 13, 13) : Color.FromArgb(200, 200, 200),
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
            Color.FromArgb(26, 26, 26) : 
            SystemColors.Control;
        
        // 修改选中颜色为黄色
        private Color HighlightColor => IsDarkTheme ? 
            Color.FromArgb(255, 204, 0) : // 深色模式下的黄色
            Color.FromArgb(255, 230, 50); // 浅色模式下的黄色
        #endregion

        #region 颜色绑定逻辑
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

            // 设置选中和悬停文字颜色
            SelectedForeColor = IsDarkTheme ? Color.Black : Color.Black;
            HoveredForeColor = IsDarkTheme ? Color.White : SystemColors.ControlText;
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

        private int itemHeight = 30;
        public int ItemHeight
        {
            get => itemHeight;
            set => itemHeight = Math.Max(1, value);
        }

        protected override void SetBoundsCore(int x, int y,
            int width, int height, BoundsSpecified specified)
        {
            base.SetBoundsCore(x, y,
                Math.Max(1, width),
                Math.Max(1, height),
                specified);
        }

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
            if (ItemNames == null || ItemHeight <= 0) return -1;

            int relativeY = yPos - TopSpace;
            int index = relativeY / ItemHeight;

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
                int actualTop = TopSpace + index * ItemHeight;
                actualTop = Math.Max(0, Math.Min(actualTop, Height - ItemHeight));

                // 如果是选中面板，使用动画效果
                if (panel == PnlSelected)
                {
                    StartSelectionAnimation(actualTop);
                }
                else
                {
                    panel.Top = actualTop;
                }
                
                panel.Text = ItemNames[index];
            }
            panel.Height = ItemHeight;
            panel.Refresh();
        }

        private void PaintItem(object sender, PaintEventArgs e)
        {
            var ctr = (Control)sender;
            if (string.IsNullOrEmpty(ctr.Text)) return;

            // 确定文字颜色
            Color textColor = ForeColor;
            if (ctr == PnlSelected)
                textColor = SelectedForeColor;
            else if (ctr == PnlHovered)
                textColor = HoveredForeColor;

            // 先绘制背景色再绘制文字
            e.Graphics.FillRectangle(new SolidBrush(ctr.BackColor),
                new Rectangle(0, 0, ctr.Width, ctr.Height));

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint =
                System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (var brush = new SolidBrush(textColor))
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
}