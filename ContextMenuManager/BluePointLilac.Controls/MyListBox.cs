using BluePointLilac.Methods;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

namespace BluePointLilac.Controls
{
    public class MyListBox : Panel
    {
        private int scrollOffset;
        private int maxScrollOffset;
        private Timer scrollAnimationTimer;
        private const int ScrollAnimationDuration = 250;
        private const int ScrollAnimationInterval = 10;
        private Stopwatch animationStopwatch;
        private int scrollStartOffset;
        private int targetScrollOffset;
        private bool isScrolling;

        public MyListBox()
        {
            AutoScroll = false; // 禁用系统滚动条
            BackColor = MyMainForm.FormBack;
            ForeColor = MyMainForm.FormFore;
            
            // 初始化滚动动画计时器
            scrollAnimationTimer = new Timer { Interval = ScrollAnimationInterval };
            scrollAnimationTimer.Tick += ScrollAnimationTimer_Tick;
            animationStopwatch = new Stopwatch();
            
            // 启用双缓冲和优化绘制
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | 
                         ControlStyles.UserPaint | 
                         ControlStyles.OptimizedDoubleBuffer | 
                         ControlStyles.ResizeRedraw, true);
            
            // 处理鼠标滚轮事件
            this.MouseWheel += (sender, e) => {
                int scrollAmount = Math.Sign(e.Delta) * 60.DpiZoom();
                SmoothScrollBy(-scrollAmount);
            };
        }

        // 平滑滚动到指定位置
        public void SmoothScrollTo(int offset)
        {
            if (scrollAnimationTimer.Enabled)
                scrollAnimationTimer.Stop();

            targetScrollOffset = Math.Max(0, Math.Min(offset, maxScrollOffset));
            
            if (scrollOffset == targetScrollOffset)
                return;

            scrollStartOffset = scrollOffset;
            animationStopwatch.Restart();
            scrollAnimationTimer.Start();
            isScrolling = true;
        }

        // 平滑滚动指定距离
        public void SmoothScrollBy(int delta)
        {
            SmoothScrollTo(scrollOffset + delta);
        }

        // 平滑滚动到顶部
        public void SmoothScrollToTop()
        {
            SmoothScrollTo(0);
        }

        // 平滑滚动到底部
        public void SmoothScrollToBottom()
        {
            SmoothScrollTo(maxScrollOffset);
        }

        private void ScrollAnimationTimer_Tick(object sender, EventArgs e)
        {
            double elapsed = animationStopwatch.ElapsedMilliseconds;
            double progress = Math.Min(elapsed / ScrollAnimationDuration, 1.0);
            
            // 使用缓动函数使动画更自然
            double easedProgress = EaseOutQuart(progress);
            
            int newOffset = scrollStartOffset + (int)((targetScrollOffset - scrollStartOffset) * easedProgress);
            
            // 设置滚动位置
            SetScrollOffset(newOffset);
            
            if (progress >= 1.0)
            {
                scrollAnimationTimer.Stop();
                SetScrollOffset(targetScrollOffset); // 确保最终位置准确
                animationStopwatch.Stop();
                isScrolling = false;
            }
        }
        
        // 设置滚动偏移量
        private void SetScrollOffset(int offset)
        {
            if (scrollOffset != offset)
            {
                scrollOffset = Math.Max(0, Math.Min(offset, maxScrollOffset));
                UpdateListPosition();
                this.Invalidate(); // 重绘以显示滚动条
            }
        }
        
        // 更新列表位置
        private void UpdateListPosition()
        {
            if (Controls.Count > 0 && Controls[0] is MyList list)
            {
                list.Top = -scrollOffset;
            }
        }
        
        // 计算最大滚动偏移量
        internal void CalculateMaxScrollOffset()
        {
            if (Controls.Count > 0 && Controls[0] is MyList list)
            {
                maxScrollOffset = Math.Max(0, list.Height - this.Height);
            }
            else
            {
                maxScrollOffset = 0;
            }
            
            // 确保当前滚动位置在有效范围内
            if (scrollOffset > maxScrollOffset)
            {
                SetScrollOffset(maxScrollOffset);
            }
        }

