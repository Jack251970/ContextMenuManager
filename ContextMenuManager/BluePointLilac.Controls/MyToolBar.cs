using BluePointLilac.Methods;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public sealed class MyToolBar : Panel
    {
        // 提高不透明度值，使白色背景更加明显
        public const float SelctedOpacity = 0.8F;
        public const float HoveredOpacity = 0.4F;
        public const float UnSelctedOpacity = 0;
        
        private FlowLayoutPanel buttonContainer;
        private Panel searchBoxContainer;

        public MyToolBar()
        {
            Height = 80.DpiZoom();
            Dock = DockStyle.Top;
            DoubleBuffered = true;
            BackColor = MyMainForm.titleArea;
            ForeColor = MyMainForm.FormFore;
            
            // 创建按钮容器（左侧）
            buttonContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                Height = Height,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            
            // 创建搜索框容器（右侧）
            searchBoxContainer = new Panel
            {
                Dock = DockStyle.Right,
                Width = 240.DpiZoom(),
                Height = Height,
                BackColor = Color.Transparent
            };
            
            Controls.Add(buttonContainer);
            Controls.Add(searchBoxContainer);
        }

        private MyToolBarButton selectedButton;
        public MyToolBarButton SelectedButton
        {
            get => selectedButton;
            set
            {
                if (selectedButton == value) return;
                if (selectedButton != null)
                {
                    selectedButton.Opacity = UnSelctedOpacity; // 动画过渡到未选中状态
                    selectedButton.Cursor = Cursors.Hand;
                    selectedButton.UpdateTextColor(); // 更新文字颜色
                }
                selectedButton = value;
                if (selectedButton != null)
                {
                    selectedButton.Opacity = SelctedOpacity; // 动画过渡到选中状态
                    selectedButton.Cursor = Cursors.Default;
                    selectedButton.UpdateTextColor(); // 更新文字颜色
                }
                SelectedButtonChanged?.Invoke(this, null);
            }
        }

        public event EventHandler SelectedButtonChanged;

        public int SelectedIndex
        {
            get => SelectedButton == null ? -1 : buttonContainer.Controls.GetChildIndex(SelectedButton);
            set => SelectedButton = value < 0 || value >= buttonContainer.Controls.Count ? 
                   null : (MyToolBarButton)buttonContainer.Controls[value];
        }

        // 添加一个方法来获取所有按钮（用于兼容旧代码）
        public Control.ControlCollection ButtonControls => buttonContainer.Controls;

        public void AddButton(MyToolBarButton button)
        {
            buttonContainer.SuspendLayout();
            button.Parent = buttonContainer;
            button.Margin = new Padding(12, 4, 0, 0).DpiZoom();

            button.MouseDown += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left && button.CanBeSelected)
                    SelectedButton = button;
            };

            button.MouseEnter += (sender, e) =>
            {
                if (button != SelectedButton)
                {
                    button.Opacity = HoveredOpacity; // 动画过渡到悬停状态
                    button.UpdateTextColor(); // 更新文字颜色
                }
            };

            button.MouseLeave += (sender, e) =>
            {
                if (button != SelectedButton)
                {
                    button.Opacity = UnSelctedOpacity; // 动画过渡到未选中状态
                    button.UpdateTextColor(); // 更新文字颜色
                }
            };

            buttonContainer.ResumeLayout();
        }

        public void AddButtons(MyToolBarButton[] buttons)
        {
            int maxWidth = 72.DpiZoom();
            Array.ForEach(buttons, button => maxWidth = Math.Max(maxWidth, TextRenderer.MeasureText(button.Text, button.Font).Width));
            Array.ForEach(buttons, button => { button.Width = maxWidth; AddButton(button); });
        }

        // 添加搜索框到工具栏右侧
        public void AddSearchBox(SearchBox searchBox)
        {
            // 清除现有搜索框
            searchBoxContainer.Controls.Clear();
            
            // 设置搜索框
            searchBox.Parent = searchBoxContainer;
            searchBox.Width = searchBoxContainer.Width - 40.DpiZoom();
            searchBox.Height = 36.DpiZoom();
            searchBox.Left = 20.DpiZoom();
            searchBox.Top = (searchBoxContainer.Height - searchBox.Height) / 2;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            
            // 调整搜索框容器的宽度
            if (searchBoxContainer != null)
            {
                searchBoxContainer.Width = 240.DpiZoom();
                searchBoxContainer.Height = Height;
                
                // 确保搜索框在容器中垂直居中
                var searchBox = searchBoxContainer.Controls.OfType<SearchBox>().FirstOrDefault();
                if (searchBox != null)
                {
                    searchBox.Width = searchBoxContainer.Width - 40.DpiZoom();
                    searchBox.Top = (searchBoxContainer.Height - searchBox.Height) / 2;
                }
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);

            var rect = ClientRectangle;

            Color color1, color2, color3;
            if (MyMainForm.IsDarkTheme())
            {
                // 深色模式三色渐变
                color1 = Color.FromArgb(128, 128, 128);   // 顶部颜色
                color2 = Color.FromArgb(56, 56, 56);   // 中间颜色
                color3 = Color.FromArgb(128, 128, 128);   // 底部颜色
            }
            else
            {
                // 浅色模式三色渐变
                color1 = Color.FromArgb(255, 255, 255); // 顶部颜色
                color2 = Color.FromArgb(230, 230, 230); // 中间颜色
                color3 = Color.FromArgb(255, 255, 255); // 底部颜色
            }

            // 创建三色渐变
            using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                rect, Color.Empty, Color.Empty, System.Drawing.Drawing2D.LinearGradientMode.Vertical))
            {
                // 使用ColorBlend创建三色渐变
                var colorBlend = new System.Drawing.Drawing2D.ColorBlend(3);
                colorBlend.Colors = new Color[] { color1, color2, color3 };
                colorBlend.Positions = new float[] { 0f, 0.5f, 1f };
                brush.InterpolationColors = colorBlend;

                e.Graphics.FillRectangle(brush, rect);
            }
        }
        
        // 重写Controls属性以保持向后兼容（注意：这可能会影响一些代码）
        public new Control.ControlCollection Controls
        {
            get
            {
                // 返回包含所有子控件的集合
                return base.Controls;
            }
        }
    }

    public sealed class MyToolBarButton : Panel
    {
        private float targetOpacity;
        private float currentOpacity;
        private readonly Timer animationTimer = new Timer { Interval = 16 };
        private const float AnimationSpeed = 0.15f;
        private int borderRadius = 10; // 圆角半径

        public MyToolBarButton(Image image, string text)
        {
            SuspendLayout();
            DoubleBuffered = true;
            ForeColor = MyMainForm.FormFore;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Size = new Size(72, 72).DpiZoom();

            // 启用透明背景
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.Opaque, false);

            animationTimer.Tick += (s, e) => UpdateAnimation();

            Controls.AddRange(new Control[] { picImage, lblText });
            lblText.Resize += (sender, e) => OnResize(null);
            picImage.Top = 6.DpiZoom();
            lblText.Top = 52.DpiZoom();
            lblText.SetEnabled(false);
            Image = image;
            Text = text;
            ResumeLayout();
        }

        readonly PictureBox picImage = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.StretchImage,
            Size = new Size(40, 40).DpiZoom(),
            BackColor = Color.Transparent,
            Enabled = false
        };

        readonly Label lblText = new Label
        {
            ForeColor = MyMainForm.FormFore,
            BackColor = Color.Transparent,
            Font = SystemFonts.MenuFont,
            AutoSize = true,
            Anchor = AnchorStyles.Left | AnchorStyles.Right
        };

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

        public bool CanBeSelected = true;
        private float opacity;
        public float Opacity
        {
            get => currentOpacity;
            set
            {
                value = Math.Max(0f, Math.Min(1f, value));
                if (Math.Abs(targetOpacity - value) < 0.001f) return;
                targetOpacity = value;
                if (!animationTimer.Enabled) animationTimer.Start();
            }
        }

        // 重写OnPaint方法以实现圆角效果
        protected override void OnPaint(PaintEventArgs e)
        {
            // 调用基类绘制以确保子控件正确显示
            base.OnPaint(e);

            // 创建圆角矩形路径
            using (var path = CreateRoundedRectanglePath(ClientRectangle, borderRadius))
            {
                // 根据当前模式选择颜色
                bool isDarkMode = false;
                if (Parent != null && Parent.Parent is MyToolBar toolbar)
                {
                    isDarkMode = MyMainForm.IsDarkTheme();
                }

                // 深色模式使用白色，浅色模式使用黑色
                Color baseColor = isDarkMode ? Color.White : Color.Black;

                // 减少两种模式的不透明度
                float opacityFactor = isDarkMode ? 0.4f : 0.6f; // 深色模式减少更多（0.4），浅色模式减少较少（0.6）
                int alpha = (int)(currentOpacity * 255 * opacityFactor);
                alpha = Math.Max(0, Math.Min(255, alpha)); // 确保alpha值在有效范围内

                Color fillColor = Color.FromArgb(alpha, baseColor);

                // 使用计算出的颜色填充圆角矩形
                using (var brush = new SolidBrush(fillColor))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brush, path);
                }
            }
        }

        // 更新文字颜色的方法
        public void UpdateTextColor()
        {
            bool isDarkMode = false;
            if (Parent != null && Parent.Parent is MyToolBar toolbar)
            {
                isDarkMode = MyMainForm.IsDarkTheme();
            }

            // 浅色模式下，当按钮被选中或悬停时，文字颜色改为白色
            if (!isDarkMode && currentOpacity > 0.1f)
            {
                lblText.ForeColor = Color.White;
            }
            else
            {
                lblText.ForeColor = MyMainForm.FormFore;
            }
        }

        // 创建圆角矩形路径的辅助方法
        private GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void UpdateAnimation()
        {
            currentOpacity += (targetOpacity - currentOpacity) * AnimationSpeed;
            var difference = Math.Abs(currentOpacity - targetOpacity);

            if (difference < 0.01f)
            {
                currentOpacity = targetOpacity;
                animationTimer.Stop();
            }

            // 更新文字颜色
            UpdateTextColor();

            // 强制重绘
            this.Invalidate();
            this.Update();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            lblText.Left = (Width - lblText.Width) / 2;
            picImage.Left = (Width - picImage.Width) / 2;
            this.Invalidate(); // 重绘以确保圆角正确显示
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
    }
}