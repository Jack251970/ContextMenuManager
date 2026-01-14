using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using BluePointLilac.Methods;

namespace BluePointLilac.Controls
{
    public class SearchBox : UserControl
    {
        private readonly TextBox textBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 9f),
            Multiline = false
        };
        
        private readonly PictureBox clearButton = new PictureBox
        {
            Size = new Size(16, 16).DpiZoom(),
            Cursor = Cursors.Hand,
            SizeMode = PictureBoxSizeMode.StretchImage,
            Visible = false,
            BackColor = Color.Transparent
        };
        
        [Category("外观"), Description("占位符文本")]
        public string PlaceholderText { get; set; } = "搜索...";
        
        [Category("外观"), Description("是否显示清除按钮"), DefaultValue(true)]
        public bool ShowClearButton { get; set; } = true;
        
        [Browsable(false)]
        public new string Text
        {
            get => textBox.Text;
            set { textBox.Text = value; UpdateClearButton(); Invalidate(); }
        }
        
        [Browsable(false)]
        public new Font Font
        {
            get => textBox.Font;
            set { textBox.Font = value; AdjustLayout(); }
        }
        
        public new event EventHandler TextChanged
        {
            add => textBox.TextChanged += value;
            remove => textBox.TextChanged -= value;
        }
        
        public SearchBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | 
                    ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
            
            Size = new Size(200, 32.DpiZoom());
            InitializeComponents();
            UpdateThemeColors();
            AdjustLayout();
            
            DarkModeHelper.ThemeChanged += OnThemeChanged;
        }
        
        private void InitializeComponents()
        {
            BackColor = DarkModeHelper.SearchBoxBack;
            textBox.BackColor = DarkModeHelper.SearchBoxBack;
            textBox.ForeColor = DarkModeHelper.FormFore;
            
            textBox.TextChanged += (s, e) => { UpdateClearButton(); Invalidate(); };
            textBox.GotFocus += (s, e) => Invalidate();
            textBox.LostFocus += (s, e) => Invalidate();
            textBox.KeyDown += (s, e) => 
            {
                if (e.KeyCode == Keys.Escape && !string.IsNullOrEmpty(textBox.Text))
                {
                    textBox.Text = string.Empty;
                    e.Handled = e.SuppressKeyPress = true;
                }
            };
            
            clearButton.Click += (s, e) => 
            {
                textBox.Text = string.Empty;
                textBox.Focus();
            };
            
            Controls.AddRange(new Control[] { textBox, clearButton });
        }
        
        private void UpdateThemeColors()
        {
            BackColor = DarkModeHelper.SearchBoxBack;
            textBox.BackColor = DarkModeHelper.SearchBoxBack;
            textBox.ForeColor = DarkModeHelper.FormFore;
        }
        
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var rect = ClientRectangle;
            
            // 绘制背景
            g.FillRectangle(new SolidBrush(DarkModeHelper.SearchBoxBack), rect);
            
            // 绘制边框
            var borderRect = rect;
            borderRect.Inflate(-1, -1);
            g.DrawRectangle(new Pen(DarkModeHelper.GetBorderColor(textBox.ContainsFocus)), borderRect);
            
            // 绘制搜索图标
            DrawSearchIcon(g);
            
            // 绘制占位符文本
            if (string.IsNullOrEmpty(textBox.Text) && !string.IsNullOrEmpty(PlaceholderText))
            {
                var placeholderRect = new Rectangle(textBox.Left, textBox.Top, textBox.Width, textBox.Height);
                TextRenderer.DrawText(g, PlaceholderText, textBox.Font, placeholderRect, 
                    DarkModeHelper.GetPlaceholderColor(), TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }
        }
        
        private void DrawSearchIcon(Graphics g)
        {
            var searchIcon = ContextMenuManager.Methods.AppImage.Search;
            if (searchIcon != null)
            {
                int iconSize = 20.DpiZoom();
                int iconY = (Height - iconSize) / 2;
                int iconX = 12.DpiZoom();
                
                g.DrawImage(searchIcon, iconX, iconY, iconSize, iconSize);
            }
        }
        
        private void UpdateClearButton() => 
            clearButton.Visible = ShowClearButton && !string.IsNullOrEmpty(textBox.Text);
        
        private void AdjustLayout()
        {
            if (textBox == null || clearButton == null) return;
            
            int textBoxHeight = Math.Min(textBox.PreferredHeight, (int)(Height * 0.7f));
            int textBoxY = (Height - textBoxHeight) / 2;
            
            int textBoxX = 44.DpiZoom(); // 12 + 20 + 12
            int textBoxWidth = Width - textBoxX - 32.DpiZoom();
            
            textBox.Location = new Point(textBoxX, textBoxY);
            textBox.Size = new Size(textBoxWidth, textBoxHeight);
            
            int btnSize = 16.DpiZoom();
            clearButton.Location = new Point(Width - 30.DpiZoom(), (Height - btnSize) / 2);
            clearButton.Size = new Size(btnSize, btnSize);
        }
        
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            AdjustLayout();
            Invalidate();
        }
        
        private void OnThemeChanged(object sender, EventArgs e)
        {
            UpdateThemeColors();
            Invalidate();
        }
        
        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            UpdateThemeColors();
            Invalidate();
        }
        
        public void Clear() => Text = string.Empty;
        public void FocusTextBox() => textBox.Focus();
        
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.F))
            {
                FocusTextBox();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DarkModeHelper.ThemeChanged -= OnThemeChanged;
            }
            base.Dispose(disposing);
        }
    }
}