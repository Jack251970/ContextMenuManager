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
                            if (item is MyListItem listItem && !listItem.IsDisposed)
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
                        if (item is MyListItem listItem && !listItem.IsDisposed)
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
                        if (item is MyListItem listItem && !listItem.IsDisposed)
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
                if (!item.IsDisposed)
                {
                    item.HighlightText = null;
                }
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

        // 恢复悬停项相关代码
        private MyListItem hoveredItem;
        public MyListItem HoveredItem
        {
            get => hoveredItem;
            set
            {
                if (hoveredItem == value) return;

                // 移除旧悬停效果 - 检查是否已释放
                if (hoveredItem != null && !hoveredItem.IsDisposed)
                {
                    hoveredItem.ForeColor = MyMainForm.FormFore;
                    hoveredItem.Font = new Font(hoveredItem.Font, FontStyle.Regular);
                }

                hoveredItem = value;

                // 应用新悬停效果 - 检查是否已释放
                if (hoveredItem != null && !hoveredItem.IsDisposed)
                {
                    hoveredItem.ForeColor = MyMainForm.MainColor;
                    hoveredItem.Font = new Font(hoveredItem.Font, FontStyle.Bold);
                    hoveredItem.Focus();
                }

                HoveredItemChanged?.Invoke(this, null);
            }
        }

        public event EventHandler HoveredItemChanged;

        public void AddItem(MyListItem item)
        {
            if (item == null || item.IsDisposed) return;

            SuspendLayout();
            item.Parent = this;

            // 恢复悬停事件 - 添加释放检查
            item.MouseEnter += (sender, e) =>
            {
                if (!item.IsDisposed)
                    HoveredItem = item;
            };

            // 添加鼠标离开事件，清除悬停
            item.MouseLeave += (sender, e) =>
            {
                if (HoveredItem == item && !item.IsDisposed)
                    HoveredItem = null;
            };

            MouseWheel += (sender, e) =>
            {
                if (!item.IsDisposed)
                    item.ContextMenuStrip?.Close();
            };

            void ResizeItem()
            {
                if (!item.IsDisposed)
                    item.Width = Owner.Width - item.Margin.Horizontal;
            }

            Owner.Resize += (sender, e) => ResizeItem();
            ResizeItem();
            ResumeLayout();
        }

        public void AddItems(MyListItem[] items)
        {
            if (items == null) return;
            Array.ForEach(items, item =>
            {
                if (!item.IsDisposed)
                    AddItem(item);
            });
        }

        public void AddItems(List<MyListItem> items)
        {
            if (items == null) return;
            items.ForEach(item =>
            {
                if (!item.IsDisposed)
                    AddItem(item);
            });
        }

        public void SetItemIndex(MyListItem item, int newIndex)
        {
            if (item == null || item.IsDisposed) return;
            Controls.SetChildIndex(item, newIndex);
        }

        public int GetItemIndex(MyListItem item)
        {
            if (item == null || item.IsDisposed) return -1;
            return Controls.GetChildIndex(item);
        }

        public void InsertItem(MyListItem item, int index)
        {
            if (item == null || item.IsDisposed) return;
            AddItem(item);
            SetItemIndex(item, index);
        }

        public virtual void ClearItems()
        {
            if (Controls.Count == 0) return;

            // 清除悬停项引用
            HoveredItem = null;

            SuspendLayout();
            for (int i = Controls.Count - 1; i >= 0; i--)
            {
                Control ctr = Controls[i];
                Controls.Remove(ctr);
                if (!ctr.IsDisposed)
                    ctr.Dispose();
            }
            ResumeLayout();
        }

        public void SortItemByText()
        {
            List<MyListItem> items = new List<MyListItem>();
            foreach (MyListItem item in Controls)
            {
                if (!item.IsDisposed)
                    items.Add(item);
            }
            Controls.Clear();
            items.Sort(new TextComparer());
            items.ForEach(item =>
            {
                if (!item.IsDisposed)
                    AddItem(item);
            });
        }

        public class TextComparer : IComparer<MyListItem>
        {
            public int Compare(MyListItem x, MyListItem y)
            {
                if (x == null || y == null) return 0;
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
                    if (control is MyListItem item && !item.IsDisposed)
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
                if (control is MyListItem item && !item.IsDisposed)
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
                if (control is MyListItem item && !item.IsDisposed)
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
                TabStop = false, // 禁用Tab键焦点
                HideSelection = true // 隐藏选择
            };

            // 添加控件到面板
            Controls.AddRange(new Control[] { lblSeparator, flpControls, rtbText, picImage });

            // 设置初始位置
            picImage.Left = 20.DpiZoom();
            rtbText.Left = 60.DpiZoom(); // 默认有图片的位置

            // 设置RichTextBox的位置和大小
            UpdateRichTextBoxPosition();

            Resize += (Sender, e) =>
            {
                if (!IsDisposed)
                {
                    pnlScrollbar.Height = ClientSize.Height;
                    UpdateRichTextBoxPosition();
                }
            };

            // 禁用RichTextBox的鼠标和键盘事件
            rtbText.MouseDown += (sender, e) =>
            {
                if (!IsDisposed)
                {
                    OnMouseDown(e);
                    // 防止RichTextBox获得焦点
                    ((RichTextBox)sender).Parent.Focus();
                }
            };

            rtbText.MouseMove += (sender, e) => { if (!IsDisposed) OnMouseMove(e); };
            rtbText.MouseUp += (sender, e) => { if (!IsDisposed) OnMouseUp(e); };
            rtbText.MouseClick += (sender, e) => { if (!IsDisposed) OnMouseClick(e); };
            rtbText.MouseDoubleClick += (sender, e) => { if (!IsDisposed) OnDoubleClick(e); };

            rtbText.KeyDown += (sender, e) =>
            {
                if (!IsDisposed)
                {
                    OnKeyDown(e);
                    e.Handled = true; // 阻止键盘事件
                }
            };

            rtbText.KeyPress += (sender, e) =>
            {
                if (!IsDisposed)
                {
                    OnKeyPress(e);
                    e.Handled = true; // 阻止键盘事件
                }
            };

            rtbText.KeyUp += (sender, e) =>
            {
                if (!IsDisposed)
                {
                    OnKeyUp(e);
                    e.Handled = true; // 阻止键盘事件
                }
            };

            flpControls.MouseClick += (sender, e) => { if (!IsDisposed) OnMouseClick(e); };
            flpControls.MouseDown += (sender, e) => { if (!IsDisposed) OnMouseDown(e); };

            // 为子控件添加悬停事件
            foreach (Control control in Controls)
            {
                control.MouseEnter += (sender, e) => { if (!IsDisposed) OnMouseEnter(e); };
                control.MouseLeave += (sender, e) => { if (!IsDisposed) OnMouseLeave(e); };
            }

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
            if (rtbText == null || rtbText.IsDisposed || IsDisposed) return;

            // 计算文本高度
            int textHeight = TextRenderer.MeasureText("A", rtbText.Font).Height;

            // 计算垂直居中位置
            int top = (Height - textHeight) / 2;

            // 设置RichTextBox的位置和大小
            rtbText.Top = top;
            rtbText.Height = textHeight + 2; // 添加一点额外高度确保文本完全显示
            rtbText.Width = Width - rtbText.Left - flpControls.Width - 10.DpiZoom();
        }

        public Image Image
        {
            get => picImage?.Image;
            set
            {
                if (picImage != null && !picImage.IsDisposed && !IsDisposed)
                    picImage.Image = value;
            }
        }

        public new string Text
        {
            get => displayText ?? string.Empty;
            set
            {
                displayText = value;
                if (rtbText != null && !rtbText.IsDisposed && !IsDisposed)
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
                if (rtbText != null && !rtbText.IsDisposed && !IsDisposed)
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
                if (rtbText != null && !rtbText.IsDisposed && !IsDisposed)
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
                if (highlightText != value && !IsDisposed)
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
                if (IsDisposed) return Color.Yellow;

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
                if (picImage != null && !picImage.IsDisposed && !IsDisposed)
                    picImage.Visible = value;
                if (rtbText != null && !rtbText.IsDisposed && !IsDisposed)
                {
                    rtbText.Left = (value ? 60 : 20).DpiZoom();
                    UpdateRichTextBoxPosition(); // 位置改变时更新位置
                }
            }
        }

        // 更新高亮显示
        private void UpdateHighlight()
        {
            if (rtbText == null || rtbText.IsDisposed || IsDisposed || string.IsNullOrEmpty(Text)) return;

            try
            {
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

                // 确保没有文本被选中
                rtbText.SelectionStart = 0;
                rtbText.SelectionLength = 0;
            }
            catch (ObjectDisposedException)
            {
                // 忽略已释放对象的异常
                return;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (IsDisposed) return;

            base.OnMouseDown(e);
            OnMouseEnter(e); // 鼠标按下时也触发悬停效果

            // 确保RichTextBox没有焦点
            if (rtbText != null && !rtbText.IsDisposed && rtbText.Focused)
            {
                // 将焦点转移到父控件
                this.Focus();
            }
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

        private void CenterControl(Control ctr)
        {
            if (ctr == null || IsDisposed) return;

            void reSize()
            {
                if (ctr.Parent == null || ctr.IsDisposed || IsDisposed) return;
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
            if (ctr == null || flpControls == null || IsDisposed) return;

            SuspendLayout();
            ctr.Parent = flpControls;
            ctr.Margin = new Padding(0, 0, space, 0);

            ctr.MouseDown += (sender, e) => { if (!IsDisposed) OnMouseDown(e); };
            ctr.MouseEnter += (sender, e) => { if (!IsDisposed) OnMouseEnter(e); };
            ctr.MouseLeave += (sender, e) => { if (!IsDisposed) OnMouseLeave(e); };
            CenterControl(ctr);
            ResumeLayout();
        }

        public void AddCtrs(Control[] ctrs)
        {
            if (ctrs == null) return;
            Array.ForEach(ctrs, ctr =>
            {
                if (!ctr.IsDisposed)
                    AddCtr(ctr);
            });
        }

        public void RemoveCtrAt(int index)
        {
            if (flpControls?.Controls != null && flpControls.Controls.Count > index && !IsDisposed)
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