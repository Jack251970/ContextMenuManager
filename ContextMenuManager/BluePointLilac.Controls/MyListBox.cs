using BluePointLilac.Methods;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public class MyListBox : Panel
    {
        public MyListBox()
        {
            AutoScroll = true;
            UpdateColors();
            DarkModeHelper.ThemeChanged += OnThemeChanged;
        }

        protected override void OnMouseWheel(MouseEventArgs e) =>
            base.OnMouseWheel(new MouseEventArgs(e.Button, e.Clicks, e.X, e.Y, Math.Sign(e.Delta) * 50.DpiZoom()));

        private void OnThemeChanged(object sender, EventArgs e)
        {
            if (IsHandleCreated && !IsDisposed)
            {
                UpdateColors();
                Invalidate(true);
            }
        }

        public void UpdateColors()
        {
            BackColor = DarkModeHelper.FormBack;
            ForeColor = DarkModeHelper.FormFore;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) DarkModeHelper.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }

    public class MyList : FlowLayoutPanel
    {
        private MyListItem hoveredItem;
        private static readonly FontStyle RegularStyle = FontStyle.Regular;
        private static readonly FontStyle BoldStyle = FontStyle.Bold;
        private readonly Dictionary<MyListItem, EventHandler> _resizeHandlers = new();

        public MyListBox Owner
        {
            get => (MyListBox)Parent;
            set => Parent = value;
        }

        public MyList(MyListBox owner) : this() => Owner = owner;

        public MyList()
        {
            AutoSize = true;
            WrapContents = true;
            Dock = DockStyle.Top;
            DoubleBuffered = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            DarkModeHelper.ThemeChanged += OnThemeChanged;
        }

        public MyListItem HoveredItem
        {
            get => hoveredItem;
            set
            {
                if (hoveredItem == value) return;
                if (hoveredItem != null)
                {
                    hoveredItem.ForeColor = DarkModeHelper.FormFore;
                    if (hoveredItem.Font.Style != RegularStyle)
                        hoveredItem.Font = new Font(hoveredItem.Font.FontFamily, hoveredItem.Font.Size, RegularStyle);
                }
                hoveredItem = value;
                if (hoveredItem != null)
                {
                    value.ForeColor = DarkModeHelper.MainColor;
                    if (value.Font.Style != BoldStyle)
                        value.Font = new Font(value.Font.FontFamily, value.Font.Size, BoldStyle);
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
            item.MouseEnter += OnItemMouseEnter;
            MouseWheel += OnItemMouseWheel;

            EventHandler handler = (s, e) => item.Width = Owner.Width - item.Margin.Horizontal;
            _resizeHandlers[item] = handler;
            Owner.Resize += handler;
            item.Width = Owner.Width - item.Margin.Horizontal;
            ResumeLayout();
        }

        private void OnItemMouseEnter(object sender, EventArgs e)
        {
            if (sender is MyListItem item) HoveredItem = item;
        }

        private void OnItemMouseWheel(object sender, MouseEventArgs e)
        {
            if (HoveredItem?.ContextMenuStrip != null) HoveredItem.ContextMenuStrip.Close();
        }

        public void AddItems(MyListItem[] items) => AddItemsCore(items);
        public void AddItems(List<MyListItem> items) => AddItemsCore(items);

        private void AddItemsCore(IEnumerable<MyListItem> items)
        {
            Owner?.SuspendLayout();
            SuspendLayout();
            try { foreach (var item in items) AddItem(item); }
            finally { ResumeLayout(); Owner?.ResumeLayout(); }
        }

        public void SetItemIndex(MyListItem item, int newIndex) => Controls.SetChildIndex(item, newIndex);
        public int GetItemIndex(MyListItem item) => Controls.GetChildIndex(item);

        public void RemoveItem(MyListItem item)
        {
            if (item == null || !Controls.Contains(item)) return;
            SuspendLayout();
            try
            {
                item.MouseEnter -= OnItemMouseEnter;
                if (_resizeHandlers.TryGetValue(item, out var handler))
                {
                    Owner.Resize -= handler;
                    _resizeHandlers.Remove(item);
                }
                if (hoveredItem == item) hoveredItem = null;
                Controls.Remove(item);
                item.Dispose();
            }
            finally { ResumeLayout(); }
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
            Owner?.SuspendLayout();
            SuspendLayout();
            try
            {
                foreach (MyListItem item in Controls)
                {
                    item.MouseEnter -= OnItemMouseEnter;
                    if (_resizeHandlers.TryGetValue(item, out var handler))
                    {
                        Owner.Resize -= handler;
                        _resizeHandlers.Remove(item);
                    }
                }
                while (Controls.Count > 0)
                {
                    var ctr = Controls[0];
                    Controls.RemoveAt(0);
                    ctr.Dispose();
                }
            }
            finally { ResumeLayout(); Owner?.ResumeLayout(); }
        }

        public void SortItemByText()
        {
            var items = new List<MyListItem>();
            foreach (MyListItem item in Controls) items.Add(item);
            Controls.Clear();
            items.Sort((x, y) => string.Compare(x.Text, y.Text, StringComparison.CurrentCulture));
            items.ForEach(AddItem);
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            if (IsHandleCreated && !IsDisposed && hoveredItem?.IsHandleCreated == true && !hoveredItem.IsDisposed)
                hoveredItem.ForeColor = DarkModeHelper.MainColor;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DarkModeHelper.ThemeChanged -= OnThemeChanged;
                foreach (var kvp in _resizeHandlers) Owner.Resize -= kvp.Value;
                _resizeHandlers.Clear();
                MouseWheel -= OnItemMouseWheel;
            }
            base.Dispose(disposing);
        }
    }

    public class MyListItem : Panel
    {
        private string subText;
        private bool hasImage = true;

        private readonly Label lblText = new() { AutoSize = true, Left = 60.DpiZoom(), Name = "Text" };
        private readonly PictureBox picImage = new()
        {
            SizeMode = PictureBoxSizeMode.AutoSize,
            Left = 20.DpiZoom(),
            Enabled = false,
            Name = "Image"
        };
        private readonly FlowLayoutPanel flpControls = new()
        {
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.RightToLeft,
            Anchor = AnchorStyles.Right,
            AutoSize = true,
            Name = "Controls"
        };
        private readonly Label lblSeparator = new()
        {
            BackColor = DarkModeHelper.FormFore,
            Dock = DockStyle.Bottom,
            Name = "Separator",
            Height = 1
        };
        private readonly Panel pnlScrollbar = new()
        {
            Width = SystemInformation.VerticalScrollBarWidth,
            Enabled = false
        };

        public MyListItem()
        {
            SuspendLayout();
            DoubleBuffered = true;
            Height = 50.DpiZoom();
            Margin = new Padding(0);
            Font = SystemFonts.IconTitleFont;
            UpdateColors();

            Controls.AddRange(new Control[] { lblSeparator, flpControls, picImage, lblText });
            Resize += (s, e) => pnlScrollbar.Height = ClientSize.Height;
            flpControls.MouseClick += (s, e) => OnMouseClick(e);
            flpControls.MouseEnter += (s, e) => OnMouseEnter(e);
            flpControls.MouseDown += (s, e) => OnMouseDown(e);
            lblSeparator.SetEnabled(false);
            lblText.SetEnabled(false);
            CenterControl(lblText);
            CenterControl(picImage);
            AddCtr(pnlScrollbar, 0);
            DarkModeHelper.ThemeChanged += OnThemeChanged;
            ResumeLayout();
        }

        public Image Image { get => picImage.Image; set => picImage.Image = value; }
        public new string Text { get => lblText.Text; set => lblText.Text = value; }
        public new Font Font { get => lblText.Font; set => lblText.Font = value; }
        public new Color ForeColor { get => lblText.ForeColor; set => lblText.ForeColor = value; }
        public string SubText { get => subText; set => subText = value; }

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

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            OnMouseEnter(null);
        }

        private void CenterControl(Control ctr)
        {
            void Resize()
            {
                if (ctr.Parent == null) return;
                var top = (ClientSize.Height - ctr.Height) / 2;
                ctr.Top = top;
                if (ctr.Parent == flpControls)
                    ctr.Margin = new Padding(0, top, ctr.Margin.Right, top);
            }
            ctr.Parent.Resize += (s, e) => Resize();
            ctr.Resize += (s, e) => Resize();
            Resize();
        }

        public void AddCtr(Control ctr) => AddCtr(ctr, 20.DpiZoom());

        public void AddCtr(Control ctr, int space)
        {
            SuspendLayout();
            ctr.Parent = flpControls;
            ctr.Margin = new Padding(0, 0, space, 0);
            ctr.MouseEnter += (s, e) => OnMouseEnter(e);
            ctr.MouseDown += (s, e) => OnMouseEnter(e);
            CenterControl(ctr);
            ResumeLayout();
        }

        public void AddCtrs(Control[] ctrs) => Array.ForEach(ctrs, AddCtr);
        public void RemoveCtrAt(int index) { if (flpControls.Controls.Count > index) flpControls.Controls.RemoveAt(index + 1); }
        public int GetCtrIndex(Control ctr) => flpControls.Controls.GetChildIndex(ctr, true) - 1;
        public void SetCtrIndex(Control ctr, int newIndex) => flpControls.Controls.SetChildIndex(ctr, newIndex + 1);

        private void OnThemeChanged(object sender, EventArgs e)
        {
            if (IsHandleCreated && !IsDisposed)
            {
                UpdateColors();
                Invalidate(true);
            }
        }

        public void UpdateColors()
        {
            BackColor = DarkModeHelper.FormBack;
            ForeColor = DarkModeHelper.FormFore;
            lblSeparator.BackColor = DarkModeHelper.FormFore;
            lblText.ForeColor = DarkModeHelper.FormFore;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) DarkModeHelper.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
