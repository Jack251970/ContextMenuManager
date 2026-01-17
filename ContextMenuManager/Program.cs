using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System;
using System.Threading;
using System.Windows.Forms;

namespace ContextMenuManager
{
    //兼容.Net3.5和.Net4.0，兼容Vista - Win11
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // 首先设置应用程序属性
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 然后设置Windows Forms同步上下文
            if (SynchronizationContext.Current == null)
            {
                SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
            }

            // 最后初始化DarkModeHelper（这时可能会有控件创建）
            DarkModeHelper.Initialize();

            if (SingleInstance.IsRunning()) return;
            AppString.LoadStrings();
            Updater.PeriodicUpdate();
            XmlDicHelper.ReloadDics();
            Application.Run(new MainForm());
        }
    }
}