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
        private TextBox textBox;
        private PictureBox clearButton;
        private const int IconPadding = 12;
        private string placeholderText = "搜索...";
        private int borderRadius = 8;
        private bool showClearButton = true;
        
        [Category("外观"), Description("占位符文本")]
        public string PlaceholderText
        {
            get => placeholderText;
            set { placeholderText = value; Invalidate(); }
        }
        
        [Category("外观"), Description("是否显示清除按钮"), DefaultValue(true)]
        public bool ShowClearButton
        {
            get => showClearButton;
            set { showClearButton = value; UpdateClearButton(); }
        }
        
        [Category("外观"), Description("圆角半径"), DefaultValue(8)]
        public int BorderRadius
        {
            get => borderRadius;
            set { borderRadius = Math.Max(0, value); Invalidate(); }
        }
        
        [Browsable(false)]
        public new string Text
        {
            get => textBox?.Text ?? string.Empty;
            set { textBox.Text = value; UpdateClearButton(); Invalidate(); }
        }
        
        [Browsable(false)]
        public new Font Font
        {
            get => textBox?.Font ?? base.Font;
            set { if (textBox != null) { textBox.Font = value; AdjustLayout(); } }
        }
        
        [Browsable(false)]
        public new Color ForeColor
        {
            get => textBox?.ForeColor ?? base.ForeColor;
            set { if (textBox != null) { textBox.ForeColor = value; clearButton.Image = CreateClearIcon(value); } }
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
        }
        
        private void InitializeComponents()
        {
            // 初始化文本框
            textBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ForeColor = ForeColor,
                Font = new Font("Segoe UI", 9f),
                Multiline = false
            };
            
            // 初始化清除按钮
            clearButton = new PictureBox
            {
                Size = new Size(16, 16).DpiZoom(),
                Cursor = Cursors.Hand,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Visible = false,
                BackColor = Color.Transparent
            };
            
            // 事件处理
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
            
            // 添加控件
            Controls.AddRange(new Control[] { textBox, clearButton });
        }
        
        private void UpdateThemeColors()
        {
            bool isDark = MyMainForm.IsDarkTheme();
            
            // 设置背景色
            BackColor = isDark ? Color.FromArgb(45, 45, 45) : Color.White;
            
            // 设置文本框背景色
            textBox.BackColor = isDark ? Color.FromArgb(55, 55, 55) : Color.White;
            
            // 设置前景色（文本颜色）
            Color textColor = isDark ? Color.White : Color.FromArgb(30, 41, 59);
            textBox.ForeColor = textColor;
            
            // 更新清除按钮图标
            clearButton.Image = CreateClearIcon(textColor);
        }
        
        private Color GetBorderColor()
        {
            if (textBox.ContainsFocus)
            {
                return MyMainForm.MainColor;
            }
            else
            {
                return MyMainForm.IsDarkTheme() ? 
                    Color.FromArgb(80, 80, 80) : 
                    Color.FromArgb(200, 200, 200);
            }
        }
        
        private Color GetPlaceholderColor()
        {
            return MyMainForm.IsDarkTheme() ? 
                Color.FromArgb(150, 150, 150) : 
                Color.FromArgb(120, 120, 120);
        }
        
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
            
            // 绘制圆角背景和边框
            using (var path = CreateRoundedRectanglePath(rect, borderRadius))
            using (var brush = new SolidBrush(BackColor))
            using (var pen = new Pen(GetBorderColor(), 1f))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }
            
            // 绘制搜索图标
            int iconSize = 16.DpiZoom();
            int iconX = IconPadding;
            int iconY = (Height - iconSize) / 2;
            DrawSearchIcon(g, iconX, iconY, iconSize);
            
            // 绘制占位符文本
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
            
            // 备用图标
            var iconColor = MyMainForm.IsDarkTheme() ? Color.White : Color.FromArgb(100, 100, 100);
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
        
        private GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0) 
            {
                path.AddRectangle(rect);
                return path;
            }
            
            int diameter = radius * 2;
            var arc = new Rectangle(rect.Location, new Size(diameter, diameter));
            
            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            
            path.CloseFigure();
            return path;
        }
        
        private void UpdateClearButton()
        {
            clearButton.Visible = showClearButton && !string.IsNullOrEmpty(textBox.Text);
        }
        
        private void AdjustLayout()
        {
            if (textBox == null || clearButton == null) return;
            
            // 文本框布局
            int textBoxHeight = Math.Min(textBox.PreferredHeight, (int)(Height * 0.7f));
            int textBoxY = (Height - textBoxHeight) / 2;
            int textBoxX = IconPadding + 24.DpiZoom();
            int textBoxWidth = Width - textBoxX - 32.DpiZoom();
            
            textBox.Location = new Point(textBoxX, textBoxY);
            textBox.Size = new Size(textBoxWidth, textBoxHeight);
            
            // 清除按钮布局
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
        
        // 当主题变化时更新颜色
        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            UpdateThemeColors();
            Invalidate();
        }
        
        public void Clear() => Text = string.Empty;
        public void FocusTextBox() => textBox?.Focus();
        
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
                textBox?.Dispose();
                clearButton?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}