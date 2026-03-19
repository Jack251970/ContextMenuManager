using iNKORE.UI.WPF.Modern.Common;
using System.Windows;

namespace ContextMenuManager
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            ShadowAssist.UseBitmapCache = false;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
        }
    }
}
