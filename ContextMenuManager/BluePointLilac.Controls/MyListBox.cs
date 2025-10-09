using BluePointLilac.Methods;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public class MyListBox : Panel
    {
        public MyListBox()
        {
            AutoScroll = true;
            BackColor = MyMainForm.FormBack;
            ForeColor = MyMainForm.FormFore;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            //使滚动幅度与MyListItem的高度相配合，防止滚动过快导致来不及重绘界面变花
            base.OnMouseWheel(new MouseEventArgs(e.Button, e.Clicks, e.X, e.Y, Math.Sign(e.Delta) * 50.DpiZoom()));
        }

        // 搜索功能
        public virtual void SearchItems(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                // 清空搜索，显示所有项
                foreach (Control control in Controls)
                {
                    if (control is MyList list)
                    {
                        foreach (Control item in list.Controls)
                        {
                            if (item is MyListItem listItem)
                            {
                                listItem.Visible = true;
                                listItem.HighlightText = null;
                            }
                        }
                    }
                }
                return;
            }

            // 搜索所有列表项
            foreach (Control control in Controls)
            {
                if (control is MyList list)
                {
                    foreach (Control item in list.Controls)
                    {
                        if (item is MyListItem listItem)
                        {
                            bool matches = listItem.Text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                            listItem.Visible = matches;
                            if (matches)
                            {
                                listItem.HighlightText = searchText;
                            }
                            else
                            {
                                listItem.HighlightText = null;
                            }
                        }
                    }
                }
            }
        }

        // 获取所有可见的列表项
        public virtual IEnumerable<MyListItem> GetAllItems()
        {
            foreach (Control control in Controls)
            {
                if (control is MyList list)
                {
                    foreach (Control item in list.Controls)
                    {
                        if (item is MyListItem listItem)
                        {
                            yield return listItem;
                        }
                    }
                }
            }
        }

        // 清除搜索高亮
        public virtual void ClearSearchHighlight()
        {
            foreach (var item in GetAllItems())
            {
                item.HighlightText = null;
            }
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
            Dock = DockStyle.Top;
            DoubleBuffered = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }

        // 移除悬停项相关代码
        // private MyListItem hoveredItem;
        // public MyListItem HoveredItem { ... }
        // public event EventHandler HoveredItemChanged;

        public void AddItem(MyListItem item)
        {
            SuspendLayout();
            item.Parent = this;

            // 移除悬停事件
            // item.MouseEnter += (sender, e) => HoveredItem = item;

            MouseWheel += (sender, e) => item.ContextMenuStrip?.Close();
            void ResizeItem() => item.Width = Owner.Width - item.Margin.Horizontal;
            Owner.Resize += (sender, e) => ResizeItem();
            ResizeItem();
            ResumeLayout();
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

        // 搜索功能
        public virtual void SearchItems(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                // 清空搜索，显示所有项
                foreach (Control control in Controls)
                {
                    if (control is MyListItem item)
                    {
                        item.Visible = true;
                        item.HighlightText = null;
                    }
                }
                return;
            }

            // 搜索所有列表项
            foreach (Control control in Controls)
            {
                if (control is MyListItem item)
                {
                    bool matches = item.Text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                    item.Visible = matches;
                    if (matches)
                    {
                        item.HighlightText = searchText;
                    }
                    else
                    {
                        item.HighlightText = null;
                    }
                }
            }
        }

        // 获取所有列表项
        public virtual IEnumerable<MyListItem> GetAllItems()
        {
            foreach (Control control in Controls)
            {
                if (control is MyListItem item)
                {
                    yield return item;
                }
            }
        }
    }

    public class MyListItem : Panel
    {
        private string highlightText;

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
            Controls.AddRange(new Control[] { lblSeparator, flpControls, lblText, picImage });
            Resize += (Sender, e) => pnlScrollbar.Height = ClientSize.Height;
            flpControls.MouseClick += (sender, e) => OnMouseClick(e);

            // 移除悬停事件
            // flpControls.MouseEnter += (sender, e) => OnMouseEnter(e);

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

        // 高亮文本属性
        public string HighlightText
        {
            get => highlightText;
            set
            {
                if (highlightText != value)
                {
                    highlightText = value;
                    Invalidate(); // 重绘以显示高亮
                }
            }
        }

        // 自动适配的高亮颜色
        public Color HighlightColor
        {
            get
            {
                // 根据背景色亮度自动选择合适的高亮颜色
                bool isDarkMode = IsDarkColor(BackColor);
                return isDarkMode ?
                    Color.FromArgb(255, 100, 70, 0) : // 暗色模式：暗橙色
                    Color.FromArgb(255, 255, 220, 100); // 浅色模式：浅黄色
            }
        }

        // 判断颜色是否为暗色
        private bool IsDarkColor(Color color)
        {
            // 计算颜色的相对亮度 (ITU-R BT.709 标准)
            double luminance = (0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B) / 255;
            return luminance < 0.5;
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
            Name = "Text",
            BackColor = Color.Transparent
        };
        private readonly PictureBox picImage = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.AutoSize,
            Left = 20.DpiZoom(),
            Enabled = false,
            Name = "Image"
        };
        private readonly FlowLayoutPanel flpControls = new FlowLayoutPanel
        {
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.RightToLeft,
            Anchor = AnchorStyles.Right,
            AutoSize = true,
            Name = "Controls"
        };
        private readonly Label lblSeparator = new Label
        {
            BackColor = MyMainForm.FormFore,
            Dock = DockStyle.Bottom,
            Name = "Separator",
            Height = 1
        };//分割线
        private readonly Panel pnlScrollbar = new Panel
        {
            Width = SystemInformation.VerticalScrollBarWidth,
            Enabled = false
        };//预留滚动条宽度

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            // 移除悬停效果
            // OnMouseEnter(null);
        }

        // 移除悬停效果
        // protected override void OnMouseEnter(EventArgs e)
        // {
        //     base.OnMouseEnter(e);
        //     // 移除悬停时的颜色和字体变化
        // }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // 如果有高亮文本，绘制高亮背景
            if (!string.IsNullOrEmpty(highlightText) && !string.IsNullOrEmpty(Text))
            {
                var text = Text;
                var searchText = highlightText;
                var index = text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);

                if (index >= 0)
                {
                    // 计算文本位置
                    var textSize = TextRenderer.MeasureText(text, Font);
                    var charWidth = textSize.Width / text.Length;
                    var highlightStart = index * charWidth;
                    var highlightWidth = searchText.Length * charWidth;

                    // 确保高亮区域在标签范围内
                    var highlightRect = new Rectangle(
                        lblText.Left + highlightStart,
                        lblText.Top,
                        Math.Min(highlightWidth, lblText.Width - highlightStart),
                        lblText.Height
                    );

                    using (var brush = new SolidBrush(HighlightColor))
                    {
                        e.Graphics.FillRectangle(brush, highlightRect);
                    }
                }
            }
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

            // 移除悬停事件
            // ctr.MouseEnter += (sender, e) => OnMouseEnter(e);

            ctr.MouseDown += (sender, e) => OnMouseDown(e);
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
    }
}