using Microsoft.Win32;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    /// <summary>
    /// 深色模式辅助类 - 集中管理深色模式相关逻辑
    /// </summary>
    public static class DarkModeHelper
    {
        #region Win32 API
        [DllImport("UXTheme.dll", SetLastError = true, EntryPoint = "#138")]
        public static extern bool CheckSystemDarkModeStatus();

        [DllImport("DwmApi")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, int[] attrValue, int attrSize);
        #endregion

        #region 主题事件
        public static event EventHandler ThemeChanged;
        
        // 用于在UI线程上执行操作
        private static SynchronizationContext uiContext;
        #endregion

        #region 颜色定义
        // 主颜色
        public static Color MainColor = Color.FromArgb(255, 143, 31);

        // 窗体颜色
        public static Color TitleArea { get; private set; }
        public static Color FormBack { get; private set; }
        public static Color FormFore { get; private set; }
        public static Color FormBorder { get; private set; }

        // 按钮颜色
        public static Color ButtonMain { get; private set; }
        public static Color ButtonSecond { get; private set; }

        // 侧边栏颜色
        public static Color SideBarBackground { get; private set; }
        public static Color SideBarSeparator { get; private set; }
        public static Color SideBarHovered { get; private set; }

        // 工具栏颜色
        public static Color ToolBarGradientTop { get; private set; }
        public static Color ToolBarGradientMiddle { get; private set; }
        public static Color ToolBarGradientBottom { get; private set; }

        // 状态栏颜色
        public static Color StatusBarGradientTop { get; private set; }
        public static Color StatusBarGradientMiddle { get; private set; }
        public static Color StatusBarGradientBottom { get; private set; }

        // 搜索框/组合框颜色
        public static Color SearchBoxBack { get; private set; }
        public static Color SearchBoxBorder { get; private set; }
        public static Color SearchBoxPlaceholder { get; private set; }
        public static Color ComboBoxBack { get; private set; }
        public static Color ComboBoxFore { get; private set; }
        public static Color ComboBoxBorder { get; private set; }
        public static Color ComboBoxArrow { get; private set; }
        #endregion

        #region 状态管理
        private static bool _isDarkTheme = false;
        public static bool IsDarkTheme => _isDarkTheme;

        // 用于存储当前是否正在监听主题变化
        private static bool _listeningForThemeChanges = false;
        #endregion

        #region 初始化方法
        /// <summary>
        /// 初始化深色模式，应在应用程序启动时调用
        /// </summary>
        public static void Initialize()
        {
            // 保存UI线程上下文
            uiContext = SynchronizationContext.Current;
            if (uiContext == null)
            {
                // 如果没有同步上下文，尝试创建一个（针对WinForms应用）
                try
                {
                    if (Application.MessageLoop)
                    {
                        uiContext = new WindowsFormsSynchronizationContext();
                    }
                }
                catch
                {
                    // 如果无法创建，uiContext将保持null
                }
            }
            
            UpdateTheme();
            StartListeningForThemeChanges();
        }

        /// <summary>
        /// 开始监听系统主题变化
        /// </summary>
        private static void StartListeningForThemeChanges()
        {
            if (!_listeningForThemeChanges)
            {
                SystemEvents.UserPreferenceChanged += OnSystemPreferencesChanged;
                _listeningForThemeChanges = true;
            }
        }

        /// <summary>
        /// 停止监听系统主题变化
        /// </summary>
        public static void StopListening()
        {
            if (_listeningForThemeChanges)
            {
                SystemEvents.UserPreferenceChanged -= OnSystemPreferencesChanged;
                _listeningForThemeChanges = false;
            }
        }
        #endregion

        #region 主题检测与更新
        /// <summary>
        /// 检测当前系统是否使用深色模式
        /// </summary>
        /// <returns>如果是深色模式返回true</returns>
        public static bool IsDarkThemeEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    var value = key?.GetValue("AppsUseLightTheme");
                    return value != null && (int)value == 0;
                }
            }
            catch
            {
                // 如果无法读取注册表，使用备用API或默认浅色模式
                try
                {
                    return CheckSystemDarkModeStatus();
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 更新主题并重新计算所有颜色
        /// </summary>
        /// <returns>如果主题发生变化返回true</returns>
        public static bool UpdateTheme()
        {
            bool newDarkTheme = IsDarkThemeEnabled();
            bool changed = _isDarkTheme != newDarkTheme;
            _isDarkTheme = newDarkTheme;

            UpdateAllColors(_isDarkTheme);
            
            if (changed)
            {
                // 在UI线程上触发事件
                if (uiContext != null)
                {
                    uiContext.Post(_ => 
                    {
                        try
                        {
                            ThemeChanged?.Invoke(null, EventArgs.Empty);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"触发主题变化事件时出错: {ex.Message}");
                        }
                    }, null);
                }
                else
                {
                    try
                    {
                        ThemeChanged?.Invoke(null, EventArgs.Empty);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"触发主题变化事件时出错: {ex.Message}");
                    }
                }
            }

            return changed;
        }

        /// <summary>
        /// 为指定窗体应用深色模式标题栏
        /// </summary>
        /// <param name="form">目标窗体</param>
        public static void ApplyDarkModeToForm(Form form)
        {
            if (_isDarkTheme && form.IsHandleCreated)
            {
                try
                {
                    DwmSetWindowAttribute(form.Handle, 20, new[] { 1 }, 4);
                }
                catch
                {
                    // 如果API调用失败，忽略错误
                }
            }
        }
        #endregion

        #region 颜色计算
        /// <summary>
        /// 根据主题模式更新所有颜色
        /// </summary>
        /// <param name="isDark">是否为深色模式</param>
        private static void UpdateAllColors(bool isDark)
        {
            if (isDark)
            {
                SetDarkModeColors();
            }
            else
            {
                SetLightModeColors();
            }
        }

        /// <summary>
        /// 设置深色模式颜色
        /// </summary>
        private static void SetDarkModeColors()
        {
            // 窗体颜色
            TitleArea = Color.FromArgb(255, 32, 32, 32);
            FormBack = Color.FromArgb(255, 28, 28, 28);
            FormFore = Color.FromArgb(255, 240, 240, 240);
            FormBorder = Color.FromArgb(255, 50, 50, 50);

            // 按钮颜色
            ButtonMain = Color.FromArgb(255, 55, 55, 55);
            ButtonSecond = Color.FromArgb(255, 38, 38, 38);

            // 侧边栏颜色
            SideBarBackground = Color.FromArgb(255, 26, 26, 26);
            SideBarSeparator = Color.FromArgb(255, 64, 64, 64);
            SideBarHovered = Color.FromArgb(255, 51, 51, 51);

            // 工具栏渐变颜色
            ToolBarGradientTop = Color.FromArgb(255, 128, 128, 128);
            ToolBarGradientMiddle = Color.FromArgb(255, 56, 56, 56);
            ToolBarGradientBottom = Color.FromArgb(255, 128, 128, 128);

            // 状态栏渐变颜色
            StatusBarGradientTop = Color.FromArgb(255, 128, 128, 128);
            StatusBarGradientMiddle = Color.FromArgb(255, 56, 56, 56);
            StatusBarGradientBottom = Color.FromArgb(255, 128, 128, 128);

            // 搜索框/组合框颜色
            SearchBoxBack = Color.FromArgb(255, 45, 45, 45);
            SearchBoxBorder = Color.FromArgb(255, 80, 80, 80);
            SearchBoxPlaceholder = Color.FromArgb(255, 150, 150, 150);
            ComboBoxBack = Color.FromArgb(255, 45, 45, 48);
            ComboBoxFore = Color.FromArgb(255, 245, 245, 245);
            ComboBoxBorder = Color.FromArgb(255, 70, 70, 75);
            ComboBoxArrow = Color.FromArgb(255, 200, 200, 200);
        }

        /// <summary>
        /// 设置浅色模式颜色
        /// </summary>
        private static void SetLightModeColors()
        {
            // 窗体颜色
            TitleArea = Color.FromArgb(255, 243, 243, 243);
            FormBack = SystemColors.Control;
            FormFore = SystemColors.ControlText;
            FormBorder = Color.LightGray;

            // 按钮颜色
            ButtonMain = SystemColors.ControlLightLight;
            ButtonSecond = SystemColors.ControlLight;

            // 侧边栏颜色
            SideBarBackground = SystemColors.Control;
            SideBarSeparator = Color.FromArgb(255, 200, 200, 200);
            SideBarHovered = Color.FromArgb(255, 230, 230, 230);

            // 工具栏渐变颜色
            ToolBarGradientTop = Color.FromArgb(255, 255, 255, 255);
            ToolBarGradientMiddle = Color.FromArgb(255, 230, 230, 230);
            ToolBarGradientBottom = Color.FromArgb(255, 255, 255, 255);

            // 状态栏渐变颜色
            StatusBarGradientTop = Color.FromArgb(255, 255, 255, 255);
            StatusBarGradientMiddle = Color.FromArgb(255, 230, 230, 230);
            StatusBarGradientBottom = Color.FromArgb(255, 255, 255, 255);

            // 搜索框/组合框颜色
            SearchBoxBack = Color.White;
            SearchBoxBorder = Color.FromArgb(255, 200, 200, 200);
            SearchBoxPlaceholder = Color.FromArgb(255, 120, 120, 120);
            ComboBoxBack = Color.FromArgb(255, 250, 250, 252);
            ComboBoxFore = Color.FromArgb(255, 25, 25, 25);
            ComboBoxBorder = Color.FromArgb(255, 210, 210, 215);
            ComboBoxArrow = Color.FromArgb(255, 100, 100, 100);
        }

        /// <summary>
        /// 获取边框颜色（根据是否聚焦）
        /// </summary>
        /// <param name="isFocused">控件是否聚焦</param>
        /// <returns>边框颜色</returns>
        public static Color GetBorderColor(bool isFocused = false)
        {
            if (isFocused)
            {
                return MainColor;
            }
            else
            {
                return IsDarkTheme ? 
                    Color.FromArgb(255, 80, 80, 80) : 
                    Color.FromArgb(255, 200, 200, 200);
            }
        }

        /// <summary>
        /// 获取占位符文本颜色
        /// </summary>
        /// <returns>占位符颜色</returns>
        public static Color GetPlaceholderColor()
        {
            return IsDarkTheme ? 
                Color.FromArgb(255, 150, 150, 150) : 
                Color.FromArgb(255, 120, 120, 120);
        }
        #endregion

        #region 事件处理
        /// <summary>
        /// 系统偏好设置变化事件处理
        /// </summary>
        private static void OnSystemPreferencesChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                // 在UI线程上更新主题
                if (uiContext != null)
                {
                    uiContext.Post(_ => 
                    {
                        try
                        {
                            UpdateTheme();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"更新主题时出错: {ex.Message}");
                        }
                    }, null);
                }
                else
                {
                    try
                    {
                        UpdateTheme();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"更新主题时出错: {ex.Message}");
                    }
                }
            }
        }
        #endregion

        #region 工具方法
        /// <summary>
        /// 调整控件的颜色以适应主题
        /// </summary>
        /// <param name="control">要调整的控件</param>
        public static void AdjustControlColors(Control control)
        {
            if (control == null || control.IsDisposed) return;

            // 确保在UI线程上执行
            if (control.InvokeRequired)
            {
                try
                {
                    control.Invoke(new Action(() => AdjustControlColors(control)));
                }
                catch (ObjectDisposedException)
                {
                    // 控件已释放，忽略
                    return;
                }
                catch (InvalidOperationException)
                {
                    // 句柄未创建或已销毁，忽略
                    return;
                }
                return;
            }

            // 检查控件是否已释放
            if (control.IsDisposed) return;

            // 递归调整所有子控件
            foreach (Control child in control.Controls)
            {
                AdjustControlColors(child);
            }

            // 根据控件类型调整颜色
            try
            {
                // 这里需要根据你的实际控件类型进行调整
                // 使用字符串类型名称检查以避免编译依赖
                string typeName = control.GetType().FullName;
                
                if (typeName == "BluePointLilac.Controls.MyListBox")
                {
                    control.BackColor = FormBack;
                    control.ForeColor = FormFore;
                }
                else if (typeName == "BluePointLilac.Controls.MyListItem")
                {
                    control.BackColor = FormBack;
                    control.ForeColor = FormFore;
                }
                else if (typeName == "BluePointLilac.Controls.MyToolBar")
                {
                    control.BackColor = TitleArea;
                    control.ForeColor = FormFore;
                }
                else if (typeName == "BluePointLilac.Controls.MyToolBarButton")
                {
                    control.ForeColor = FormFore;
                }
                else if (typeName == "BluePointLilac.Controls.MySideBar")
                {
                    control.BackColor = ButtonSecond;
                    control.ForeColor = FormFore;
                }
                else if (typeName == "BluePointLilac.Controls.MyStatusBar")
                {
                    control.BackColor = ButtonMain;
                    control.ForeColor = FormFore;
                }
                else if (control is RComboBox combo)
                {
                    // 对于RComboBox，使用其自己的主题更新方法
                    if (combo.InvokeRequired)
                    {
                        combo.Invoke(new Action(() => combo.UpdateColors()));
                    }
                    else
                    {
                        combo.UpdateColors();
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不要抛出，避免影响其他控件
                System.Diagnostics.Debug.WriteLine($"调整控件颜色时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建圆角矩形路径
        /// </summary>
        /// <param name="rect">矩形区域</param>
        /// <param name="radius">圆角半径</param>
        /// <returns>圆角矩形路径</returns>
        public static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            
            if (radius <= 0) 
            {
                path.AddRectangle(rect);
                return path;
            }
            
            int diameter = radius * 2;
            var arc = new Rectangle(rect.Location, new Size(diameter, diameter));
            
            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            
            path.CloseFigure();
            return path;
        }
        #endregion
    }
}