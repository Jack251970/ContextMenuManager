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
        private string displayText;
        private readonly RichTextBox rtbText;

        public MyListItem()
        {
            SuspendLayout();
            DoubleBuffered = true;
            Height = 50.DpiZoom();
            Margin = new Padding(0);
            BackColor = MyMainForm.FormBack;

            // 使用RichTextBox
            rtbText = new RichTextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = MyMainForm.FormBack,
                ForeColor = MyMainForm.FormFore,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.None,
                Multiline = false,
                DetectUrls = false,
                Name = "Text",
                TabStop = false // 禁用Tab键焦点
            };

            // 添加控件到面板
            Controls.AddRange(new Control[] { lblSeparator, flpControls, rtbText, picImage });

            // 设置初始位置
            picImage.Left = 20.DpiZoom();
            rtbText.Left = 60.DpiZoom(); // 默认有图片的位置

            // 设置RichTextBox的尺寸和位置以实现垂直居中
            UpdateRichTextBoxPosition();

            Resize += (Sender, e) =>
            {
                pnlScrollbar.Height = ClientSize.Height;
                UpdateRichTextBoxPosition();
            };

            // 禁用RichTextBox的鼠标和键盘事件，防止文本选中
            rtbText.MouseDown += (sender, e) =>
            {
                // 将事件传递给父控件
                OnMouseDown(e);
            };

            rtbText.MouseMove += (sender, e) =>
            {
                OnMouseMove(e);
            };

            rtbText.MouseUp += (sender, e) =>
            {
                OnMouseUp(e);
            };

            rtbText.KeyDown += (sender, e) =>
            {
                // 阻止键盘事件
                e.Handled = true;
            };

            rtbText.KeyPress += (sender, e) =>
            {
                // 阻止键盘事件
                e.Handled = true;
            };

            flpControls.MouseClick += (sender, e) => OnMouseClick(e);
            flpControls.MouseDown += (sender, e) => OnMouseDown(e);

            lblSeparator.SetEnabled(false);

            CenterControl(picImage);
            AddCtr(pnlScrollbar, 0);

            // 最后设置HasImage，确保所有控件已初始化
            hasImage = true;
            picImage.Visible = true;

            ResumeLayout();
        }

        // 更新RichTextBox的位置以实现垂直居中
        private void UpdateRichTextBoxPosition()
        {
            if (rtbText == null) return;

            // 计算文本高度
            int textHeight = TextRenderer.MeasureText("A", rtbText.Font).Height;

            // 计算垂直居中位置
            int top = (Height - textHeight) / 2;

            // 设置RichTextBox的位置和大小
            rtbText.Top = top;
            rtbText.Height = textHeight;
            rtbText.Width = Width - rtbText.Left - flpControls.Width - 10.DpiZoom();
        }

        public Image Image
        {
            get => picImage.Image;
            set => picImage.Image = value;
        }
        public new string Text
        {
            get => displayText ?? string.Empty;
            set
            {
                displayText = value;
                if (rtbText != null)
                {
                    rtbText.Text = value;
                    UpdateHighlight();
                    UpdateRichTextBoxPosition(); // 文本改变时更新位置
                }
            }
        }
        public new Font Font
        {
            get => rtbText?.Font ?? SystemFonts.IconTitleFont;
            set
            {
                if (rtbText != null)
                {
                    rtbText.Font = value;
                    UpdateHighlight();
                    UpdateRichTextBoxPosition(); // 字体改变时更新位置
                }
            }
        }
        public new Color ForeColor
        {
            get => rtbText?.ForeColor ?? MyMainForm.FormFore;
            set
            {
                if (rtbText != null)
                {
                    rtbText.ForeColor = value;
                    UpdateHighlight();
                }
            }
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
                    UpdateHighlight();
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
                if (picImage != null)
                    picImage.Visible = value;
                if (rtbText != null)
                {
                    rtbText.Left = (value ? 60 : 20).DpiZoom();
                    UpdateRichTextBoxPosition(); // 位置改变时更新位置
                }
            }
        }

        // 更新高亮显示
        private void UpdateHighlight()
        {
            if (rtbText == null || string.IsNullOrEmpty(Text)) return;

            // 保存当前选择位置
            int originalSelectionStart = rtbText.SelectionStart;
            int originalSelectionLength = rtbText.SelectionLength;

            // 清除所有格式
            rtbText.SelectAll();
            rtbText.SelectionBackColor = rtbText.BackColor;
            rtbText.DeselectAll();

            // 如果有高亮文本，应用高亮
            if (!string.IsNullOrEmpty(highlightText))
            {
                var text = Text;
                var searchText = highlightText;
                var index = text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);

                if (index >= 0)
                {
                    rtbText.Select(index, searchText.Length);
                    rtbText.SelectionBackColor = HighlightColor;
                    rtbText.DeselectAll();
                }
            }

            // 恢复原始选择位置
            rtbText.Select(originalSelectionStart, originalSelectionLength);

            // 取消文本选中状态
            rtbText.SelectionStart = 0;
            rtbText.SelectionLength = 0;
        }

        private readonly PictureBox picImage = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.AutoSize,
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

            // 确保RichTextBox没有焦点
            if (rtbText != null && rtbText.Focused)
            {
                // 将焦点转移到父控件
                this.Focus();
            }
        }

        // 移除悬停效果
        // protected override void OnMouseEnter(EventArgs e)
        // {
        //     base.OnMouseEnter(e);
        //     // 移除悬停时的颜色和字体变化
        // }

        private void CenterControl(Control ctr)
        {
            if (ctr == null) return;

            void reSize()
            {
                if (ctr.Parent == null || ctr.IsDisposed) return;
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
            if (ctr == null || flpControls == null) return;

            SuspendLayout();
            ctr.Parent = flpControls;
            ctr.Margin = new Padding(0, 0, space, 0);

            ctr.MouseDown += (sender, e) => OnMouseDown(e);
            CenterControl(ctr);
            ResumeLayout();
        }

        public void AddCtrs(Control[] ctrs)
        {
            if (ctrs == null) return;
            Array.ForEach(ctrs, ctr => AddCtr(ctr));
        }

        public void RemoveCtrAt(int index)
        {
            if (flpControls?.Controls != null && flpControls.Controls.Count > index)
                flpControls.Controls.RemoveAt(index + 1);
        }

        public int GetCtrIndex(Control ctr)
        {
            return flpControls?.Controls?.GetChildIndex(ctr, true) - 1 ?? -1;
        }

        public void SetCtrIndex(Control ctr, int newIndex)
        {
            flpControls?.Controls?.SetChildIndex(ctr, newIndex + 1);
        }
    }
}