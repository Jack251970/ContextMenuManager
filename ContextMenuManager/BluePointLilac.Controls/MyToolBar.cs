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
        public const float SelctedOpacity = 0.8F, HoveredOpacity = 0.4F, UnSelctedOpacity = 0;
        private FlowLayoutPanel buttonPanel;
        private ModernSearchBox searchBox;
        private Panel searchPanel;
        private MyToolBarButton selectedButton;

        public MyToolBar()
        {
            Height = 80.DpiZoom();
            Dock = DockStyle.Top;
            DoubleBuffered = true;
            BackColor = MyMainForm.titleArea;
            ForeColor = MyMainForm.FormFore;
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.Opaque, false);
            InitializeButtonPanel();
            InitializeSearchBox();
        }

        public event EventHandler SelectedButtonChanged;
        public event EventHandler SearchTextChanged;
        public event EventHandler SearchPerformed;
        public string SearchText => searchBox?.SearchText ?? string.Empty;
        public MyToolBarButton[] ToolBarButtons => buttonPanel.Controls.OfType<MyToolBarButton>().ToArray();

        public MyToolBarButton SelectedButton
        {
            get => selectedButton;
            set
            {
                if (selectedButton == value) return;
                if (selectedButton != null)
                {
                    selectedButton.Opacity = UnSelctedOpacity;
                    selectedButton.Cursor = Cursors.Hand;
                    selectedButton.UpdateTextColor();
                }
                selectedButton = value;
                if (selectedButton != null)
                {
                    selectedButton.Opacity = SelctedOpacity;
                    selectedButton.Cursor = Cursors.Default;
                    selectedButton.UpdateTextColor();
                }
                SelectedButtonChanged?.Invoke(this, null);
            }
        }

        public int SelectedIndex
        {
            get
            {
                if (SelectedButton == null) return -1;
                var buttons = buttonPanel.Controls.OfType<MyToolBarButton>().ToList();
                return buttons.IndexOf(SelectedButton);
            }
            set
            {
                var buttons = buttonPanel.Controls.OfType<MyToolBarButton>().ToList();
                SelectedButton = value < 0 || value >= buttons.Count ? null : buttons[value];
            }
        }

        private void InitializeButtonPanel()
        {
            buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            Controls.Add(buttonPanel);
        }

        private void InitializeSearchBox()
        {
            searchPanel = new Panel
            {
                Height = 40.DpiZoom(),
                Width = 220.DpiZoom(),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                BackColor = Color.Transparent
            };
            searchPanel.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            searchPanel.SetStyle(ControlStyles.Opaque, false);

            searchBox = new ModernSearchBox
            {
                Size = new Size(200.DpiZoom(), 32.DpiZoom()),
                Location = new Point(10, 4)
            };

            // 修复：使用 SearchText 属性变化事件而不是 GetTextBox()
            // 在 ModernSearchBox 中添加 TextChanged 事件或在外部定时检查
            var timer = new Timer { Interval = 100 };
            timer.Tick += (s, e) => SearchTextChanged?.Invoke(this, EventArgs.Empty);
            timer.Start();

            searchBox.SearchPerformed += (s, e) => SearchPerformed?.Invoke(this, e);
            searchPanel.Controls.Add(searchBox);
            Controls.Add(searchPanel);
            searchPanel.BringToFront();
            AdjustSearchBoxPosition();
        }

        // 修复：移除 GetSearchTextBox 方法或修改 ModernSearchBox 类
        public void FocusSearchBox() => searchBox?.FocusSearch();
        public void ClearSearch() => searchBox?.ClearSearch();

        public void AddButton(MyToolBarButton button)
        {
            buttonPanel.SuspendLayout();
            button.Parent = buttonPanel;
            button.Margin = new Padding(12, 4, 0, 0).DpiZoom();

            button.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left && button.CanBeSelected) SelectedButton = button; };
            button.MouseEnter += (s, e) => { if (button != SelectedButton) { button.Opacity = HoveredOpacity; button.UpdateTextColor(); } };
            button.MouseLeave += (s, e) => { if (button != SelectedButton) { button.Opacity = UnSelctedOpacity; button.UpdateTextColor(); } };

            buttonPanel.ResumeLayout(true);
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
            Color c1, c2, c3;

            if (MyMainForm.IsDarkTheme())
            {
                c1 = Color.FromArgb(128, 128, 128);
                c2 = Color.FromArgb(56, 56, 56);
                c3 = Color.FromArgb(128, 128, 128);
            }
            else
            {
                c1 = Color.FromArgb(255, 255, 255);
                c2 = Color.FromArgb(230, 230, 230);
                c3 = Color.FromArgb(255, 255, 255);
            }

            using (var brush = new LinearGradientBrush(rect, Color.Empty, Color.Empty, LinearGradientMode.Vertical))
            {
                var blend = new ColorBlend(3);
                blend.Colors = new[] { c1, c2, c3 };
                blend.Positions = new[] { 0f, 0.5f, 1f };
                brush.InterpolationColors = blend;
                e.Graphics.FillRectangle(brush, rect);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            AdjustSearchBoxPosition();
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            AdjustSearchBoxPosition();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (searchPanel != null)
            {
                searchPanel.Visible = Visible;
                if (Visible) AdjustSearchBoxPosition();
            }
        }

        private void AdjustSearchBoxPosition()
        {
            if (searchPanel != null)
            {
                searchPanel.Location = new Point(
                    ClientSize.Width - searchPanel.Width - 20.DpiZoom(),
                    (Height - searchPanel.Height) / 2
                );
            }
        }
    }

    public sealed class MyToolBarButton : Panel
    {
        private float targetOpacity, currentOpacity;
        private readonly Timer animationTimer = new Timer { Interval = 16 };
        private const float AnimationSpeed = 0.15f;
        private const int borderRadius = 10;

        private readonly PictureBox picImage = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.StretchImage,
            Size = new Size(40, 40).DpiZoom(),
            BackColor = Color.Transparent,
            Enabled = false
        };

        private readonly Label lblText = new Label
        {
            ForeColor = MyMainForm.FormFore,
            BackColor = Color.Transparent,
            Font = SystemFonts.MenuFont,
            AutoSize = true,
        };

        public MyToolBarButton(Image image, string text)
        {
            SuspendLayout();
            DoubleBuffered = true;
            ForeColor = MyMainForm.FormFore;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Size = new Size(72, 72).DpiZoom();
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.Opaque, false);

            animationTimer.Tick += (s, e) => UpdateAnimation();
            Controls.AddRange(new Control[] { picImage, lblText });
            lblText.Resize += (s, e) => OnResize(null);
            picImage.Top = 6.DpiZoom();
            lblText.Top = 52.DpiZoom();
            lblText.SetEnabled(false);
            Image = image;
            Text = text;
            ResumeLayout();
        }

        public Image Image { get => picImage.Image; set => picImage.Image = value; }
        public new string Text { get => lblText.Text; set => lblText.Text = value; }
        public bool CanBeSelected { get; set; } = true;

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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var path = CreateRoundedPath(ClientRectangle, borderRadius))
            {
                bool isDarkMode = Parent?.Parent is MyToolBar ? MyMainForm.IsDarkTheme() : false;
                Color baseColor = isDarkMode ? Color.White : Color.Black;
                float opacityFactor = isDarkMode ? 0.4f : 0.6f;
                int alpha = (int)(currentOpacity * 255 * opacityFactor);
                alpha = Math.Max(0, Math.Min(255, alpha));

                using (var brush = new SolidBrush(Color.FromArgb(alpha, baseColor)))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brush, path);
                }
            }
        }

        public void UpdateTextColor()
        {
            bool isDarkMode = Parent?.Parent is MyToolBar ? MyMainForm.IsDarkTheme() : false;
            lblText.ForeColor = (!isDarkMode && currentOpacity > 0.1f) ? Color.White : MyMainForm.FormFore;
        }

        private void UpdateAnimation()
        {
            currentOpacity += (targetOpacity - currentOpacity) * AnimationSpeed;
            if (Math.Abs(currentOpacity - targetOpacity) < 0.01f)
            {
                currentOpacity = targetOpacity;
                animationTimer.Stop();
            }
            UpdateTextColor();
            Invalidate();
            Update();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            lblText.Left = (Width - lblText.Width) / 2;
            picImage.Left = (Width - picImage.Width) / 2;
            Invalidate();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x20;
                return cp;
            }
        }

        private GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}