        // 缓动函数 - 四次方缓出
        private double EaseOutQuart(double progress)
        {
            return 1 - Math.Pow(1 - progress, 4);
        }
        
        // 确保布局正确
        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            CalculateMaxScrollOffset();
            UpdateListPosition();
        }
        
        // 绘制自定义滚动条
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            // 如果需要滚动条且内容高度大于控件高度
            if (maxScrollOffset > 0)
            {
                int scrollbarWidth = 8.DpiZoom();
                int scrollbarRight = this.Width - scrollbarWidth - 2;
                
                // 计算滚动条高度和位置
                float visibleRatio = (float)this.Height / (this.Height + maxScrollOffset);
                int scrollbarHeight = Math.Max(30.DpiZoom(), (int)(this.Height * visibleRatio));
                
                float scrollRatio = (float)scrollOffset / maxScrollOffset;
                int scrollbarTop = (int)((this.Height - scrollbarHeight) * scrollRatio);
                
                // 绘制滚动条背景
                using (var bgBrush = new SolidBrush(Color.FromArgb(50, Color.Gray)))
                {
                    e.Graphics.FillRectangle(bgBrush, 
                        scrollbarRight, 0, scrollbarWidth, this.Height);
                }
                
                // 绘制滚动条
                using (var scrollBrush = new SolidBrush(Color.FromArgb(150, Color.DarkGray)))
                {
                    e.Graphics.FillRectangle(scrollBrush, 
                        scrollbarRight, scrollbarTop, scrollbarWidth, scrollbarHeight);
                }
            }
        }
        
        // 处理鼠标拖动滚动
        private Point dragStartPoint;
        private int dragStartOffset;
        private bool isDragging;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            
            // 检查是否点击在滚动条区域
            int scrollbarWidth = 8.DpiZoom();
            int scrollbarRight = this.Width - scrollbarWidth - 2;
            
            if (e.X >= scrollbarRight && e.X <= this.Width && maxScrollOffset > 0)
            {
                isDragging = true;
                dragStartPoint = e.Location;
                dragStartOffset = scrollOffset;
            }
        }
        
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            
            if (isDragging && maxScrollOffset > 0)
            {
                int deltaY = e.Y - dragStartPoint.Y;
                float scrollRatio = (float)maxScrollOffset / (this.Height - 8.DpiZoom());
                int newOffset = dragStartOffset + (int)(deltaY * scrollRatio);
                
                SetScrollOffset(newOffset);
                this.Invalidate();
            }
        }
        
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            isDragging = false;
        }
        
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isDragging = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                scrollAnimationTimer?.Stop();
                scrollAnimationTimer?.Dispose();
                animationStopwatch?.Stop();
            }
            base.Dispose(disposing);
        }
    }

    public class MyList : FlowLayoutPanel
    {
        public MyListBox Owner
        {
            get => (MyListBox)Parent;
            set => Parent = value;
        }

        public MyList(MyListBox owner) : this()
        {
            Owner = owner;
        }

        public MyList()
        {
            AutoSize = true;
            WrapContents = true;
            Dock = DockStyle.None; // 改为None，使用自定义定位
            DoubleBuffered = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            
            // 优化性能
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | 
                         ControlStyles.AllPaintingInWmPaint | 
                         ControlStyles.UserPaint, true);
        }

        private MyListItem hoveredItem;
        public MyListItem HoveredItem
        {
            get => hoveredItem;
            set
            {
                if (hoveredItem == value) return;
                if (hoveredItem != null)
                {
                    hoveredItem.StartColorAnimation(MyMainForm.FormFore);
                    hoveredItem.Font = new Font(hoveredItem.Font, FontStyle.Regular);
                }
                hoveredItem = value;
                if (hoveredItem != null)
                {
                    hoveredItem.StartColorAnimation(MyMainForm.MainColor);
                    hoveredItem.Font = new Font(hoveredItem.Font, FontStyle.Bold);
                    hoveredItem.Focus();
                }
                HoveredItemChanged?.Invoke(this, null);
            }
        }

        public event EventHandler HoveredItemChanged;

        public void AddItem(MyListItem item)
        {
            SuspendLayout();
            item.Parent = this;
            item.MouseEnter += (sender, e) => HoveredItem = item;
            MouseWheel += (sender, e) => item.ContextMenuStrip?.Close();

            // 淡入动画
            item.Opacity = 0;
            Timer fadeTimer = new Timer { Interval = 15 };
            fadeTimer.Tick += (sender, e) =>
            {
                if (item.Opacity >= 1)
                {
                    fadeTimer.Stop();
                    fadeTimer.Dispose();
                    return;
                }
                item.Opacity = Math.Min(item.Opacity + 0.1F, 1);
                item.Invalidate(); // 触发重绘
            };
            fadeTimer.Start();

            void ResizeItem() => item.Width = Owner.Width - item.Margin.Horizontal;
            Owner.Resize += (sender, e) => ResizeItem();
            ResizeItem();
            ResumeLayout();
            
            // 更新滚动范围
            if (Owner != null)
            {
                Owner.CalculateMaxScrollOffset();
                Owner.Invalidate();
            }
        }

        public void AddItems(MyListItem[] items)
        {
            Array.ForEach(items, item => AddItem(item));
        }

        public void AddItems(List<MyListItem> items)
        {
            items.ForEach(item => AddItem(item));
        }

        public void SetItemIndex(MyListItem item, int newIndex)
        {
            Controls.SetChildIndex(item, newIndex);
        }

        public int GetItemIndex(MyListItem item)
        {
            return Controls.GetChildIndex(item);
        }

        public void InsertItem(MyListItem item, int index)
        {
            if (item == null) return;
            AddItem(item);
            SetItemIndex(item, index);
        }

        public virtual void ClearItems()
        {
            if (Controls.Count == 0) return;
            SuspendLayout();
            for (int i = Controls.Count - 1; i >= 0; i--)
            {
                Control ctr = Controls[i];
                Controls.Remove(ctr);
                ctr.Dispose();
            }
            ResumeLayout();
            
            // 更新滚动范围
            if (Owner != null)
            {
                Owner.CalculateMaxScrollOffset();
                Owner.Invalidate();
            }
        }

        public void SortItemByText()
        {
            List<MyListItem> items = new List<MyListItem>();
            foreach (MyListItem item in Controls) items.Add(item);
            Controls.Clear();
            items.Sort(new TextComparer());
            items.ForEach(item => AddItem(item));
        }

        public class TextComparer : IComparer<MyListItem>
        {
            public int Compare(MyListItem x, MyListItem y)
            {
                if (x.Equals(y)) return 0;
                string[] strs = { x.Text, y.Text };
                Array.Sort(strs);
                if (strs[0] == x.Text) return -1;
                else return 1;
            }
        }
    }

    public class MyListItem : Panel
    {
        public MyListItem()
        {
            SuspendLayout();
            HasImage = true;
            DoubleBuffered = true;
            Height = 50.DpiZoom();
            Margin = new Padding(0);
            Font = SystemFonts.IconTitleFont;
            ForeColor = MyMainForm.FormFore;
            BackColor = MyMainForm.FormBack;
            
            // 优化性能
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | 
                         ControlStyles.AllPaintingInWmPaint | 
                         ControlStyles.UserPaint, true);
            
            // 创建FlowLayoutPanel并设置其属性
            flpControls = new FlowLayoutPanel
            {
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                Anchor = AnchorStyles.Right,
                AutoSize = true,
                Name = "Controls"
            };
            
            // 使用反射设置DoubleBuffered属性
            typeof(FlowLayoutPanel).GetProperty("DoubleBuffered", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(flpControls, true, null);
            
            Controls.AddRange(new Control[] { lblSeparator, flpControls, lblText, picImage });
            Resize += (Sender, e) => pnlScrollbar.Height = ClientSize.Height;
            flpControls.MouseClick += (sender, e) => OnMouseClick(e);
            flpControls.MouseEnter += (sender, e) => OnMouseEnter(e);
            flpControls.MouseDown += (sender, e) => OnMouseDown(e);
            lblSeparator.SetEnabled(false);
            lblText.SetEnabled(false);
            CenterControl(lblText);
            CenterControl(picImage);
            AddCtr(pnlScrollbar, 0);
            ResumeLayout();
        }

        public Image Image
        {
            get => picImage.Image;
            set => picImage.Image = value;
        }

        public new string Text
        {
            get => lblText.Text;
            set => lblText.Text = value;
        }

        public new Font Font
        {
            get => lblText.Font;
            set => lblText.Font = value;
        }

        public new Color ForeColor
        {
            get => lblText.ForeColor;
            set => lblText.ForeColor = value;
        }

        private bool hasImage;
        public bool HasImage
        {
            get => hasImage;
            set
            {
                hasImage = value;
                picImage.Visible = value;
                lblText.Left = (value ? 60 : 20).DpiZoom();
            }
        }

        private readonly Label lblText = new Label
        {
            AutoSize = true,
            Name = "Text"
        };

        private readonly PictureBox picImage = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.AutoSize,
            Left = 20.DpiZoom(),
            Enabled = false,
            Name = "Image"
        };

        private FlowLayoutPanel flpControls;

        private readonly Label lblSeparator = new Label
        {
            BackColor = MyMainForm.FormFore,
            Dock = DockStyle.Bottom,
            Name = "Separator",
            Height = 1
        }; // 分割线

        private readonly Panel pnlScrollbar = new Panel
        {
            Width = SystemInformation.VerticalScrollBarWidth,
            Enabled = false
        }; // 预留滚动条宽度

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e); OnMouseEnter(null);
        }

        private void CenterControl(Control ctr)
        {
            void reSize()
            {
                if (ctr.Parent == null) return;
                int top = (ClientSize.Height - ctr.Height) / 2;
                ctr.Top = top;
                if (ctr.Parent == flpControls)
                {
                    ctr.Margin = new Padding(0, top, ctr.Margin.Right, top);
                }
            }
            ctr.Parent.Resize += (sender, e) => reSize();
            ctr.Resize += (sender, e) => reSize();
            reSize();
        }

        public void AddCtr(Control ctr)
        {
            AddCtr(ctr, 20.DpiZoom());
        }

        public void AddCtr(Control ctr, int space)
        {
            SuspendLayout();
            ctr.Parent = flpControls;
            ctr.Margin = new Padding(0, 0, space, 0);
            ctr.MouseEnter += (sender, e) => OnMouseEnter(e);
            ctr.MouseDown += (sender, e) => OnMouseEnter(e);
            CenterControl(ctr);
            ResumeLayout();
        }

        public void AddCtrs(Control[] ctrs)
        {
            Array.ForEach(ctrs, ctr => AddCtr(ctr));
        }

        public void RemoveCtrAt(int index)
        {
            if (flpControls.Controls.Count > index) flpControls.Controls.RemoveAt(index + 1);
        }

        public int GetCtrIndex(Control ctr)
        {
            return flpControls.Controls.GetChildIndex(ctr, true) - 1;
        }

        public void SetCtrIndex(Control ctr, int newIndex)
        {
            flpControls.Controls.SetChildIndex(ctr, newIndex + 1);
        }

        private Timer colorAnimTimer;
        private Color startColor;
        private Color targetColor;
        private const int AnimDuration = 200; // 动画时长(ms)
        private const int AnimInterval = 15;  // 刷新间隔
        public float Opacity { get; set; } = 1f; // 添加透明度属性

        public void StartColorAnimation(Color newColor)
        {
            if (colorAnimTimer != null)
            {
                colorAnimTimer.Stop();
            }

            startColor = this.ForeColor;
            targetColor = newColor;
            colorAnimTimer = new Timer { Interval = AnimInterval };

            DateTime startTime = DateTime.Now;
            colorAnimTimer.Tick += (sender, e) =>
            {
                double progress = (DateTime.Now - startTime).TotalMilliseconds / AnimDuration;
                if (progress >= 1d)
                {
                    this.ForeColor = targetColor;
                    colorAnimTimer.Stop();
                    colorAnimTimer.Dispose();
                    return;
                }

                int r = (int)(startColor.R + (targetColor.R - startColor.R) * progress);
                int g = (int)(startColor.G + (targetColor.G - startColor.G) * progress);
                int b = (int)(startColor.B + (targetColor.B - startColor.B) * progress);
                this.ForeColor = Color.FromArgb(r, g, b);
                this.Invalidate(); // 触发重绘
            };

            colorAnimTimer.Start();
        }
    }
}