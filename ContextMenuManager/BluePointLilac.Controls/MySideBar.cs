using BluePointLilac.Methods;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public sealed class MySideBar : Panel
    {
        #region 核心字段与动画参数
        private string[] itemNames;
        private int itemHeight;
        private int selectIndex = -1;
        private int hoverIndex = -1;
        private int stepCounter = 0; // 动画步数计数器
        private int lastHoverIndex = -1; // 悬浮状态缓存
        
        // 动画配置
        private const int AnimationSteps = 10; // 动画总帧数
        private Timer animationTimer;
        private int targetTop = -1;   // 目标位置
        private int currentTop = -1;  // 当前实际位置
        
        // 布局常量
        public int TopSpace { get; set; } = 2.DpiZoom();
        public int HorizontalSpace { get; set; } = 20.DpiZoom();
        private float VerticalSpace => (itemHeight - TextHeight) * 0.5F;
        private int TextHeight => TextRenderer.MeasureText(" ", Font).Height;
        public bool IsFixedWidth { get; set; } = true;
        #endregion

        #region 控件实例与初始化
        readonly Panel PnlSelected = new Panel
        {
            BackColor = MyMainForm.MainColor,
            ForeColor = MyMainForm.FormFore,
            Enabled = false
        };

        readonly Panel PnlHovered = new Panel
        {
            BackColor = MyMainForm.ButtonSecond,
            ForeColor = MyMainForm.FormFore,
            Enabled = false
        };

        readonly Label LblSeparator = new Label
        {
            BackColor = MyMainForm.FormFore,
            Dock = DockStyle.Right,
            Width = 1,
        };

        public MySideBar()
        {
            Dock = DockStyle.Left;
            ItemHeight = 30.DpiZoom();
            Font = new Font(SystemFonts.MenuFont.FontFamily, SystemFonts.MenuFont.Size + 1F);
            ForeColor = MyMainForm.FormFore;
            BackColor = MyMainForm.ButtonSecond;
            BackgroundImageLayout = ImageLayout.None;
            
            // 启用双缓冲
            SetStyle(ControlStyles.OptimizedDoubleBuffer | 
                     ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.UserPaint, true);
            DoubleBuffered = true;

            Controls.AddRange(new Control[] { LblSeparator, PnlSelected, PnlHovered });
            PnlHovered.Paint += PaintItem;
            PnlSelected.Paint += PaintItem;
            
            animationTimer = new Timer { Interval = 10 };
            animationTimer.Tick += AnimateSelection;

            // 延迟执行默认选中（保证布局完成）
            var initTimer = new Timer { Interval = 1 };
            initTimer.Tick += (s, e) =>
            {
                initTimer.Stop();
                if(ItemNames?.Length > 0) SelectedIndex = 0;
            };
            initTimer.Start();
        }
        #endregion

        #region 主要属性与资源管理
        public string[] ItemNames
        {
            get => itemNames;
            set
            {
                itemNames = value;
                if(value != null && !IsFixedWidth)
                {
                    int maxWidth = 0;
                    Array.ForEach(value, str => maxWidth = Math.Max(maxWidth, GetItemWidth(str)));
                    Width = maxWidth + 2 * HorizontalSpace;
                }
                PnlHovered.Width = PnlSelected.Width = Width;
                PaintItems();
                SelectedIndex = -1;
                
                if(ItemNames?.Length > 0) SelectedIndex = 0;
            }
        }

        public int ItemHeight
        {
            get => itemHeight;
            set => PnlHovered.Height = PnlSelected.Height = itemHeight = value;
        }

        public Color SeparatorColor
        {
            get => LblSeparator.BackColor;
            set => LblSeparator.BackColor = value;
        }
        
        public Color SelectedBackColor
        {
            get => PnlSelected.BackColor;
            set => PnlSelected.BackColor = value;
        }
        
        public Color HoveredBackColor
        {
            get => PnlHovered.BackColor;
            set => PnlHovered.BackColor = value;
        }
        
        public Color SelectedForeColor
        {
            get => PnlSelected.ForeColor;
            set => PnlSelected.ForeColor = value;
        }
        
        public Color HoveredForeColor
        {
            get => PnlHovered.ForeColor;
            set => PnlHovered.ForeColor = value;
        }
        #endregion

        #region 动画增强功能
        private void AnimateSelection(object sender, EventArgs e)
        {
            if(itemNames == null || itemNames.Length == 0) return;
            if(currentTop == targetTop || stepCounter >= AnimationSteps)
            {
                animationTimer.Stop();
                currentTop = targetTop;
                PnlSelected.Invalidate(); // 强制重绘
                return;
            }

            double progress = (double)++stepCounter / AnimationSteps;
            PnlSelected.Top = (int)(currentTop + (targetTop - currentTop) * progress);
        }

        private void RefreshItem(Panel panel, int index)
        {
            if(panel == PnlSelected && index != -1)
            {
                targetTop = TopSpace + index * ItemHeight;
                
                // 关键修复：仅当目标位置变化时启动动画
                if(currentTop == targetTop) return;
                
                if(currentTop == -1) 
                {
                    currentTop = targetTop;
                    PnlSelected.Top = currentTop;
                }
                else
                {
                    stepCounter = 0;
                    animationTimer.Start();
                }
            }
            else
            {
                panel.Top = index < 0 ? -ItemHeight : TopSpace + index * ItemHeight;
            }
            
            // 仅当文本变化时刷新
            if(panel.Text != (index < 0 ? null : ItemNames?[index]))
            {
                panel.Text = index < 0 ? null : ItemNames?[index];
                panel.Refresh();
            }
        }
        #endregion

        #region 绘制与交互逻辑
        public int GetItemWidth(string str) =>
            TextRenderer.MeasureText(str, Font).Width + 2 * HorizontalSpace;

        private void PaintItems()
        {
            if(itemNames == null) return;
            
            BackgroundImage?.Dispose();
            BackgroundImage = new Bitmap(Width, ItemHeight * itemNames.Length);
            
            using(Graphics g = Graphics.FromImage(BackgroundImage))
            {
                g.Clear(BackColor);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                
                for(int i = 0; i < itemNames.Length; i++)
                {
                    if(itemNames[i] != null)
                    {
                        g.DrawString(itemNames[i], Font, new SolidBrush(ForeColor),
                            new PointF(HorizontalSpace, TopSpace + i * ItemHeight + VerticalSpace));
                    }
                    else
                    {
                        g.DrawLine(new Pen(SeparatorColor),
                            new PointF(HorizontalSpace, TopSpace + (i + 0.5F) * ItemHeight),
                            new PointF(Width - HorizontalSpace, TopSpace + (i + 0.5F) * ItemHeight)
                        );
                    }
                }
            }
        }

        private void ShowItem(Panel panel, MouseEventArgs e)
        {
            if(itemNames == null) return;
            
            int index = (e.Y - TopSpace) / ItemHeight;
            
            if(index >= itemNames.Length || index < 0 || 
              string.IsNullOrEmpty(itemNames[index]) || index == SelectedIndex)
            {
                Cursor = Cursors.Default;
                HoveredIndex = SelectedIndex;
            }
            else
            {
                Cursor = Cursors.Hand;
                if(panel == PnlSelected) SelectedIndex = index;
                else
                {
                    if(lastHoverIndex != index)
                    {
                        HoveredIndex = index;
                        lastHoverIndex = index;
                    }
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            ShowItem(PnlHovered, e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if(e.Button == MouseButtons.Left) ShowItem(PnlSelected, e);
        }
        
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            HoveredIndex = SelectedIndex;
        }
        
        private void PaintItem(object sender, PaintEventArgs e)
        {
            Control ctr = (Control)sender;
            using (Brush brush = new SolidBrush(ctr.ForeColor))
            {
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                e.Graphics.DrawString(ctr.Text, Font, brush,
                    new PointF(HorizontalSpace, VerticalSpace));
            }
        }
        #endregion

        #region 事件与索引管理
        public event EventHandler SelectIndexChanged;
        public event EventHandler HoverIndexChanged;

        public int SelectedIndex
        {
            get => selectIndex;
            set
            {
                if(selectIndex == value) return;
                HoveredIndex = value;
                RefreshItem(PnlSelected, value);
                selectIndex = value;
                SelectIndexChanged?.Invoke(this, null);
            }
        }

        public int HoveredIndex
        {
            get => hoverIndex;
            set
            {
                if(hoverIndex == value) return;
                RefreshItem(PnlHovered, value);
                hoverIndex = value;
                HoverIndexChanged?.Invoke(this, null);
            }
        }
        #endregion
    }
}