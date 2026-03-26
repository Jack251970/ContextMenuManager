using ContextMenuManager.Methods;

namespace ContextMenuManager.Controls
{
    internal sealed class EnhanceMenusDialog
    {
        public string ScenePath { get; set; }

        public bool ShowDialog()
        {
            return RunDialog(null);
        }

        public bool RunDialog(MainWindow owner)
        {
            var dialog = ContentDialogHost.CreateDialog(AppString.SideBar.EnhanceMenu, owner);

            var list = new EnhanceMenuList
            {
                ScenePath = ScenePath,
                UseUserDic = XmlDicHelper.EnhanceMenuPathDic[ScenePath],
                MinWidth = 500,
                MinHeight = 400
            };
            list.LoadItems();

            dialog.Content = list;
            ContentDialogHost.RunBlocking(dialog.ShowAsync, owner);
            return false;
        }
    }
}
