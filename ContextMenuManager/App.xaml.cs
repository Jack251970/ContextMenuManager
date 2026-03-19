using iNKORE.UI.WPF.Modern;
using System.Windows;

namespace ContextMenuManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Follow the system theme (light/dark)
            ThemeManager.Current.ApplicationTheme = null;
        }
    }
}
