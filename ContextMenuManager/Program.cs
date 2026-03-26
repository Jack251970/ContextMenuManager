using ContextMenuManager.Methods;
using System;

namespace ContextMenuManager
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // 启动WPF应用
            if (SingleInstance<App>.InitializeAsFirstInstance())
            {
                // 初始化字符串、更新检查和XML字典
                AppString.LoadStrings();
                Updater.PeriodicUpdate();
                XmlDicHelper.ReloadDics();

                using var application = new App();
                application.InitializeComponent();
                application.Run();
            }
        }
    }
}
