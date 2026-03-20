using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Windows.Forms.Integration;

namespace ContextMenuManager.Controls
{
    internal sealed class DetailedEditDialog
    {
        public Guid GroupGuid { get; set; }

        public bool ShowDialog() => RunDialog(null);

        public bool RunDialog(MainWindow owner)
        {
            var dialog = ContentDialogHost.CreateDialog(
                AppString.Dialog.DetailedEdit.Replace("%s", GuidInfo.GetText(GroupGuid)), 
                owner);
            dialog.CloseButtonText = ResourceString.OK;
            dialog.FullSizeDesired = true;

            var list = new DetailedEditList
            {
                GroupGuid = GroupGuid,
                UseUserDic = XmlDicHelper.DetailedEditGuidDic[GroupGuid],
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