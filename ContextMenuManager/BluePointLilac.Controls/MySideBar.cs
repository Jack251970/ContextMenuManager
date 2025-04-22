using BluePointLilac.Methods;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public sealed class MySideBar : Panel
    {
        public MySideBar()
        {
            Dock = DockStyle.Left;
            ItemHeight = 30.DpiZoom();
            Font = new Font(SystemFonts.MenuFont.FontFamily, SystemFonts.MenuFont.Size + 1F);
            ForeColor = MyMainForm.FormFore;
            BackColor = MyMainForm.ButtonSecond;
            BackgroundImageLayout = ImageLayout.None;
            
            Controls.AddRange(new Control[] { LblSeparator, PnlSelected });
            PnlSelected.Paint += PaintItem;
            SelectedIndex = -1;

            animationTimer.Tick += AnimationTick;
        }

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
        public Color SelectedBackColor { get; set; } = MyMainForm.MainColor;
        public Color HoveredBackColor { get; set; } = MyMainForm.ButtonSecond;
        public Color SelectedForeColor { get; set; } = MyMainForm.FormFore;

        readonly Panel PnlSelected = new Panel
        {
            BackColor = MyMainForm.MainColor,
            ForeColor = MyMainForm.FormFore,
            Enabled = false
        };
        readonly Label LblSeparator = new Label
        {
            BackColor = MyMainForm.FormFore,
            Dock = DockStyle.Right,
            Width = 1,
        };

        public int GetItemWidth(string str)
        {
            return TextRenderer.MeasureText(str, Font).Width + 2 * HorizontalSpace;
        }

        private void PaintItems()
        {
            BackgroundImage = new Bitmap(Width, ItemHeight * ItemNames.Length);
            using(Graphics g = Graphics.FromImage(BackgroundImage))
            {
                g.Clear(BackColor);
                if(itemNames == null) return;
                
                for(int i = 0; i < itemNames.Length; i++)
                {
                    if(i == HoveredIndex)
                    {
                        Rectangle rect = new Rectangle(0, 
                            TopSpace + i * ItemHeight, 
                            Width, 
                            ItemHeight);
                        g.FillRectangle(new SolidBrush(HoveredBackColor), rect);
                    }

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

        private Timer animationTimer = new Timer { Interval = 15 };
        private int startTop;
        private int targetTop;
        private float animationProgress;

        private void RefreshItem(int index)
        {
            if(animationTimer.Enabled) animationTimer.Stop();

            startTop = PnlSelected.Top;
            targetTop = index < 0 ? -ItemHeight : (TopSpace + index * ItemHeight);
            PnlSelected.Text = index < 0 ? null : ItemNames[index];
            
            animationProgress = 0F;
            animationTimer.Start();
        }

        private void AnimationTick(object sender, EventArgs e)
        {
            animationProgress += 0.1F;
            PnlSelected.Top = (int)Lerp(startTop, targetTop, EaseOutQuad(animationProgress));

            if(animationProgress >= 1F)
            {
                animationTimer.Stop();
                PnlSelected.Top = targetTop;
                PnlSelected.Refresh();
            }
        }

        private void PaintItem(object sender, PaintEventArgs e)
        {
            var panel = (Panel)sender;
            if(panel == PnlSelected)
            {
                e.Graphics.FillRectangle(new SolidBrush(SelectedBackColor), e.ClipRectangle);
                e.Graphics.DrawString(panel.Text, Font,
                    new SolidBrush(SelectedForeColor),
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
                SelectIndexChanged?.Invoke(this, null);
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
                Invalidate(); // 触发重绘
                HoverIndexChanged?.Invoke(this, null);
            }
        }

        private int CalculateIndex(MouseEventArgs e) => (e.Y - TopSpace) / ItemHeight;
        private bool IsValidIndex(int index) => 
            index >= 0 && index < itemNames.Length && !string.IsNullOrEmpty(itemNames[index]);

        private float Lerp(float a, float b, float t) => a + (b - a) * t;
        private float EaseOutQuad(float x) => x < 0.5 ? 2 * x * x : 1 - (float)Math.Pow(-2 * x + 2, 2) / 2;
    }
}