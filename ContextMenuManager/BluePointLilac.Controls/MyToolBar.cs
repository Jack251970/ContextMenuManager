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
        public const float SelctedOpacity = 1.0F;
        public const float HoveredOpacity = 0.6F;
        public const float UnSelctedOpacity = 0;

        private readonly FlowLayoutPanel buttonContainer;
        private readonly Panel searchBoxContainer;

        public MyToolBar()
        {
            Height = 80.DpiZoom();
            Dock = DockStyle.Top;
            DoubleBuffered = true;
            BackColor = DarkModeHelper.TitleArea;
            ForeColor = DarkModeHelper.FormFore;

            buttonContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                Height = Height,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            searchBoxContainer = new Panel
            {
                Dock = DockStyle.Right,
                Width = 240.DpiZoom(),
                Height = Height,
                BackColor = Color.Transparent
            };

            Controls.Add(buttonContainer);
            Controls.Add(searchBoxContainer);

            DarkModeHelper.ThemeChanged += OnThemeChanged;
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
                    selectedButton.Opacity = UnSelctedOpacity;
                    selectedButton.Cursor = Cursors.Hand;
                    selectedButton.UpdateTextColor();
                    selectedButton.IsSelected = false;
                }
                selectedButton = value;
                if (selectedButton != null)
                {
                    selectedButton.Opacity = SelctedOpacity;
                    selectedButton.Cursor = Cursors.Default;
                    selectedButton.UpdateTextColor();
                    selectedButton.IsSelected = true;
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
                    button.Opacity = HoveredOpacity;
                    button.UpdateTextColor();
                    button.IsHovered = true;
                }
            };

            button.MouseLeave += (sender, e) =>
            {
                if (button != SelectedButton)
                {
                    button.Opacity = UnSelctedOpacity;
                    button.UpdateTextColor();
                    button.IsHovered = false;
                }
            };

            buttonContainer.ResumeLayout();
        }

        public void AddButtons(MyToolBarButton[] buttons)
        {
            var maxWidth = 72.DpiZoom();
            Array.ForEach(buttons, button => maxWidth = Math.Max(maxWidth, TextRenderer.MeasureText(button.Text, button.Font).Width));
            Array.ForEach(buttons, button => { button.Width = maxWidth; AddButton(button); });
        }

        public void AddSearchBox(SearchBox searchBox)
        {
            searchBoxContainer.Controls.Clear();

            searchBox.Parent = searchBoxContainer;
            searchBox.Width = searchBoxContainer.Width - 40.DpiZoom();
            searchBox.Height = 36.DpiZoom();
            searchBox.Left = 20.DpiZoom();
            searchBox.Top = (searchBoxContainer.Height - searchBox.Height) / 2;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (searchBoxContainer != null)
            {
                searchBoxContainer.Width = 240.DpiZoom();
                searchBoxContainer.Height = Height;

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
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var color1 = DarkModeHelper.ToolBarGradientTop;
            var color2 = DarkModeHelper.ToolBarGradientMiddle;
            var color3 = DarkModeHelper.ToolBarGradientBottom;

            using var brush = new LinearGradientBrush(rect, Color.Empty, Color.Empty, LinearGradientMode.Vertical);
            var colorBlend = new ColorBlend(3);
            colorBlend.Colors = new Color[] { color1, color2, color3 };
            colorBlend.Positions = new float[] { 0f, 0.5f, 1f };
            brush.InterpolationColors = colorBlend;

            g.FillRectangle(brush, rect);

            using var borderPen = new Pen(DarkModeHelper.IsDarkTheme ?
                Color.FromArgb(60, 60, 60) : Color.FromArgb(220, 220, 220), 1);
            g.DrawLine(borderPen, 0, rect.Bottom - 1, rect.Right, rect.Bottom - 1);
        }

        public new Control.ControlCollection Controls => base.Controls;

        private void OnThemeChanged(object sender, EventArgs e)
        {
            BackColor = DarkModeHelper.TitleArea;
            ForeColor = DarkModeHelper.FormFore;
            Invalidate();
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

    public sealed class MyToolBarButton : Panel
    {
        private float targetOpacity;
        private float currentOpacity;
        private float currentScale = 1.0f;
        private float targetScale = 1.0f;
        private readonly Timer animationTimer = new() { Interval = 16 };
        private const float AnimationSpeed = 0.12f;
        private const float ScaleAnimationSpeed = 0.15f;
        private readonly int borderRadius = 12;

        private bool isSelected = false;
        private bool isHovered = false;

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    targetScale = value ? 1.02f : 1.0f;
                    if (!animationTimer.Enabled) animationTimer.Start();
                }
            }
        }

        public bool IsHovered
        {
            get => isHovered;
            set
            {
                if (isHovered != value && !isSelected)
                {
                    isHovered = value;
                    targetScale = value ? 1.01f : 1.0f;
                    if (!animationTimer.Enabled) animationTimer.Start();
                }
            }
        }

        public MyToolBarButton(Image image, string text)
        {
            SuspendLayout();
            DoubleBuffered = true;
            ForeColor = DarkModeHelper.FormFore;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Size = new Size(72, 72).DpiZoom();

            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.Opaque, false);
            SetStyle(ControlStyles.ResizeRedraw, true);

            animationTimer.Tick += (s, e) => UpdateAnimation();

            Controls.AddRange(new Control[] { picImage, lblText });
            lblText.Resize += (sender, e) => OnResize(null);
            picImage.Top = 8.DpiZoom();
            lblText.Top = 50.DpiZoom();
            lblText.SetEnabled(false);
            Image = image;
            Text = text;
            ResumeLayout();
        }

        private readonly PictureBox picImage = new()
        {
            SizeMode = PictureBoxSizeMode.StretchImage,
            Size = new Size(36, 36).DpiZoom(),
            BackColor = Color.Transparent,
            Enabled = false
        };

        private readonly Label lblText = new()
        {
            ForeColor = DarkModeHelper.FormFore,
            BackColor = Color.Transparent,
            Font = new Font(SystemFonts.MenuFont.FontFamily, SystemFonts.MenuFont.SizeInPoints, FontStyle.Regular, GraphicsUnit.Point),
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

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            var isDarkMode = DarkModeHelper.IsDarkTheme;
            var mainColor = DarkModeHelper.MainColor;

            var padding = 4;
            var drawRect = new Rectangle(
                padding,
                padding,
                Width - padding * 2,
                Height - padding * 2);

            using var path = DarkModeHelper.CreateRoundedRectanglePath(drawRect, borderRadius);

            if (currentOpacity > 0.01f)
            {
                var baseColor = isDarkMode ? Color.White : mainColor;
                var opacityFactor = isDarkMode ? 0.35f : 0.5f;
                var alpha = (int)(currentOpacity * 255 * opacityFactor);
                alpha = Math.Max(0, Math.Min(255, alpha));
                var fillColor = Color.FromArgb(alpha, baseColor);

                using var brush = new SolidBrush(fillColor);
                g.FillPath(brush, path);

                if (isSelected && currentOpacity > 0.5f)
                {
                    var glowAlpha = (int)(currentOpacity * 60);
                    var glowColor = Color.FromArgb(glowAlpha, mainColor);
                    using var glowPath = DarkModeHelper.CreateRoundedRectanglePath(
                        new Rectangle(drawRect.X - 2, drawRect.Y - 2, drawRect.Width + 4, drawRect.Height + 4),
                        borderRadius + 2);
                    using var glowPen = new Pen(glowColor, 2);
                    g.DrawPath(glowPen, glowPath);
                }

                var borderColor = isSelected ?
                    Color.FromArgb(180, mainColor) :
                    Color.FromArgb(100, isDarkMode ? Color.White : mainColor);
                using var borderPen = new Pen(borderColor, 1);
                g.DrawPath(borderPen, path);
            }
        }

        public void UpdateTextColor()
        {
            var isDarkMode = DarkModeHelper.IsDarkTheme;

            if (!isDarkMode && currentOpacity > 0.3f)
            {
                lblText.ForeColor = Color.White;
            }
            else
            {
                lblText.ForeColor = DarkModeHelper.FormFore;
            }
        }

        private void UpdateAnimation()
        {
            var needsUpdate = false;

            var opacityDiff = targetOpacity - currentOpacity;
            if (Math.Abs(opacityDiff) > 0.001f)
            {
                currentOpacity += opacityDiff * AnimationSpeed;
                needsUpdate = true;
            }
            else
            {
                currentOpacity = targetOpacity;
            }

            var scaleDiff = targetScale - currentScale;
            if (Math.Abs(scaleDiff) > 0.001f)
            {
                currentScale += scaleDiff * ScaleAnimationSpeed;
                needsUpdate = true;
            }
            else
            {
                currentScale = targetScale;
            }

            UpdateTextColor();

            Invalidate();
            Update();

            if (!needsUpdate)
            {
                animationTimer.Stop();
            }
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
                var cp = base.CreateParams;
                cp.ExStyle |= 0x20;
                return cp;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                animationTimer?.Stop();
                animationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}