using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System;
using System.IO;
using System.Windows.Forms;

namespace ContextMenuManager.Controls.Interfaces
{
    internal interface ITsiDeleteItem
    {
        DeleteMeMenuItem TsiDeleteMe { get; set; }
        void DeleteMe();
    }

    internal interface ITsiRegDeleteItem : ITsiDeleteItem
    {
        string Text { get; }
        string RegPath { get; }
    }

    internal sealed class DeleteMeMenuItem : RToolStripMenuItem
    {
        public DeleteMeMenuItem(ITsiDeleteItem item) : base(item is RestoreItem ? AppString.Menu.DeleteBackup : AppString.Menu.Delete)
        {
            Click += (sender, e) =>
            {
                if (item is ITsiRegDeleteItem regItem && AppConfig.AutoBackup)
                {
                    if (AppMessageBox.Show(AppString.Message.DeleteButCanRestore, MessageBoxButtons.YesNo) != DialogResult.Yes)
                    {
                        return;
                    }
                    var date = DateTime.Today.ToString("yyyy-MM-dd");
                    var time = DateTime.Now.ToString("HH-mm-ss");
                    var filePath = $@"{AppConfig.RegBackupDir}\{date}\{regItem.Text} - {time}.reg";
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    ExternalProgram.ExportRegistry(regItem.RegPath, filePath);
                }
                else if (AppMessageBox.Show(item is RestoreItem ? AppString.Message.ConfirmDeleteBackupPermanently : AppString.Message.ConfirmDeletePermanently,
                    MessageBoxButtons.YesNo) != DialogResult.Yes)
                {
                    return;
                }
                var listItem = (MyListItem)item;
                var list = (MyList)listItem.Parent;
                var index = list.GetItemIndex(listItem);
                try
                {
                    item.DeleteMe();
                }
                catch
                {
                    AppMessageBox.Show(AppString.Message.AuthorityProtection);
                    return;
                }
                list.Controls.Remove(listItem);
                list.Controls[index < list.Controls.Count ? index : (list.Controls.Count - 1)].Focus();
                listItem.Dispose();
            };
        }
    }
}