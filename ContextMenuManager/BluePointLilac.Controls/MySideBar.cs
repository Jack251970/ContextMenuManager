using BluePointLilac.Methods;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using Microsoft.Win32;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public sealed class MySideBar : Panel
    {
        // 构造函数
        public MySideBar()
        {
            // 双缓冲设置（移除 OptimizedDoubleBuffer）
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            Dock = DockStyle.Left;
            ItemHeight = 30.DpiZoom();
            Font = new Font(SystemFonts.MenuFont.FontFamily, SystemFonts.MenuFont.Size + 1F);
            ForeColor = SystemColors.ControlText;

            Controls.AddRange(new Control[] { LblSeparator, PnlSelected });
            PnlSelected.Paint += PaintItem;
            SelectedIndex = -1;

            animationTimer.Interval = 1; // 最小间隔1ms
            animationTimer.Tick += AnimationTick;

            // 监听系统主题变化
            SystemEvents.UserPreferenceChanged += OnSystemThemeChanged;

            // 初始颜色适配
            UpdateColors();
        }

        // 深色模式检测（通过注册表）
        private bool IsDarkMode => IsSystemDarkMode();

        private bool IsSystemDarkMode()
        {
            try
            {
                var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                return key != null && key.GetValue("AppsUseLightTheme") is int value && value == 0; // 0 表示深色模式
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error detecting dark mode: {ex.Message}");
                return false; // 默认浅色模式
            }
        }

        // 渐变角度（水平渐变）
        public float GradientAngle { get; set; } = 0F;

        // 项目名称管理
        private string[] itemNames;
        public string[] ItemNames
        {
            get => itemNames;
            set
            {
                itemNames = value;
                if (value != null && !IsFixedWidth)
                {
                    // 使用传统循环计算最大宽度
                    int maxWidth = 0;
                    foreach (var str in value)
                    {
                        maxWidth = Math.Max(maxWidth, GetItemWidth(str));
                    }
                    Width = maxWidth + 2 * HorizontalSpace;
                }
                PnlSelected.Width = Width;
                UpdateColors(); // 更新颜色
                SelectedIndex = -1;
            }
        }

        // 项目高度管理
        private int itemHeight;
        public int ItemHeight
        {
            get => itemHeight;
            set => PnlSelected.Height = itemHeight = value;
        }

        public int TopSpace { get; set; } = 2.DpiZoom();
        public int HorizontalSpace { get; set; } = 20.DpiZoom();
        private float VerticalSpace => (itemHeight - TextHeight) * 0.5F;
        private int TextHeight => TextRenderer.MeasureText(" ", Font).Height;
        public bool IsFixedWidth { get; set; } = true;

        // 分隔线颜色
        public Color SeparatorColor
        {
            get => LblSeparator.BackColor;
            set => LblSeparator.BackColor = value;
        }

        // 选中项颜色
        public Color SelectedBackColor 
        { 
            get => IsDarkMode ? Color.FromArgb(48, 102, 193) : SystemColors.Highlight; 
            set { /* ... */ } 
        }

        // 悬停项颜色
        public Color HoveredBackColor 
        { 
            get => IsDarkMode ? Color.FromArgb(50, 50, 50) : SystemColors.ControlLightLight; 
            set { /* ... */ } 
        }

        // 文字颜色
        public Color SelectedForeColor 
        { 
            get => IsDarkMode ? Color.White : SystemColors.ControlText; 
            set { /* ... */ } 
        }

        public Color HoveredForeColor 
        { 
            get => IsDarkMode ? Color.White : SystemColors.ControlText; 
            set { /* ... */ } 
        }

        // 子控件初始化
        readonly Panel PnlSelected = new Panel
        {
            BackColor = Color.FromArgb(48, 102, 193),
            ForeColor = Color.White,
            Enabled = false
        };

        readonly Label LblSeparator = new Label
        {
            BackColor = SystemColors.ControlDark,
            Dock = DockStyle.Right,
            Width = 1,
        };

        // 文字宽度计算
        public int GetItemWidth(string str)
        {
            return TextRenderer.MeasureText(str, Font).Width + 2 * HorizontalSpace;
        }

        // 主绘制逻辑
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Debug.WriteLine($"OnPaint triggered. IsDarkMode: {IsDarkMode}");

            // 强制清除背景
            e.Graphics.Clear(IsDarkMode ? Color.FromArgb(32, 32, 32) : SystemColors.Window);

            // 绘制背景渐变
            using (var gradientBrush = new LinearGradientBrush(
                ClientRectangle,
                IsDarkMode ? Color.FromArgb(32, 32, 32) : SystemColors.Window, // 深色模式起点色
                IsDarkMode ? Color.FromArgb(16, 16, 16) : SystemColors.Control, // 深色模式终点色
                GradientAngle))
            {
                e.Graphics.FillRectangle(gradientBrush, ClientRectangle);
            }

            // 绘制文字和分隔线
            if (itemNames == null) return;
            for (int i = 0; i < itemNames.Length; i++)
            {
                var rect = GetItemRect(i);
                if (itemNames[i] != null)
                {
                    e.Graphics.DrawString(itemNames[i], Font, new SolidBrush(ForeColor), 
                        new PointF(rect.X + HorizontalSpace, rect.Y + VerticalSpace));
                }
                else
                {
                    e.Graphics.DrawLine(new Pen(SeparatorColor), 
                        rect.Left + HorizontalSpace, rect.Top + rect.Height / 2, 
                        rect.Right - HorizontalSpace, rect.Top + rect.Height / 2);
                }
            }

            // 绘制悬停效果
            if (HoveredIndex >= 0 && HoveredIndex < itemNames.Length)
            {
                var rect = GetItemRect(HoveredIndex);
                using (var hoverBrush = new LinearGradientBrush(
                    rect,
                    Color.FromArgb(50, HoveredBackColor.R, HoveredBackColor.G, HoveredBackColor.B),
                    Color.Transparent,
                    LinearGradientMode.Vertical))
                {
                    e.Graphics.FillRectangle(hoverBrush, rect);
                }

                if (itemNames[HoveredIndex] != null)
                {
                    e.Graphics.DrawString(itemNames[HoveredIndex], Font, 
                        new SolidBrush(HoveredForeColor), 
                        new PointF(rect.X + HorizontalSpace, rect.Y + VerticalSpace));
                }
            }
        }

        // 系统主题变化监听
        private void OnSystemThemeChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.Color)
            {
                Debug.WriteLine("System theme changed. Updating colors...");
                UpdateColors();
            }
        }

        // 更新所有颜色的方法
        private void UpdateColors()
        {
            Debug.WriteLine($"Updating colors. IsDarkMode: {IsDarkMode}");

            // 更新文字颜色
            ForeColor = IsDarkMode ? Color.White : SystemColors.ControlText;

            // 更新分隔线颜色
            SeparatorColor = IsDarkMode ? Color.White : SystemColors.ControlDark;

            // 强制重绘
            Invalidate();         // 刷新自身
            Parent?.Invalidate(); // 刷新父容器
            Refresh();            // 立即刷新界面
        }

        // 动画相关
        private Timer animationTimer = new Timer { Interval = 1 };
        private Stopwatch frameSw = new Stopwatch();
        private int startTop, targetTop;
        private const int AnimationDuration = 200;

        private void RefreshItem(int index)
        {
            if (index < -1 || index >= itemNames?.Length) return;

            animationTimer.Stop();
            frameSw.Restart();

            startTop = PnlSelected.Top;
            targetTop = index < 0 ? -ItemHeight : (TopSpace + index * ItemHeight);
            PnlSelected.Text = index < 0 ? null : ItemNames[index];

            if (startTop != targetTop)
            {
                animationTimer.Start();
            }
            else
            {
                PnlSelected.Top = targetTop;
                SelectIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void AnimationTick(object sender, EventArgs e)
        {
            double elapsed = frameSw.Elapsed.TotalMilliseconds;
            float progress = Math.Min(1f, (float)(elapsed / AnimationDuration));

            int newTop = (int)Math.Round(startTop + (targetTop - startTop) * CubicBezier(progress));

            if (Math.Abs(PnlSelected.Top - newTop) >= 1 || progress >= 0.99f)
            {
                PnlSelected.Top = newTop;
                PnlSelected.Invalidate();
            }

            if (progress >= 1f)
            {
                animationTimer.Stop();
                PnlSelected.Top = targetTop;
                SelectIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private float CubicBezier(float t) => t * t * (3f - 2f * t);

        // 选中项绘制
        private void PaintItem(object sender, PaintEventArgs e)
        {
            var panel = (Panel)sender;
            if (panel == PnlSelected)
            {
                e.Graphics.FillRectangle(new SolidBrush(SelectedBackColor), e.ClipRectangle);
                e.Graphics.DrawString(panel.Text, Font, 
                    new SolidBrush(SelectedForeColor), 
                    new PointF(HorizontalSpace, VerticalSpace));
            }
        }

        // 鼠标事件处理
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            HoveredIndex = IsValidIndex(CalculateIndex(e)) ? CalculateIndex(e) : -1;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && IsValidIndex(CalculateIndex(e)))
            {
                SelectedIndex = CalculateIndex(e);
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            HoveredIndex = -1;
        }

        // 辅助方法
        private Rectangle GetItemRect(int index) => new Rectangle(
            0, 
            TopSpace + index * ItemHeight, 
            Width, 
            ItemHeight
        );

        private int CalculateIndex(MouseEventArgs e) => (e.Y - TopSpace) / ItemHeight;

        private bool IsValidIndex(int index) => 
            index >= 0 && index < itemNames?.Length && !string.IsNullOrEmpty(itemNames[index]);

        // 资源释放
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SystemEvents.UserPreferenceChanged -= OnSystemThemeChanged;
            }
            base.Dispose(disposing);
        }

        // **新增/修复：添加 SelectedIndex 属性**
        private int selectIndex;
        public int SelectedIndex
        {
            get => selectIndex;
            set
            {
                if (selectIndex == value) return;
                RefreshItem(value);
                selectIndex = value;
            }
        }

        private int hoverIndex = -1;
        public int HoveredIndex
        {
            get => hoverIndex;
            set
            {
                if (hoverIndex == value) return;
                int oldIndex = hoverIndex;
                hoverIndex = value;

                if (oldIndex != -1) Invalidate(GetItemRect(oldIndex));
                if (hoverIndex != -1) Invalidate(GetItemRect(hoverIndex));
                HoverIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        // 事件
        public event EventHandler SelectIndexChanged;
        public event EventHandler HoverIndexChanged;
    }
}