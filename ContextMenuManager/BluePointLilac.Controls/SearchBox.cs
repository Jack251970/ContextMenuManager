using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BluePointLilac.Methods; // 添加DPI缩放方法的命名空间
using ContextMenuManager.Methods; // 添加AppImage命名空间

namespace BluePointLilac.Controls
{
    // 搜索框控件
    public class SearchBox : UserControl
    {
        // 内部文本框控件
        private TextBox textBox;
        private const int IconPadding = 8;
        private string placeholderText = string.Empty;
        private int borderRadius = 18; // 圆角半径，与高度协调
        
        public string PlaceholderText
        {
            get => placeholderText;
            set
            {
                placeholderText = value;
                if (textBox != null && string.IsNullOrEmpty(textBox.Text))
                {
                    Invalidate();
                }
            }
        }

        public string Text
        {
            get => textBox?.Text ?? string.Empty;
            set => textBox.Text = value;
        }

        public Font TextFont
        {
            get => textBox?.Font ?? base.Font;
            set
            {
                if (textBox != null)
                {
                    textBox.Font = value;
                }
            }
        }

        public event EventHandler TextChanged
        {
            add => textBox.TextChanged += value;
            remove => textBox.TextChanged -= value;
        }

        public SearchBox()
        {
            // 设置控件的基本属性
            Size = new Size(150, 36.DpiZoom());
            BackColor = Color.White;
            ForeColor = SystemColors.WindowText;
            DoubleBuffered = true; // 启用双缓冲以减少闪烁
            
            // 初始化内部文本框
            InitializeTextBox();
        }

        private void InitializeTextBox()
        {
            // 初始化文本框
            textBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Color.White, // 白色背景，确保文本清晰可见
                ForeColor = ForeColor,
                Font = SystemFonts.DefaultFont,
                Multiline = false,
                Location = new Point(28, (Height - Font.Height) / 2), // 设置文本框位置，避开图标
                Size = new Size(Width - 36, Font.Height + 4), // 调整大小，只覆盖文本区域
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                Padding = new Padding(0), // 重置内边距
            };
            
            textBox.TextChanged += (sender, e) =>
            {
                Invalidate(); // 当文本改变时重绘控件以更新占位符显示
            };
            
            textBox.GotFocus += (sender, e) =>
            {
                Invalidate(); // 当获得焦点时重绘以更新边框颜色
            };
            
            textBox.LostFocus += (sender, e) =>
            {
                Invalidate(); // 当失去焦点时重绘以更新边框颜色
            };
            
            // 添加文本框到控件
            Controls.Add(textBox);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // 绘制圆角背景
            using (var path = CreateRoundedRectanglePath(ClientRectangle, borderRadius))
            {
                // 绘制圆角背景
                using (var brush = new SolidBrush(BackColor))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brush, path);
                }

                // 根据焦点状态绘制圆角边框
                var currentBorderColor = textBox.ContainsFocus ? MyMainForm.MainColor : Color.Gray;
                using (var pen = new Pen(currentBorderColor, 1.5f))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }

            // 绘制搜索图标
            var iconSize = 16;
            var iconY = (Height - iconSize) / 2;
            var iconX = IconPadding;
            
            // 获取搜索图标并绘制
            var searchIcon = AppImage.Search;
            if (searchIcon != null)
            {
                e.Graphics.DrawImage(searchIcon, new Rectangle(iconX, iconY, iconSize, iconSize));
            }
            else
            {
                // 如果图标不可用，则绘制一个简单的放大镜
                using (var iconPen = new Pen(ForeColor, 1.5f))
                {
                    // 绘制放大镜的圆圈部分
                    e.Graphics.DrawEllipse(iconPen, iconX, iconY, iconSize - 4, iconSize - 4);
                    // 绘制手柄
                    var handleAngle = Math.PI / 4;
                    var x = iconX;
                    var y = iconY;
                    var endX = x + (float)(Math.Cos(handleAngle) * (iconSize - 4) / 1.5) + 2;
                    var endY = y + (float)(Math.Sin(handleAngle) * (iconSize - 4) / 1.5) + 2;
                    e.Graphics.DrawLine(iconPen, x + (iconSize - 4) / 1.8f, y + (iconSize - 4) / 1.8f, endX, endY);
                }
            }

            // 如果文本框为空且有占位符文本，则绘制占位符
            if (string.IsNullOrEmpty(textBox.Text) && !string.IsNullOrEmpty(PlaceholderText))
            {
                TextRenderer.DrawText(e.Graphics, PlaceholderText, Font, 
                    new Point(28, (Height - Font.Height) / 2), 
                    SystemColors.GrayText, 
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }
        }

        // 创建圆角矩形路径
        private GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            var diameter = radius * 2;
            var arc = new Rectangle(rect.Location, new Size(diameter, diameter));

            // 左上角
            path.AddArc(arc, 180, 90);

            // 右上角
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);

            // 右下角
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // 左下角
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // 重新调整内部控件的位置
            if (textBox != null)
            {
                int iconPaddingDpi = IconPadding.DpiZoom();
                int textBoxTop = (Height - textBox.Font.Height.DpiZoom()) / 2;
                int textBoxHeight = textBox.Font.Height.DpiZoom() + 4.DpiZoom();

                textBox.Location = new Point(iconPaddingDpi + 20, textBoxTop);
                textBox.Size = new Size(Width - (iconPaddingDpi * 2) - 20, textBoxHeight);
            }
            Invalidate(); // 重绘控件
        }

        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e);
            if (textBox != null)
            {
                textBox.ForeColor = ForeColor;
            }
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);
            // 不要改变TextBox的背景色，保持白色
            Invalidate();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            if (textBox != null)
            {
                textBox.Font = Font;
                // 重新调整位置以适应新字体
                textBox.Location = new Point(28, (Height - textBox.Font.Height) / 2);
                textBox.Size = new Size(Width - 36, textBox.Font.Height + 4);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // 不执行任何操作，防止背景被绘制
            // 背景将在OnPaint中绘制
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                textBox?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}