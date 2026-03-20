using System.Windows.Forms;

namespace ContextMenuManager.Methods
{
    public static class AppMessageBox
    {
        public static DialogResult Show(string text, MessageBoxButtons buttons = MessageBoxButtons.OK,
            MessageBoxIcon icon = MessageBoxIcon.Warning, string caption = null)
        {
            var icon1 = icon switch
            {
                MessageBoxIcon.Error or MessageBoxIcon.Stop or MessageBoxIcon.Hand => System.Windows.MessageBoxImage.Error,
                MessageBoxIcon.Warning or MessageBoxIcon.Exclamation => System.Windows.MessageBoxImage.Warning,
                MessageBoxIcon.Question => System.Windows.MessageBoxImage.Question,
                MessageBoxIcon.Information or MessageBoxIcon.Asterisk => System.Windows.MessageBoxImage.Information,
                _ => System.Windows.MessageBoxImage.None
            };
            var buttons1 = buttons switch
            {
                MessageBoxButtons.OK => System.Windows.MessageBoxButton.OK,
                MessageBoxButtons.OKCancel => System.Windows.MessageBoxButton.OKCancel,
                MessageBoxButtons.YesNo => System.Windows.MessageBoxButton.YesNo,
                MessageBoxButtons.YesNoCancel => System.Windows.MessageBoxButton.YesNoCancel,
                _ => System.Windows.MessageBoxButton.OK
            };
            return iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(text, caption ?? AppString.General.AppName, buttons1, icon1) switch
            {
                System.Windows.MessageBoxResult.OK => DialogResult.OK,
                System.Windows.MessageBoxResult.Cancel => DialogResult.Cancel,
                System.Windows.MessageBoxResult.Yes => DialogResult.Yes,
                System.Windows.MessageBoxResult.No => DialogResult.No,
                System.Windows.MessageBoxResult.None => DialogResult.None,
                _ => DialogResult.None
            };
        }
    }
}
