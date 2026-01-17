using ContextMenuManager.Methods;

namespace ContextMenuManager.Controls.Interfaces
{
    internal interface ITsiRestoreItem
    {
        DeleteMeMenuItem TsiDeleteMe { get; set; }
        void RestoreMe();
    }

    internal sealed class RestoreMeMenuItem : RToolStripMenuItem
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
