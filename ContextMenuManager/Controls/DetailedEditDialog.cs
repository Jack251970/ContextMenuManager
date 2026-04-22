using ContextMenuManager.Methods;
using System;

namespace ContextMenuManager.Controls
{
    internal sealed class DetailedEditDialog
    {
        public Guid GroupGuid { get; set; }

        public bool ShowDialog()
        {
            return RunDialog(null);
        }

        public bool RunDialog(MainWindow owner)
        {
            var dialog = ContentDialogHost.CreateDialog(
                AppString.Dialog.DetailedEdit.Replace("%s", ResourceString.StripMnemonics(GuidInfo.GetText(GroupGuid))),
                owner);

            var list = new DetailedEditList
            {
                GroupGuid = GroupGuid,
                UseUserDic = XmlDicHelper.DetailedEditGuidDic[GroupGuid],
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
