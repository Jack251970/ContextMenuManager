using BluePointLilac.Methods;
using ContextMenuManager.Methods;  // 添加AppString类的命名空间
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public class MyListBox : Panel
    {
        private MyList mainList;

        public MyListBox()
        {
            AutoScroll = true;
            BackColor = MyMainForm.FormBack;
            ForeColor = MyMainForm.FormFore;
            
            // 初始化主列表
            mainList = new MyList(this);
            mainList.Dock = DockStyle.Fill;
            Controls.Add(mainList);
        }

        // 添加项目到列表
        public void AddItem(MyListItem item)
        {
            mainList.AddItem(item);
        }

        public void AddItems(MyListItem[] items)
        {
            mainList.AddItems(items);
        }

        public void AddItems(List<MyListItem> items)
        {
            mainList.AddItems(items);
        }

        public void ClearItems()
        {
            mainList.ClearItems();
        }

        // 其他方法保持不变，但需要代理到 mainList
        public MyListItem SelectedItem
        {
            get => mainList.HoveredItem;
            set => mainList.HoveredItem = value;
        }

        public event EventHandler SelectedItemChanged
        {
            add => mainList.HoveredItemChanged += value;
            remove => mainList.HoveredItemChanged -= value;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            //使滚动幅度与MyListItem的高度相配合，防止滚动过快导致来不及重绘界面变花
            base.OnMouseWheel(new MouseEventArgs(e.Button, e.Clicks, e.X, e.Y, Math.Sign(e.Delta) * 50.DpiZoom()));
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

        // 添加一个私有字段来保存搜索框实例
        private SearchBox searchBox;
        
        // 获取或设置搜索框的方法，不再创建搜索框，只用于过滤
        public SearchBox SearchBox 
        {
            get => searchBox;
            set
            {
                if (searchBox != null)
                {
                    // 移除旧事件处理程序
                    searchBox.TextChanged -= SearchBox_TextChanged;
                }
                
                searchBox = value;
                
                if (searchBox != null)
                {
                    // 添加新事件处理程序
                    searchBox.TextChanged += SearchBox_TextChanged;
                }
            }
        }
        
        // 搜索框文本改变事件处理
        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            string searchText = searchBox?.Text.ToLower().Trim() ?? "";
            FilterListItems(searchText);
        }
        
        // 实现列表过滤功能
        private void FilterListItems(string searchText)
        {
            // 遍历所有控件，根据搜索文本显示或隐藏它们
            foreach (Control control in Controls)
            {
                if (control is MyListItem item)
                {
                    if (string.IsNullOrEmpty(searchText) || item.Text.ToLower().Contains(searchText))
                    {
                        item.Visible = true;
                    }
                    else
                    {
                        item.Visible = false;
                    }
                }
            }
        }

        private MyListItem hoveredItem;
        public MyListItem HoveredItem
        {
            get => hoveredItem;
            set
            {
                if(hoveredItem == value) return;
                if(hoveredItem != null)
                {
                    hoveredItem.ForeColor = MyMainForm.FormFore;
                    hoveredItem.Font = new Font(hoveredItem.Font, FontStyle.Regular);
                }
                hoveredItem = value;
                if (hoveredItem != null)
                {
                    value.ForeColor = MyMainForm.MainColor;
                    value.Font = new Font(hoveredItem.Font, FontStyle.Bold);
                    value.Focus();
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
            if(item == null) return;
            AddItem(item);
            SetItemIndex(item, index);
        }

        public virtual void ClearItems()
        {
            if(Controls.Count == 0) return;
            SuspendLayout();
            for(int i = Controls.Count - 1; i >= 0; i--)
            {
                Control ctr = Controls[i];
                Controls.Remove(ctr);
                ctr.Dispose();
            }
            ResumeLayout();
            
            // 同时清空保存的原始列表
            if (Tag is List<MyListItem> savedItems)
            {
                savedItems.Clear();
            }
        }

        public void SortItemByText()
        {
            List<MyListItem> items = new List<MyListItem>();
            foreach(MyListItem item in Controls) items.Add(item);
            Controls.Clear();
            items.Sort(new TextComparer());
            items.ForEach(item => AddItem(item));
        }

        public class TextComparer : IComparer<MyListItem>
        {
            public int Compare(MyListItem x, MyListItem y)
            {
                if(x.Equals(y)) return 0;
                string[] strs = { x.Text, y.Text };
                Array.Sort(strs);
                if(strs[0] == x.Text) return -1;
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
            base.OnMouseDown(e); OnMouseEnter(null);
        }

        private void CenterControl(Control ctr)
        {
            void reSize()
            {
                if(ctr.Parent == null) return;
                int top = (ClientSize.Height - ctr.Height) / 2;
                ctr.Top = top;
                if(ctr.Parent == flpControls)
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
            if(flpControls.Controls.Count > index) flpControls.Controls.RemoveAt(index + 1);
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