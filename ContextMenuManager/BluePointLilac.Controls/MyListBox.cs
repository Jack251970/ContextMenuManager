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
            BackColor = MyMainForm.FormBack;
            ForeColor = MyMainForm.FormFore;
            DoubleBuffered = true;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(new MouseEventArgs(e.Button, e.Clicks, e.X, e.Y,
                Math.Sign(e.Delta) * 50.DpiZoom()));
        }

        public virtual void SearchItems(string searchText)
        {
            SuspendLayout();
            foreach (Control control in Controls)
            {
                if (control is MyList list && !list.IsDisposed)
                {
                    list.SearchItems(searchText);
                }
            }
            ResumeLayout();
        }

        public virtual IEnumerable<MyListItem> GetAllItems()
        {
            foreach (Control control in Controls)
            {
                if (control is MyList list && !list.IsDisposed)
                {
                    foreach (var item in list.GetAllItems()) yield return item;
                }
            }
        }

        public virtual void ClearSearchHighlight()
        {
            foreach (var item in GetAllItems())
            {
                if (!item.IsDisposed) item.HighlightText = null;
            }
        }
    }

    public class MyList : FlowLayoutPanel
    {
        public MyListBox Owner { get => (MyListBox)Parent; set => Parent = value; }

        public MyList(MyListBox owner) : this() => Owner = owner;
        public MyList()
        {
            AutoSize = true; WrapContents = true; Dock = DockStyle.Top;
            DoubleBuffered = true; AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }

        private MyListItem hoveredItem;
        public MyListItem HoveredItem
        {
            get => hoveredItem;
            set
            {
                if (hoveredItem == value) return;
                if (hoveredItem != null && !hoveredItem.IsDisposed)
                {
                    hoveredItem.ForeColor = MyMainForm.FormFore;
                    hoveredItem.Font = new Font(hoveredItem.Font, FontStyle.Regular);
                }
                hoveredItem = value;
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

            item.MouseEnter += (s, e) => { if (!item.IsDisposed) HoveredItem = item; };
            item.MouseLeave += (s, e) => { if (HoveredItem == item) HoveredItem = null; };
            MouseWheel += (s, e) => item.ContextMenuStrip?.Close();

            void ResizeItem() => item.Width = Owner.Width - item.Margin.Horizontal;
            Owner.Resize += (s, e) => ResizeItem();
            ResizeItem();
            ResumeLayout();
        }

        public void AddItems(IEnumerable<MyListItem> items)
        {
            if (items == null) return;
            foreach (var item in items) AddItem(item);
        }

        public void SetItemIndex(MyListItem item, int newIndex) => Controls.SetChildIndex(item, newIndex);
        public int GetItemIndex(MyListItem item) => Controls.GetChildIndex(item);
        public void InsertItem(MyListItem item, int index) { AddItem(item); SetItemIndex(item, index); }

        public virtual void ClearItems()
        {
            if (Controls.Count == 0) return;
            HoveredItem = null;
            SuspendLayout();
            for (int i = Controls.Count - 1; i >= 0; i--)
            {
                Control ctr = Controls[i];
                Controls.Remove(ctr);
                if (!ctr.IsDisposed) ctr.Dispose();
            }
            ResumeLayout();
        }

        public void SortItemByText()
        {
            var items = new List<MyListItem>();
            foreach (MyListItem item in Controls) if (!item.IsDisposed) items.Add(item);
            Controls.Clear();
            items.Sort((x, y) => string.Compare(x?.Text, y?.Text, StringComparison.Ordinal));
            foreach (var item in items) AddItem(item);
        }

        public virtual void SearchItems(string searchText)
        {
            SuspendLayout();
            foreach (Control control in Controls)
            {
                if (control is MyListItem item && !item.IsDisposed)
                {
                    bool matches = string.IsNullOrEmpty(searchText) ||
                        item.Text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                    item.Visible = matches;
                    item.HighlightText = matches ? searchText : null;
                }
            }
            ResumeLayout();
        }

        public virtual IEnumerable<MyListItem> GetAllItems()
        {
            foreach (Control control in Controls)
            {
                if (control is MyListItem item && !item.IsDisposed) yield return item;
            }
        }
    }

    public class MyListItem : Panel
    {
        private string highlightText, displayText;
        private readonly RichTextBox rtbText;
        private bool hasImage;

        public MyListItem()
        {
            SuspendLayout();
            DoubleBuffered = true;
            Height = 50.DpiZoom();
            Margin = new Padding(0);
            BackColor = MyMainForm.FormBack;

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
                TabStop = false,
                HideSelection = true
            };

            Controls.AddRange(new Control[] { lblSeparator, flpControls, rtbText, picImage });
            picImage.Left = 20.DpiZoom(); rtbText.Left = 60.DpiZoom();
            UpdateRichTextBoxPosition();

            Resize += (s, e) => { pnlScrollbar.Height = ClientSize.Height; UpdateRichTextBoxPosition(); };

            // 修复事件绑定
            AttachEvents(rtbText);
            AttachEvents(flpControls);
            foreach (Control ctrl in Controls) { AttachEvents(ctrl); }

            lblSeparator.SetEnabled(false);
            CenterControl(picImage);
            AddCtr(pnlScrollbar, 0);
            hasImage = true; picImage.Visible = true;
            ResumeLayout();
        }

        private void AttachEvents(Control ctrl)
        {
            if (ctrl == rtbText)
            {
                ctrl.MouseDown += (s, e) => { OnMouseDown(e); if (rtbText.Focused) Focus(); };
                ctrl.MouseMove += (s, e) => OnMouseMove(e);
                ctrl.MouseUp += (s, e) => OnMouseUp(e);
                ctrl.MouseClick += (s, e) => OnMouseClick(e);
                ctrl.MouseDoubleClick += (s, e) => OnDoubleClick(e);

                // 分别处理键盘事件
                ctrl.KeyDown += (s, e) => { e.Handled = true; };
                ctrl.KeyPress += (s, e) => { e.Handled = true; };
                ctrl.KeyUp += (s, e) => { e.Handled = true; };
            }
            else
            {
                ctrl.MouseEnter += (s, e) => OnMouseEnter(e);
                ctrl.MouseLeave += (s, e) => OnMouseLeave(e);
                if (ctrl != flpControls) ctrl.MouseDown += (s, e) => OnMouseDown(e);
            }
        }

        private void UpdateRichTextBoxPosition()
        {
            if (rtbText?.IsDisposed != false || IsDisposed) return;
            int textHeight = TextRenderer.MeasureText("A", rtbText.Font).Height;
            rtbText.Top = (Height - textHeight) / 2;
            rtbText.Height = textHeight + 2;
            rtbText.Width = Width - rtbText.Left - flpControls.Width - 10.DpiZoom();
        }

        public Image Image { get => picImage?.Image; set => picImage.Image = value; }
        public new string Text { get => displayText ?? ""; set { displayText = value; rtbText.Text = value; UpdateHighlight(); UpdateRichTextBoxPosition(); } }
        public new Font Font { get => rtbText?.Font; set { rtbText.Font = value; UpdateHighlight(); UpdateRichTextBoxPosition(); } }
        public new Color ForeColor { get => rtbText?.ForeColor ?? Color.Empty; set { rtbText.ForeColor = value; UpdateHighlight(); } }

        public string HighlightText { get => highlightText; set { highlightText = value; UpdateHighlight(); } }
        public Color HighlightColor => IsDarkColor(BackColor) ? Color.FromArgb(255, 100, 70, 0) : Color.FromArgb(255, 255, 220, 100);
        private bool IsDarkColor(Color c) => (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255 < 0.5;

        public bool HasImage
        {
            get => hasImage;
            set { hasImage = value; picImage.Visible = value; rtbText.Left = (value ? 60 : 20).DpiZoom(); UpdateRichTextBoxPosition(); }
        }

        private void UpdateHighlight()
        {
            if (rtbText?.IsDisposed != false || IsDisposed || string.IsNullOrEmpty(Text)) return;
            try
            {
                rtbText.SelectAll();
                rtbText.SelectionBackColor = rtbText.BackColor;
                rtbText.DeselectAll();
                if (!string.IsNullOrEmpty(highlightText))
                {
                    int index = Text.IndexOf(highlightText, StringComparison.OrdinalIgnoreCase);
                    if (index >= 0) { rtbText.Select(index, highlightText.Length); rtbText.SelectionBackColor = HighlightColor; }
                }
                rtbText.DeselectAll();
            }
            catch (ObjectDisposedException) { }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e); OnMouseEnter(e);
            if (rtbText?.Focused == true) Focus();
        }

        private readonly PictureBox picImage = new PictureBox { SizeMode = PictureBoxSizeMode.AutoSize, Enabled = false };
        private readonly FlowLayoutPanel flpControls = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Anchor = AnchorStyles.Right, AutoSize = true };
        private readonly Label lblSeparator = new Label { BackColor = MyMainForm.FormFore, Dock = DockStyle.Bottom, Height = 1 };
        private readonly Panel pnlScrollbar = new Panel { Width = SystemInformation.VerticalScrollBarWidth, Enabled = false };

        private void CenterControl(Control ctr)
        {
            if (ctr == null || IsDisposed) return;
            void reSize()
            {
                if (ctr.IsDisposed || IsDisposed) return;
                int top = (ClientSize.Height - ctr.Height) / 2;
                ctr.Top = top;
                if (ctr.Parent == flpControls) ctr.Margin = new Padding(0, top, ctr.Margin.Right, top);
            }
            ctr.Parent.Resize += (s, e) => reSize();
            ctr.Resize += (s, e) => reSize();
            reSize();
        }

        // 修复方法重载冲突
        public void AddCtr(Control ctr) => AddCtrInternal(ctr, 20.DpiZoom());

        public void AddCtr(Control ctr, int space) => AddCtrInternal(ctr, space.DpiZoom());

        private void AddCtrInternal(Control ctr, int space)
        {
            if (ctr == null || flpControls == null || IsDisposed) return;
            SuspendLayout();
            ctr.Parent = flpControls;
            ctr.Margin = new Padding(0, 0, space, 0);
            ctr.MouseDown += (s, e) => OnMouseDown(e);
            ctr.MouseEnter += (s, e) => OnMouseEnter(e);
            ctr.MouseLeave += (s, e) => OnMouseLeave(e);
            CenterControl(ctr);
            ResumeLayout();
        }

        // 修复Array.ForEach调用
        public void AddCtrs(Control[] ctrs)
        {
            if (ctrs != null)
            {
                foreach (var ctr in ctrs)
                {
                    if (ctr != null && !ctr.IsDisposed)
                        AddCtr(ctr);
                }
            }
        }

        public void RemoveCtrAt(int index) { if (flpControls?.Controls?.Count > index) flpControls.Controls.RemoveAt(index + 1); }
        public int GetCtrIndex(Control ctr) => flpControls?.Controls?.GetChildIndex(ctr, true) - 1 ?? -1;
        public void SetCtrIndex(Control ctr, int newIndex) => flpControls?.Controls?.SetChildIndex(ctr, newIndex + 1);
    }
}