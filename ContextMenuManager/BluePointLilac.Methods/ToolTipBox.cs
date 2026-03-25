using System.Windows;

namespace ContextMenuManager.Methods
{
    public static class ToolTipBox
    {
        public static void SetToolTip(FrameworkElement element, string tip)
        {
            if (tip.IsNullOrWhiteSpace()) return;
            element.ToolTip = tip;
        }
    }
}
