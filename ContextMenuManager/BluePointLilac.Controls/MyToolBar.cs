using BluePointLilac.Methods;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace BluePointLilac.Controls
{
    public sealed class MyToolBar : FlowLayoutPanel
    {
        public const float SelctedOpacity = 0.3F;
        public const float HoveredOpacity = 0.2F;
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
                    selectedButton.IsSelected = false;
                    selectedButton.Invalidate(); // 强制重绘以更新选中状态
                }
                selectedButton = value;
                if (selectedButton != null)
                {
                    selectedButton.Opacity = SelctedOpacity; // 动画过渡到选中状态
                    selectedButton.Cursor = Cursors.Default;
                    selectedButton.IsSelected = true;
                    selectedButton.Invalidate(); // 强制重绘以更新选中状态
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

            // 使用三色渐变色
            Color color1 = Color.LightYellow; // 浅黄色
            Color color2 = Color.Gold;        // 金色
            Color color3 = Color.Orange;      // 橙色

            // 创建三色渐变
            using (var brush = new LinearGradientBrush(
                rect, color1, color3, LinearGradientMode.Vertical))
            {
                // 设置混合位置
                var blend = new ColorBlend
                {
                    Colors = new[] { color1, color2, color3 },
                    Positions = new[] { 0f, 0.5f, 1f }
                };
                brush.InterpolationColors = blend;

                e.Graphics.FillRectangle(brush, rect);
            }
        }
    }

    public sealed class MyToolBarButton : Panel
    {
        private float targetOpacity;
        private readonly Timer animationTimer = new Timer { Interval = 16 };
        private const float AnimationSpeed = 0.15f;
        private const int CornerRadius = 12; // 圆角半径
        private readonly Color selectedBackgroundColor = Color.FromArgb(204, 255, 255, 255); // 80%透明白色

        public MyToolBarButton(Image image, string text)
        {
            SuspendLayout();
            DoubleBuffered = true;
            ForeColor = MyMainForm.FormFore;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Size = new Size(72, 72).DpiZoom();

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
            get => targetOpacity;
            set
            {
                value = Math.Max(0f, Math.Min(1f, value));
                if (Math.Abs(targetOpacity - value) < 0.001f) return;
                targetOpacity = value;
                if (!animationTimer.Enabled) animationTimer.Start();
            }
        }

        public bool IsSelected { get; set; }

        private void UpdateAnimation()
        {
            // 不再使用BackColor来存储透明度，而是直接使用targetOpacity
            // 只需要触发重绘即可
            this.Invalidate();
            this.Update();
            
            // 检查是否需要停止定时器
            if (Math.Abs(targetOpacity - Opacity) < 0.01f)
            {
                animationTimer.Stop();
            }
        }

        public bool CanBeSelected { get; set; } = true;

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            lblText.Left = (Width - lblText.Width) / 2;
            picImage.Left = (Width - picImage.Width) / 2;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // 清除背景，确保没有残留
            e.Graphics.Clear(BackColor);
            
            // 如果是选中状态，绘制80%透明白色的圆角矩形
            if (IsSelected)
            {
                using (var path = GetRoundedRectanglePath(ClientRectangle, CornerRadius))
                using (var brush = new SolidBrush(selectedBackgroundColor))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brush, path);
                }
            }
            // 如果不是选中状态但有透明度（悬停状态），绘制半透明背景
            else if (Opacity > 0)
            {
                using (var path = GetRoundedRectanglePath(ClientRectangle, CornerRadius))
                using (var brush = new SolidBrush(Color.FromArgb((int)(Opacity * 255), MyMainForm.FormFore)))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brush, path);
                }
            }
            
            // 绘制图片和文本
            base.OnPaint(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // 不绘制默认背景
            // base.OnPaintBackground(e);
        }

        private GraphicsPath GetRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}