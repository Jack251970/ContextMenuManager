using ContextMenuManager.Methods;
using System.Windows.Forms;

namespace ContextMenuManager.Controls.Interfaces
{
    interface ITsiRestoreItem
    {
        DeleteMeMenuItem TsiDeleteMe { get; set; }
        void RestoreMe();
    }

    sealed class RestoreMeMenuItem : RToolStripMenuItem
    {
        public RestoreMeMenuItem(ITsiRestoreItem item) : base(AppString.Menu.RestoreBackup)
        {
            Click += (sender, e) =>
            {
                item.RestoreMe();
            };
        }
    }
}
