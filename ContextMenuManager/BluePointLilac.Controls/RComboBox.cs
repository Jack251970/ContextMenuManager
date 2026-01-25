using BluePointLilac.Controls;
using BluePointLilac.Methods;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ContextMenuManager.BluePointLilac.Controls
{
    // 一个支持圆角、动画和深色模式的 ComboBox 控件。
    public unsafe partial class RComboBox : ComboBox
    {
        #region Fields

        // State
        private int originalSelectedIndex = -1;
        private bool mouseOverDropDown = false;
        private bool focused = false;

        // Animation
        private readonly Timer animTimer;
        private float borderWidth = 1.2f;
        private float targetWidth = 1.2f;
        private Color currentBorder;
        private Color targetBorder;
        private int animatedIndex = -1;
        private int previousAnimatedIndex = -1;
        private float hoverProgress = 0f;

        // Style
        [DefaultValue(8)]
        [Category("Style")]
        public int BorderRadius { get; set; } = 8;

        // Win32
        private IntPtr dropDownHwnd = IntPtr.Zero;

        #endregion

        #region Properties

        private Color hoverColor = Color.FromArgb(255, 145, 60);
        // 获取或设置鼠标悬停时控件的边框颜色。
        [DefaultValue(typeof(Color), "255, 145, 60")]
        public Color HoverColor
        {
            get => hoverColor;
            set
            {
                hoverColor = value;
                DropDownHoverColor = value;
            }
        }

        private Color focusColor = Color.FromArgb(255, 107, 0);
        // 获取或设置控件获得焦点时的边框颜色。
        [DefaultValue(typeof(Color), "255, 107, 0")]
        public Color FocusColor
        {
            get => focusColor;
            set
            {
                focusColor = value;
                DropDownSelectedColor = value;
            }
        }

        // 获取或设置下拉箭头的颜色。
        [DefaultValue(typeof(Color), "100, 100, 100")]
        public Color ArrowColor { get; set; } = Color.FromArgb(100, 100, 100);

        // 获取或设置是否根据内容自动调整控件宽度。
        [DefaultValue(true)]
        public new bool AutoSize { get; set; } = true;

        // 获取或设置控件的最小宽度。
        [DefaultValue(120)]
        private int minWidth = 120;
        public int MinWidth
        {
            get => minWidth.DpiZoom();
            set => minWidth = value;
        }

        // 获取或设置控件的最大宽度。
        [DefaultValue(400)]
        private int maxWidth = 400;
        public int MaxWidth
        {
            get => maxWidth.DpiZoom();
            set => maxWidth = value;
        }

        // 获取或设置文本与控件边缘的内边距。
        [DefaultValue(50)]
        private int textPadding = 5;
        public int TextPadding
        {
            get => textPadding.DpiZoom();
            set => textPadding = value;
        }

        #endregion

        #region DropDown Properties

        // 获取或设置下拉列表中各项的高度。
        [Category("DropDown"), Description("下拉列表中各项的高度")]
        [DefaultValue(32)]
        public int DropDownItemHeight { get; set; } = 32;

        // 获取或设置下拉列表各项的字体。
        [Category("DropDown"), Description("下拉列表各项的字体")]
        [DefaultValue(null)]
        public Font DropDownFont { get; set; }

        // 获取或设置下拉列表鼠标悬停项的背景色。
        [Category("DropDown"), Description("下拉列表鼠标悬停项的背景色")]
        public Color DropDownHoverColor { get; set; }

        // 获取或设置下拉列表鼠标悬停项的文字色。
        [Category("DropDown"), Description("下拉列表鼠标悬停项的文字色")]
        [DefaultValue(typeof(Color), "White")]
        public Color DropDownHoverForeColor { get; set; } = Color.White;

        // 获取或设置下拉列表选中项的背景色。
        [Category("DropDown"), Description("下拉列表选中项的背景色")]
        public Color DropDownSelectedColor { get; set; }

        // 获取或设置下拉列表选中项的文字色。
        [Category("DropDown"), Description("下拉列表选中项的文字色")]
        [DefaultValue(typeof(Color), "White")]
        public Color DropDownSelectedForeColor { get; set; } = Color.White;

        // 获取或设置下拉列表各项的文字色。
        [Category("DropDown"), Description("下拉列表各项的文字色")]
        public Color DropDownForeColor { get; set; }

        #endregion

        public void AutosizeDropDownWidth()
        {
            var maxWidth = 0;
            foreach (var item in Items)
            {
                var width = TextRenderer.MeasureText(item.ToString(), Font).Width;
                if (width > maxWidth)
                {
                    maxWidth = width;
                }
            }
            DropDownWidth = maxWidth + SystemInformation.VerticalScrollBarWidth;
        }

        // 处理控件的构造和样式设置。
        public RComboBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.ResizeRedraw |
                    ControlStyles.OptimizedDoubleBuffer, true);

            DrawMode = DrawMode.OwnerDrawVariable;
            DropDownStyle = ComboBoxStyle.DropDownList;
            FlatStyle = FlatStyle.Flat;
            Height = 32.DpiZoom();

            animTimer = new Timer { Interval = 16 };
            InitEvents();
        }

        // 初始化所有事件订阅。
        private void InitEvents()
        {
            DarkModeHelper.ThemeChanged += OnThemeChanged;

            GotFocus += (s, e) => { focused = true; UpdateState(); };
            LostFocus += (s, e) => { focused = false; UpdateState(); };
            MouseEnter += (s, e) => UpdateState();
            MouseLeave += (s, e) => { mouseOverDropDown = false; UpdateState(); };
            MouseMove += (s, e) => UpdateDropDownHoverState(e.Location);
            MouseDown += RComboBox_MouseDown;

            SelectedIndexChanged += (s, e) => { if (AutoSize) AdjustWidth(); };
            TextChanged += (s, e) => { if (AutoSize) AdjustWidth(); };

            DropDown += RComboBox_DropDown;
            DropDownClosed += (s, e) => {
                originalSelectedIndex = animatedIndex = previousAnimatedIndex = -1;
                dropDownHwnd = IntPtr.Zero;
            };

            animTimer.Tick += AnimTimer_Tick;
            animTimer.Start();
        }

        // 处理鼠标按下事件以打开下拉列表。
        private void RComboBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && !DroppedDown)
            {
                // 确保控件获得焦点
                if (!Focused)
                {
                    Focus();
                }
                // 打开下拉列表
                DroppedDown = true;
            }
        }

        // 动画计时器的 Tick 事件处理程序，用于更新边框和下拉项的动画效果。
        private void AnimTimer_Tick(object sender, EventArgs e)
        {
            if (IsDisposed)
            {
                animTimer.Stop();
                return;
            }

            var needsRedraw = false;
            if (Math.Abs(borderWidth - targetWidth) > 0.01f)
            {
                borderWidth += (targetWidth - borderWidth) * 0.3f;
                needsRedraw = true;
            }
            if (currentBorder != targetBorder)
            {
                currentBorder = ColorLerp(currentBorder, targetBorder, 0.25f);
                needsRedraw = true;
            }
            if (needsRedraw) Invalidate();

            UpdateDropDownAnimation();
        }

        // 更新下拉列表项的悬停动画状态。
        private void UpdateDropDownAnimation()
        {
            if (DroppedDown && dropDownHwnd != IntPtr.Zero)
            {
                GetCursorPos(out var p);
                var r = new RECT();
                GetWindowRect(dropDownHwnd, ref r);
                var dropDownRect = new Rectangle(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);

                var hoverIndex = -1;
                if (dropDownRect.Contains(p.X, p.Y))
                {
                    hoverIndex = (p.Y - dropDownRect.Top) / DropDownItemHeight.DpiZoom();
                    if (hoverIndex < 0 || hoverIndex >= Items.Count || hoverIndex == originalSelectedIndex)
                    {
                        hoverIndex = -1;
                    }
                }

                if (animatedIndex != hoverIndex)
                {
                    previousAnimatedIndex = animatedIndex;
                    animatedIndex = hoverIndex;
                    hoverProgress = 0f;
                }
            }
            else
            {
                if (animatedIndex != -1)
                {
                    previousAnimatedIndex = animatedIndex;
                    animatedIndex = -1;
                    hoverProgress = 0f;
                }
            }

            if (hoverProgress < 1f)
            {
                hoverProgress += 0.1f;
                if (hoverProgress > 1f) hoverProgress = 1f;

                if (dropDownHwnd != IntPtr.Zero)
                {
                    InvalidateRect(dropDownHwnd, IntPtr.Zero, true);
                }
            }
            else
            {
                previousAnimatedIndex = -1;
            }
        }

        // 测量下拉列表中项的大小。
        protected override void OnMeasureItem(MeasureItemEventArgs e)
        {
            base.OnMeasureItem(e);
            e.ItemHeight = DropDownItemHeight.DpiZoom();
        }

        // 在下拉列表打开时触发，设置下拉列表的高度和圆角。
        private void RComboBox_DropDown(object sender, EventArgs e)
        {
            originalSelectedIndex = SelectedIndex;
            DropDownHeight = Items.Count * DropDownItemHeight.DpiZoom() + 2 * SystemInformation.BorderSize.Height;

            if (Parent.FindForm() is Form form)
                form.BeginInvoke(() =>
                {
                    try
                    {
                        // 验证控件句柄是否有效
                        if (IsDisposed || !IsHandleCreated) return;

                        var cbi = new COMBOBOXINFO { cbSize = Marshal.SizeOf<COMBOBOXINFO>() };
                        if (!GetComboBoxInfo(Handle, ref cbi)) return;
                        
                        // 验证下拉窗口句柄是否有效
                        if (cbi.hwndList == IntPtr.Zero) return;
                        
                        dropDownHwnd = cbi.hwndList;
                        var r = new RECT();
                        if (!GetWindowRect(cbi.hwndList, ref r)) return;
                        
                        var h = CreateRoundRectRgn(0, 0, r.Right - r.Left, r.Bottom - r.Top, BorderRadius.DpiZoom(), BorderRadius.DpiZoom());
                        if (h == IntPtr.Zero) return;
                        
                        // SetWindowRgn 在成功时会接管 region 句柄的所有权，失败时需要手动删除
                        if (SetWindowRgn(cbi.hwndList, h, true) == 0)
                        {
                            DeleteObject(h);
                        }
                    }
                    catch (Exception ex) when (ex is Win32Exception or ObjectDisposedException or InvalidOperationException)
                    {
                        // 静默捕获预期的异常，防止 ExecutionEngineException 导致程序崩溃
                        // 如果设置圆角失败，下拉列表将使用默认的矩形样式
                    }
                });
        }

        // 自定义绘制下拉列表中的每个项。
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var bounds = e.Bounds;
            bool isActuallySelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected ||
                                      (DroppedDown && animatedIndex == -1 && e.Index == originalSelectedIndex);

            var textFont = DropDownFont ?? Font;
            var textColor = DropDownForeColor;

            var backColor = BackColor;
            if (e.Index == animatedIndex)
            {
                backColor = ColorLerp(BackColor, DropDownHoverColor, hoverProgress);
                textColor = ColorLerp(DropDownForeColor, DropDownHoverForeColor, hoverProgress);
            }
            else if (e.Index == previousAnimatedIndex)
            {
                backColor = ColorLerp(DropDownHoverColor, BackColor, hoverProgress);
                textColor = ColorLerp(DropDownHoverForeColor, DropDownForeColor, hoverProgress);
            }
            else if (isActuallySelected)
            {
                backColor = DropDownSelectedColor;
                textColor = DropDownSelectedForeColor;
            }

            using (var backBrush = new SolidBrush(backColor))
            {
                Rectangle highlightBounds = new(bounds.X + 2, bounds.Y + 2, bounds.Width - 4, bounds.Height - 4);
                using var path = DarkModeHelper.CreateRoundedRectanglePath(highlightBounds, 4);
                e.Graphics.FillPath(backBrush, path);
            }

            var text = GetItemText(Items[e.Index]);
            Rectangle textBounds = new(bounds.Left + TextPadding, bounds.Top, bounds.Width - TextPadding * 2, bounds.Height);
            TextRenderer.DrawText(e.Graphics, text, textFont, textBounds, textColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }

        #region Win32

        // 检索光标在屏幕坐标中的位置。
        // lpPoint: 指向 POINT 结构的指针，该结构接收光标的屏幕坐标。
        // returns: 如果函数成功，则返回非零值。
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetCursorPos(out POINT lpPoint);

        // 定义一个点的 x 和 y 坐标。
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        // 定义一个矩形的左上角和右下角的坐标。
        [StructLayout(LayoutKind.Sequential)]
        internal struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // 包含有关组合框的信息。
        [StructLayout(LayoutKind.Sequential)]
        internal struct COMBOBOXINFO
        {
            public int cbSize;
            public RECT rcItem;
            public RECT rcButton;
            public int stateButton;
            public IntPtr hwndCombo;
            public IntPtr hwndItem;
            public IntPtr hwndList;
        }

        #endregion




        private static Color ColorLerp(Color c1, Color c2, float t) => Color.FromArgb(
            (int)(c1.A + (c2.A - c1.A) * t), (int)(c1.R + (c2.R - c1.R) * t),
            (int)(c1.G + (c2.G - c1.G) * t), (int)(c1.B + (c2.B - c1.B) * t));

        // 当系统主题（深色/浅色模式）更改时，更新控件的颜色。
        public void UpdateColors()
        {
            if (IsDisposed) return;

            SafeInvoke(() =>
            {
                BackColor = DarkModeHelper.ComboBoxBack;
                ForeColor = DarkModeHelper.ComboBoxFore;
                DropDownForeColor = DarkModeHelper.IsDarkTheme ? Color.White : Color.Black;
                currentBorder = targetBorder = DarkModeHelper.ComboBoxBorder;
                ArrowColor = DarkModeHelper.ComboBoxArrow;
                DropDownHoverColor = DarkModeHelper.ComboBoxHover;
                DropDownSelectedColor = DarkModeHelper.ComboBoxSelected;
                HoverColor = DarkModeHelper.MainColor;
                FocusColor = DarkModeHelper.MainColor;
            });
        }

        // 根据控件的焦点和鼠标悬停状态更新边框的外观。
        private void UpdateState()
        {
            bool hover = mouseOverDropDown || ClientRectangle.Contains(PointToClient(MousePosition));
            targetBorder = focused ? FocusColor : (hover ? HoverColor : DarkModeHelper.ComboBoxBorder);
            targetWidth = focused || hover ? 2f : 1.2f;
            Invalidate();
        }

        // 更新下拉按钮的悬停状态，并相应地更改光标。
        private void UpdateDropDownHoverState(Point location)
        {
            bool hover = GetDropDownButtonRect().Contains(location);
            if (mouseOverDropDown == hover) return;
            mouseOverDropDown = hover;
            Cursor = hover ? Cursors.Hand : Cursors.Default;
            UpdateState();
        }

        // 获取下拉按钮的矩形区域。
        private Rectangle GetDropDownButtonRect()
        {
            int w = SystemInformation.HorizontalScrollBarArrowWidth + 8.DpiZoom();
            return new Rectangle(ClientRectangle.Right - w, ClientRectangle.Top, w, ClientRectangle.Height);
        }

        // 根据当前选择的项或文本内容调整控件的宽度。
        private void AdjustWidth()
        {
            if (!AutoSize) return;

            int newWidth;
            if (Items.Count == 0 && string.IsNullOrEmpty(Text))
            {
                newWidth = MinWidth;
            }
            else
            {
                var textToMeasure = string.IsNullOrEmpty(Text) ? GetItemText(SelectedItem) : Text;
                newWidth = TextRenderer.MeasureText(textToMeasure, Font).Width + TextPadding + GetDropDownButtonRect().Width;
            }

            Width = Math.Max(MinWidth, Math.Min(MaxWidth, newWidth));
        }

        // 在控件首次创建时调整宽度。
        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            if (AutoSize) AdjustWidth();
            UpdateColors();
        }

        // 当字体更改时调整宽度。
        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            if (AutoSize) AdjustWidth();
        }

        // 处理主题更改事件，更新控件颜色。
        private void OnThemeChanged(object sender, EventArgs e) => UpdateColors();

        // 释放资源。
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DarkModeHelper.ThemeChanged -= OnThemeChanged;
                animTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        // 自定义绘制控件。
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using var pen = new Pen(currentBorder, borderWidth);
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = DarkModeHelper.CreateRoundedRectanglePath(rect, BorderRadius.DpiZoom());
            e.Graphics.Clear(Parent.BackColor);

            using (var brush = new SolidBrush(BackColor))
            {
                e.Graphics.FillPath(brush, path);
            }

            // 绘制边框
            e.Graphics.DrawPath(pen, path);

            // 绘制文本
            var text = Text;
            if (string.IsNullOrEmpty(text) && SelectedItem != null)
            {
                text = GetItemText(SelectedItem);
            }
            Rectangle textRect = new(TextPadding, 0, Width - GetDropDownButtonRect().Width - TextPadding, Height);
            TextRenderer.DrawText(e.Graphics, text, Font, textRect, ForeColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

            // 绘制箭头
            var btnRect = GetDropDownButtonRect();
            var center = new Point(btnRect.Left + btnRect.Width / 2, btnRect.Top + btnRect.Height / 2);
            int s = 6.DpiZoom();
            using var arrowBrush = new SolidBrush(mouseOverDropDown || focused ? FocusColor : ArrowColor);
            e.Graphics.FillPolygon(arrowBrush, new Point[] {
                new(center.X - s, center.Y - s / 2),
                new(center.X + s, center.Y - s / 2),
                new(center.X, center.Y + s / 2)
            });
        }

        // 在 UI 线程上安全地执行操作。
        private void SafeInvoke(Action action)
        {
            if (IsHandleCreated)
                if (InvokeRequired) Invoke(action);
                else action();
        }
    }

    // 包含 P/Invoke 方法的 RComboBox 类的部分。
    public partial class RComboBox
    {
        // 使指定窗口的整个工作区无效。窗口将在下一次 `WM_PAINT` 消息时重绘。
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, [MarshalAs(UnmanagedType.Bool)] bool bErase);

        // 获取有关指定组合框的信息。
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetComboBoxInfo(IntPtr hwnd, ref COMBOBOXINFO pcbi);

        // 检索指定窗口的边框矩形的尺寸。
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetWindowRect(IntPtr hwnd, ref RECT lpRect);

        // 创建一个具有圆角的矩形区域。
        [LibraryImport("gdi32.dll")]
        internal static partial IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int w, int h);

        // 将窗口的窗口区域设置为特定区域。
        [LibraryImport("user32.dll")]
        internal static partial int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, [MarshalAs(UnmanagedType.Bool)] bool bRedraw);

        // 删除 GDI 对象，释放系统资源。
        [LibraryImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool DeleteObject(IntPtr hObject);
    }
}
