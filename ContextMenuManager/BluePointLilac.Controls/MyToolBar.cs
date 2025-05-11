using BluePointLilac.Methods;
using System;
using System.Drawing;
using System.Windows.Forms;

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

            Color startColor, endColor;
            if (IsDarkMode())
            {
                startColor = Color.FromArgb(30, 30, 30); // 深色模式起始颜色
                endColor = Color.FromArgb(50, 50, 50);   // 深色模式结束颜色
            }
            else
            {
                startColor = Color.FromArgb(240, 240, 240); // 浅色模式起始颜色
                endColor = Color.FromArgb(200, 200, 200);  // 浅色模式结束颜色
            }

            using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                rect, startColor, endColor, System.Drawing.Drawing2D.LinearGradientMode.Vertical))
            {
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
        private readonly Timer animationTimer = new Timer { Interval = 16 };
        private const float AnimationSpeed = 0.15f;

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
            get => BackColor.A / 255f;
            set
            {
                value = Math.Max(0f, Math.Min(1f, value));
                if (Math.Abs(targetOpacity - value) < 0.001f) return;
                targetOpacity = value;
                if (!animationTimer.Enabled) animationTimer.Start();
            }
        }

        private void UpdateAnimation()
        {
            var currentOpacity = Opacity;
            var newOpacity = currentOpacity + (targetOpacity - currentOpacity) * AnimationSpeed;
            var difference = Math.Abs(newOpacity - targetOpacity);

            if (difference < 0.01f)
            {
                newOpacity = targetOpacity;
                animationTimer.Stop();
            }

            BackColor = Color.FromArgb((int)(newOpacity * 255), MyMainForm.FormFore);

            if (difference >= 0.01f)
            {
                this.Invalidate();
                this.Update();
            }
        }

        public bool CanBeSelected { get; set; } = true;

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            lblText.Left = (Width - lblText.Width) / 2;
            picImage.Left = (Width - picImage.Width) / 2;
        }
    }
}