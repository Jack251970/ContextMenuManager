using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public class ModernSearchBox : Panel
    {
        private TextBox searchTextBox;
        private bool isFocused = false;
        private bool isMouseOverIcon = false;
        private int borderRadius = 8;
        private Color borderColor;
        private Color hoverBorderColor;
        private Color focusBorderColor;
        private Rectangle iconRect;

        public event EventHandler SearchPerformed;

        public ModernSearchBox()
        {
            InitializeComponent();
            DoubleBuffered = true;
            UpdateColors();

            // 启用透明背景支持
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.Opaque, false);
            BackColor = Color.Transparent;

            // 设置图标区域
            UpdateIconRect();

            // 添加全局鼠标点击事件监听
            Application.AddMessageFilter(new GlobalMouseMessageFilter(this));
        }

        public string SearchText
        {
            get => searchTextBox.Text;
            set => searchTextBox.Text = value;
        }

        private void InitializeComponent()
        {
            // 设置面板大小
            Size = new Size(200, 32);

            // 创建搜索文本框
            searchTextBox = new TextBox
            {
                Location = new Point(12, 6),
                Size = new Size(160, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                PlaceholderText = "搜索...",
                BorderStyle = BorderStyle.None,
                BackColor = GetBackgroundColor(),
                ForeColor = MyMainForm.FormFore
            };

            // 事件处理
            searchTextBox.GotFocus += (s, e) =>
            {
                isFocused = true;
                Invalidate();
            };

            searchTextBox.LostFocus += (s, e) =>
            {
                isFocused = false;
                Invalidate();
            };

            searchTextBox.TextChanged += (s, e) => Invalidate();

            searchTextBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    PerformSearch();
                    e.Handled = e.SuppressKeyPress = true;
                }
            };

            // 添加控件
            Controls.Add(searchTextBox);

            // 鼠标事件
            MouseEnter += (s, e) => Invalidate();
            MouseLeave += (s, e) =>
            {
                isMouseOverIcon = false;
                Invalidate();
            };
            MouseMove += (s, e) =>
            {
                bool wasOverIcon = isMouseOverIcon;
                isMouseOverIcon = iconRect.Contains(e.Location);

                if (wasOverIcon != isMouseOverIcon)
                {
                    Cursor = isMouseOverIcon ? Cursors.Hand : Cursors.Default;
                    Invalidate();
                }
            };
            MouseClick += (s, e) =>
            {
                if (iconRect.Contains(e.Location) && e.Button == MouseButtons.Left)
                {
                    PerformSearch();
                }
            };

            searchTextBox.MouseEnter += (s, e) => Invalidate();
            searchTextBox.MouseLeave += (s, e) => Invalidate();
        }

        private void UpdateColors()
        {
            if (MyMainForm.IsDarkTheme())
            {
                // 深色主题
                borderColor = Color.FromArgb(80, 80, 80);
                hoverBorderColor = Color.FromArgb(120, 120, 120);
                focusBorderColor = MyMainForm.MainColor;
            }
            else
            {
                // 浅色主题
                borderColor = Color.FromArgb(180, 180, 180);
                hoverBorderColor = Color.FromArgb(140, 140, 140);
                focusBorderColor = MyMainForm.MainColor;
            }
        }

        private Color GetBackgroundColor()
        {
            return MyMainForm.IsDarkTheme()
                ? Color.FromArgb(45, 45, 45)
                : Color.FromArgb(250, 250, 250);
        }

        private void UpdateIconRect()
        {
            iconRect = new Rectangle(Width - 28, 8, 16, 16);
        }

        private void PerformSearch()
        {
            SearchPerformed?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 确定边框颜色
            Color currentBorderColor = borderColor;
            if (isFocused)
            {
                currentBorderColor = focusBorderColor;
            }
            else if (ClientRectangle.Contains(PointToClient(MousePosition)) || isMouseOverIcon)
            {
                currentBorderColor = hoverBorderColor;
            }

            // 绘制圆角背景
            using (var path = CreateRoundedRectanglePath(ClientRectangle, borderRadius))
            {
                // 背景颜色
                Color bgColor = GetBackgroundColor();
                using (var brush = new SolidBrush(bgColor))
                {
                    g.FillPath(brush, path);
                }

                // 边框
                using (var pen = new Pen(currentBorderColor, isFocused ? 1.5f : 1f))
                {
                    g.DrawPath(pen, path);
                }
            }

            // 绘制搜索图标
            DrawSearchIcon(g, iconRect.Location, iconRect.Size);
        }

        private void DrawSearchIcon(Graphics g, Point location, Size size)
        {
            var iconColor = isMouseOverIcon ? focusBorderColor :
                           isFocused ? focusBorderColor :
                           ClientRectangle.Contains(PointToClient(MousePosition)) ? hoverBorderColor :
                           borderColor;

            using (var pen = new Pen(iconColor, 1.5f))
            {
                // 绘制放大镜圆形
                int circleDiameter = 8;
                Rectangle circleRect = new Rectangle(
                    location.X + 2,
                    location.Y + 2,
                    circleDiameter,
                    circleDiameter
                );
                g.DrawArc(pen, circleRect, 0, 360);

                // 绘制放大镜手柄
                Point lineStart = new Point(
                    location.X + circleDiameter,
                    location.Y + circleDiameter
                );
                Point lineEnd = new Point(
                    location.X + size.Width - 2,
                    location.Y + size.Height - 2
                );
                g.DrawLine(pen, lineStart, lineEnd);
            }
        }

        private GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();

            // 左上角
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            // 右上角
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            // 右下角
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            // 左下角
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);

            path.CloseFigure();
            return path;
        }

        public void FocusSearch()
        {
            searchTextBox.Focus();
            searchTextBox.SelectAll();
        }

        public void ClearSearch()
        {
            searchTextBox.Text = string.Empty;
        }

        public void LoseFocus()
        {
            // 将焦点转移到父控件或其他控件
            if (Parent != null)
            {
                Parent.Focus();
            }
        }

        public TextBox GetTextBox()
        {
            return searchTextBox;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (searchTextBox != null)
            {
                searchTextBox.Width = Width - 40; // 留出图标和边距空间
                searchTextBox.Location = new Point(8, (Height - searchTextBox.Height) / 2);
            }

            UpdateIconRect();
            Invalidate();
        }

        // 确保透明背景正确渲染
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x20; // 添加 WS_EX_TRANSPARENT 样式
                return cp;
            }
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);
            UpdateColors();
            Invalidate();
        }

        // 防止点击搜索图标时失去焦点
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (iconRect.Contains(e.Location))
            {
                // 点击图标时不转移焦点，保持搜索框的焦点状态
                return;
            }
        }

        // 防止鼠标移动到其他控件时失去焦点
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            // 不自动失去焦点，只有当用户点击其他地方时才失去焦点
        }

        // 内部类：全局鼠标消息过滤器
        private class GlobalMouseMessageFilter : IMessageFilter
        {
            private readonly ModernSearchBox searchBox;

            public GlobalMouseMessageFilter(ModernSearchBox searchBox)
            {
                this.searchBox = searchBox;
            }

            public bool PreFilterMessage(ref Message m)
            {
                // 监听鼠标按下消息
                if (m.Msg == 0x201 || m.Msg == 0x202) // WM_LBUTTONDOWN 或 WM_LBUTTONUP
                {
                    // 检查点击是否在搜索框内
                    if (!searchBox.IsMouseInside(searchBox, Control.MousePosition))
                    {
                        // 点击在搜索框外部，取消焦点
                        searchBox.LoseFocus();
                    }
                }
                return false;
            }
        }

        // 检查鼠标是否在控件内
        private bool IsMouseInside(Control control, Point screenPoint)
        {
            // 将屏幕坐标转换为控件相对坐标
            Point clientPoint = control.PointToClient(screenPoint);

            // 检查是否在控件客户区内
            if (control.ClientRectangle.Contains(clientPoint))
            {
                return true;
            }

            // 递归检查子控件
            foreach (Control child in control.Controls)
            {
                if (IsMouseInside(child, screenPoint))
                {
                    return true;
                }
            }

            return false;
        }
    }
}