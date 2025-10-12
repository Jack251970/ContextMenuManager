using System.Reflection;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    // 扩展方法，用于安全地设置控件样式
    public static class ControlExtensions
    {
        public static void SetStyle(this Control control, ControlStyles flag, bool value)
        {
            try
            {
                // 使用反射调用 SetStyle 方法
                MethodInfo method = typeof(Control).GetMethod("SetStyle",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (method != null)
                {
                    method.Invoke(control, new object[] { flag, value });
                }
            }
            catch
            {
                // 如果反射失败，忽略错误
            }
        }
    }
}