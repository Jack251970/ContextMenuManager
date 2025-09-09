using BluePointLilac.Methods;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public sealed class MyToolBar : FlowLayoutPanel
    {
        // 提高不透明度值，使白色背景更加明显
        public const float SelctedOpacity = 0.6F;  // 从0.3提高到0.6
        public const float HoveredOpacity = 0.4F;  // 从0.2提高到0.4
        public const float UnSelctedOpacity = 0;

        public MyToolBar()
        {
            Height = 80.DpiZoom();
            Dock = DockStyle.Top;
            DoubleBuffered = true;
            BackColor = MyMainForm.titleArea;
            ForeColor = MyMainForm.FormFore;
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
                }
                selectedButton = value;
                if (selectedButton != null)
                {
                    selectedButton.Opacity = SelctedOpacity; // 动画过渡到选中状态
                    selectedButton.Cursor = Cursors.Default;
                }
                SelectedButtonChanged?.Invoke(this, null);
            }
        }

        public event EventHandler SelectedButtonChanged;

        public int SelectedIndex
        {
            get => SelectedButton == null ? -1 : Controls.GetChildIndex(SelectedButton);
            set => SelectedButton = value < 0 || value >= Controls.Count ? null : (MyToolBarButton)Controls[value];
        }

        public void AddButton(MyToolBarButton button)
        {
            SuspendLayout();
            button.Parent = this;
            button.Margin = new Padding(12, 4, 0, 0).DpiZoom();

            button.MouseDown += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left && button.CanBeSelected)
                    SelectedButton = button;
            };

            button.MouseEnter += (sender, e) =>
            {
                if (button != SelectedButton)
                    button.Opacity = HoveredOpacity; // 动画过渡到悬停状态
            };

            button.MouseLeave += (sender, e) =>
            {
                if (button != SelectedButton)
                    button.Opacity = UnSelctedOpacity; // 动画过渡到未选中状态
            };

            ResumeLayout();
        }

        public void AddButtons(MyToolBarButton[] buttons)
        {
            int maxWidth = 72.DpiZoom();
            Array.ForEach(buttons, button => maxWidth = Math.Max(maxWidth, TextRenderer.MeasureText(button.Text, button.Font).Width));
            Array.ForEach(buttons, button => { button.Width = maxWidth; AddButton(button); });
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);

            var rect = ClientRectangle;

            Color color1, color2, color3;
            if (IsDarkMode())
            {
                // 深色模式三色渐变
                color1 = Color.FromArgb(128, 128, 128);   // 顶部颜色
                color2 = Color.FromArgb(56, 56, 56);   // 中间颜色
                color3 = Color.FromArgb(128, 128, 128);   // 底部颜色
            }
            else
            {
                // 浅色模式三色渐变
                color1 = Color.FromArgb(255, 235, 59); // 顶部颜色
                color2 = Color.FromArgb(255, 196, 0); // 中间颜色
                color3 = Color.FromArgb(255, 235, 59); // 底部颜色
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

        private bool IsDarkMode()
        {
            try
            {
                var key = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1);
                return key != null && (int)key == 0; // 0 表示深色模式
            }
            catch
            {
                return false; // 默认返回浅色模式
            }
        }
    }

    public sealed class MyToolBarButton : Panel
    {
        private float targetOpacity;
        private float currentOpacity;
        private readonly Timer animationTimer = new Timer { Interval = 16 };
        private const float AnimationSpeed = 0.15f;
        private int borderRadius = 12; // 圆角半径

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
                // 使用当前不透明度计算颜色
                Color fillColor = Color.FromArgb((int)(currentOpacity * 255), Color.White);

                // 使用计算出的颜色填充圆角矩形
                using (var brush = new SolidBrush(fillColor))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brush, path);
                }
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

            // 强制重绘
            this.Invalidate();
            this.Update();
        }

        public bool CanBeSelected { get; set; } = true;

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