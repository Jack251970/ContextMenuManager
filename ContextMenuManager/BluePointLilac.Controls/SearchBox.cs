using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BluePointLilac.Methods;

namespace BluePointLilac.Controls
{
    public class SearchBox : UserControl
    {
        private TextBox textBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 9f),
            Multiline = false
        };
        
        private PictureBox clearButton = new PictureBox
        {
            Size = new Size(16, 16).DpiZoom(),
            Cursor = Cursors.Hand,
            SizeMode = PictureBoxSizeMode.StretchImage,
            Visible = false,
            BackColor = Color.Transparent
        };
        
        private const int IconPadding = 12;
        private int borderRadius = 8;
        
        [Category("外观"), Description("占位符文本")]
        public string PlaceholderText { get; set; } = "搜索...";
        
        [Category("外观"), Description("是否显示清除按钮"), DefaultValue(true)]
        public bool ShowClearButton { get; set; } = true;
        
        [Category("外观"), Description("圆角半径"), DefaultValue(8)]
        public int BorderRadius
        {
            get => borderRadius;
            set { borderRadius = Math.Max(0, value); Invalidate(); }
        }
        
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
        
        [Browsable(false)]
        public new Color ForeColor
        {
            get => textBox.ForeColor;
            set { textBox.ForeColor = value; clearButton.Image = CreateClearIcon(value); }
        }
        
        public new event EventHandler TextChanged
        {
            add => textBox.TextChanged += value;
            remove => textBox.TextChanged -= value;
        }
        
        public event EventHandler ClearButtonClicked;
        
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
                ClearButtonClicked?.Invoke(this, EventArgs.Empty);
            };
            
            Controls.AddRange(new Control[] { textBox, clearButton });
        }
        
        private void UpdateThemeColors()
        {
            BackColor = DarkModeHelper.SearchBoxBack;
            textBox.BackColor = DarkModeHelper.SearchBoxBack;
            textBox.ForeColor = DarkModeHelper.FormFore;
            clearButton.Image = CreateClearIcon(DarkModeHelper.FormFore);
        }
        
        private Color GetBorderColor() => DarkModeHelper.GetBorderColor(textBox.ContainsFocus);
        private Color GetPlaceholderColor() => DarkModeHelper.GetPlaceholderColor();
        
        private Image CreateClearIcon(Color color)
        {
            int size = 16.DpiZoom();
            var bitmap = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(color, 1.5f))
                {
                    int padding = 4.DpiZoom();
                    g.DrawLine(pen, padding, padding, size - padding, size - padding);
                    g.DrawLine(pen, size - padding, padding, padding, size - padding);
                }
            }
            return bitmap;
        }
        
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            var rect = ClientRectangle;
            rect.Inflate(-1, -1);
            
            using (var path = DarkModeHelper.CreateRoundedRectanglePath(rect, borderRadius))
            using (var brush = new SolidBrush(BackColor))
            using (var pen = new Pen(GetBorderColor(), 1f))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }
            
            int iconSize = 16.DpiZoom();
            int iconY = (Height - iconSize) / 2;
            DrawSearchIcon(g, IconPadding, iconY, iconSize);
            
            if (string.IsNullOrEmpty(textBox.Text) && !string.IsNullOrEmpty(PlaceholderText))
            {
                var placeholderRect = new Rectangle(textBox.Left, textBox.Top, textBox.Width, textBox.Height);
                TextRenderer.DrawText(g, PlaceholderText, textBox.Font, placeholderRect, 
                    GetPlaceholderColor(), TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }
        
        private void DrawSearchIcon(Graphics g, int x, int y, int size)
        {
            var searchIcon = ContextMenuManager.Methods.AppImage.Search;
            if (searchIcon != null)
            {
                g.DrawImage(searchIcon, new Rectangle(x, y, size, size));
                return;
            }
            
            var iconColor = DarkModeHelper.FormFore;
            using (var pen = new Pen(iconColor, 1.5f))
            {
                g.DrawEllipse(pen, x, y, size - 4, size - 4);
                float centerX = x + (size - 4) / 2f;
                float centerY = y + (size - 4) / 2f;
                float angle = (float)Math.PI / 4;
                float handleLength = size / 2f;
                float endX = centerX + (float)Math.Cos(angle) * handleLength;
                float endY = centerY + (float)Math.Sin(angle) * handleLength;
                g.DrawLine(pen, centerX, centerY, endX, endY);
            }
        }
        
        private void UpdateClearButton() => 
            clearButton.Visible = ShowClearButton && !string.IsNullOrEmpty(textBox.Text);
        
        private void AdjustLayout()
        {
            if (textBox == null || clearButton == null) return;
            
            int textBoxHeight = Math.Min(textBox.PreferredHeight, (int)(Height * 0.7f));
            int textBoxY = (Height - textBoxHeight) / 2;
            int textBoxX = IconPadding + 24.DpiZoom();
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
                textBox?.Dispose();
                clearButton?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}