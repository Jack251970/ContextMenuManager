using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using System.Windows.Forms.Integration;

namespace ContextMenuManager.Controls
{
    internal sealed class EnhanceMenusDialog
    {
        public string ScenePath { get; set; }

        public bool ShowDialog() => RunDialog(null);

        public bool RunDialog(MainWindow owner)
        {
            var dialog = ContentDialogHost.CreateDialog(AppString.SideBar.EnhanceMenu, owner);
            dialog.CloseButtonText = ResourceString.OK;
            dialog.FullSizeDesired = true;

            var list = new EnhanceMenuList
            {
                ScenePath = ScenePath,
                UseUserDic = XmlDicHelper.EnhanceMenuPathDic[ScenePath],
                Dock = System.Windows.Forms.DockStyle.Fill
            };
            list.LoadItems();

            var host = new WindowsFormsHost
            {
                Child = new System.Windows.Forms.Panel
                {
                    Controls = { list },
                    Height = 400,
                    Width = 600
                },
                Height = 400,
                Width = 600
            };

            dialog.Content = host;
            ContentDialogHost.RunBlocking(dialog.ShowAsync, owner);
            return false;
        }
    }
}