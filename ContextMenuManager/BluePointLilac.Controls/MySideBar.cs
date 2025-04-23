using BluePointLilac.Methods;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public sealed class MySideBar : Panel
    {
        public MySideBar()
        {
            // 双缓冲设置
            SetStyle(ControlStyles.OptimizedDoubleBuffer | 
                     ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.UserPaint, true);
            
            Dock = DockStyle.Left;
            ItemHeight = 30.DpiZoom();
            Font = new Font(SystemFonts.MenuFont.FontFamily, SystemFonts.MenuFont.Size + 1F);
            ForeColor = MyMainForm.FormFore;
            BackColor = MyMainForm.ButtonSecond;
            BackgroundImageLayout = ImageLayout.None;
            
            Controls.AddRange(new Control[] { LblSeparator, PnlSelected });
            PnlSelected.Paint += PaintItem;
            SelectedIndex = -1;

            animationTimer.Interval = 1; // 最小间隔1ms
            animationTimer.Tick += AnimationTick;
        }

        // 新增深色模式兼容属性
        public bool IsDarkMode { get; set; } = false; // 是否启用深色模式
        private Color _gradientColor1 = Color.FromArgb(240, 240, 240); // 浅灰
        private Color _gradientColor2 = Color.FromArgb(200, 200, 200); // 深灰
        public Color GradientColor1
        {
            get => IsDarkMode ? Darken(_gradientColor1) : _gradientColor1;
            set => _gradientColor1 = value;
        }
        public Color GradientColor2
        {
            get => IsDarkMode ? Darken(_gradientColor2) : _gradientColor2;
            set => _gradientColor2 = value;
        }
        public float GradientAngle { get; set; } = 90F; // 渐变角度

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
                    Array.ForEach(value, str => maxWidth = Math.Max(maxWidth, GetItemWidth(str)));
                    Width = maxWidth + 2 * HorizontalSpace;
                }
                PnlSelected.Width = Width;
                PaintItems();
                SelectedIndex = -1;
            }
        }

        private int itemHeight;
        public int ItemHeight
        {
            get => itemHeight;
            set => PnlSelected.Height = itemHeight = value;
        }
        public int TopSpace { get; set; } = 2.DpiZoom();
        public int HorizontalSpace { get; set; } = 20.DpiZoom();
        private float VerticalSpace => (itemHeight - TextHeight) * 0.5F;
        private int TextHeight => TextRenderer.MeasureText(" ", Font).Height;
        public bool IsFixedWidth { get; set; } = true;

        public Color SeparatorColor
        {
            get => LblSeparator.BackColor;
            set => LblSeparator.BackColor = value;
        }
        public Color SelectedBackColor { get; set; } = Color.FromArgb(180, 180, 180); // 灰色选中背景
        public Color HoveredBackColor { get; set; } = Color.FromArgb(220, 220, 220); // 灰色悬停背景
        public Color SelectedForeColor { get; set; } = Color.Black; // 默认文字颜色
        public Color HoveredForeColor { get; set; } = Color.Black; // 默认悬停文字颜色

        readonly Panel PnlSelected = new Panel
        {
            BackColor = Color.FromArgb(180, 180, 180),
            ForeColor = Color.Black,
            Enabled = false
        };
        readonly Label LblSeparator = new Label
        {
            BackColor = Color.DarkGray, // 分隔线颜色适配深色模式
            Dock = DockStyle.Right,
            Width = 1,
        };

        // 深色模式颜色调整方法
        private Color Darken(Color color)
        {
            int r = Math.Max(0, color.R - 50);
            int g = Math.Max(0, color.G - 50);
            int b = Math.Max(0, color.B - 50);
            return Color.FromArgb(r, g, b);
        }

        public int GetItemWidth(string str)
        {
            return TextRenderer.MeasureText(str, Font).Width + 2 * HorizontalSpace;
        }

        private void PaintItems()
        {
            BackgroundImage = new Bitmap(Width, ItemHeight * ItemNames.Length);
            using(Graphics g = Graphics.FromImage(BackgroundImage))
            using(var gradientBrush = new LinearGradientBrush(
                new Rectangle(0, 0, Width, Height),
                GradientColor1,
                GradientColor2,
                GradientAngle))
            {
                g.FillRectangle(gradientBrush, new Rectangle(0, 0, Width, Height));
                
                if(itemNames == null) return;
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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            // 绘制悬停渐变效果
            if(HoveredIndex >= 0 && HoveredIndex < itemNames.Length)
            {
                using(var hoverBrush = new LinearGradientBrush(
                    GetItemRect(HoveredIndex),
                    Color.FromArgb(50, HoveredBackColor.R, HoveredBackColor.G, HoveredBackColor.B),
                    Color.Transparent,
                    LinearGradientMode.Vertical))
                {
                    e.Graphics.FillRectangle(hoverBrush, GetItemRect(HoveredIndex));
                }
                
                // 绘制文字
                if(itemNames[HoveredIndex] != null)
                {
                    e.Graphics.DrawString(itemNames[HoveredIndex], Font,
                        new SolidBrush(IsDarkMode ? Color.White : Color.Black), // 深色模式文字颜色
                        new PointF(HorizontalSpace, 
                            TopSpace + HoveredIndex * ItemHeight + VerticalSpace));
                }
            }
        }

        private Timer animationTimer = new Timer { Interval = 1 }; // 1ms触发间隔
        private Stopwatch frameSw = new Stopwatch();
        private int startTop;
        private int targetTop;
        private const int AnimationDuration = 200; // 动画总时长200ms

        private void RefreshItem(int index)
        {
            if (index < -1 || index >= itemNames?.Length) return;

            animationTimer.Stop();
            frameSw.Restart();
            
            startTop = PnlSelected.Top;
            targetTop = index < 0 ? -ItemHeight : (TopSpace + index * ItemHeight);
            PnlSelected.Text = index < 0 ? null : ItemNames[index];
            
            if (startTop != targetTop)
            {
                animationTimer.Start();
            }
            else
            {
                PnlSelected.Top = targetTop;
                SelectIndexChanged?.Invoke(this, null);
            }
        }

        private void AnimationTick(object sender, EventArgs e)
        {
            double elapsed = frameSw.Elapsed.TotalMilliseconds;
            float progress = Math.Min(1f, (float)(elapsed / AnimationDuration));

            float t = CubicBezier(progress);
            int newTop = (int)Math.Round(startTop + (targetTop - startTop) * t);
            
            bool shouldUpdate = Math.Abs(PnlSelected.Top - newTop) >= 1 
                                || progress >= 0.99f;

            if (shouldUpdate)
            {
                PnlSelected.Top = newTop;
                PnlSelected.Invalidate();
            }

            if (progress >= 1f)
            {
                animationTimer.Stop();
                PnlSelected.Top = targetTop;
                SelectIndexChanged?.Invoke(this, null);
            }
        }

        private float CubicBezier(float t) 
            => t * t * (3f - 2f * t);

        private void PaintItem(object sender, PaintEventArgs e)
        {
            var panel = (Panel)sender;
            if(panel == PnlSelected)
            {
                e.Graphics.FillRectangle(new SolidBrush(SelectedBackColor), e.ClipRectangle);
                e.Graphics.DrawString(panel.Text, Font,
                    new SolidBrush(IsDarkMode ? Color.White : Color.Black), // 深色模式文字颜色
                    new PointF(HorizontalSpace, VerticalSpace));
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int index = CalculateIndex(e);
            HoveredIndex = IsValidIndex(index) ? index : -1;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if(e.Button == MouseButtons.Left)
            {
                int index = CalculateIndex(e);
                if(IsValidIndex(index)) SelectedIndex = index;
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            HoveredIndex = -1;
        }

        public event EventHandler SelectIndexChanged;
        public event EventHandler HoverIndexChanged;

        private int selectIndex;
        public int SelectedIndex
        {
            get => selectIndex;
            set
            {
                if(selectIndex == value) return;
                RefreshItem(value);
                selectIndex = value;
            }
        }

        private int hoverIndex = -1;
        public int HoveredIndex
        {
            get => hoverIndex;
            set
            {
                if(hoverIndex == value) return;
                int oldIndex = hoverIndex;
                hoverIndex = value;
                
                if(oldIndex != -1)
                    Invalidate(GetItemRect(oldIndex));
                if(hoverIndex != -1)
                    Invalidate(GetItemRect(hoverIndex));
                    
                HoverIndexChanged?.Invoke(this, null);
            }
        }

        private Rectangle GetItemRect(int index) => new Rectangle(
            0, 
            TopSpace + index * ItemHeight, 
            Width, 
            ItemHeight
        );

        private int CalculateIndex(MouseEventArgs e) => (e.Y - TopSpace) / ItemHeight;
        private bool IsValidIndex(int index) => 
            index >= 0 && index < itemNames.Length && !string.IsNullOrEmpty(itemNames[index]);
    }
}