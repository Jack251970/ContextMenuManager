using System.Windows;

namespace ContextMenuManager.Methods
{
    public static class AppMessageBox
    {
        public static MessageBoxResult Show(string text, string caption = null, MessageBoxButton button = MessageBoxButton.OK,
            MessageBoxImage icon = MessageBoxImage.None)
        {
            return DispatcherHelper.Invoke(() =>
            {
                if (icon == MessageBoxImage.None)
                {
                    return iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(text, caption ?? AppString.General.AppName, button);
                }
                else
                {
                    return iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(text, caption ?? AppString.General.AppName, button, icon);
                }
            });
        }
    }
}
