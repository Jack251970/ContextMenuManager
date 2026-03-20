using ContextMenuManager.Methods;
using System.Drawing;
using System.Windows.Forms;

namespace ContextMenuManager.Controls.Interfaces
{
    internal interface ITsiIconItem
    {
        ChangeIconMenuItem TsiChangeIcon { get; set; }
        string IconLocation { get; set; }
        string IconPath { get; set; }
        int IconIndex { get; set; }
        Image Image { get; set; }
        Icon ItemIcon { get; }
    }

    internal sealed class ChangeIconMenuItem : RToolStripMenuItem
    {
        public ChangeIconMenuItem(ITsiIconItem item) : base(AppString.Menu.ChangeIcon)
        {
            Click += (sender, e) =>
            {
                var dlg = new IconDialog();
                dlg.IconPath = item.IconPath;
                dlg.IconIndex = item.IconIndex;
                if (dlg.ShowDialog() != true) return;
                using var icon = ResourceIcon.GetIcon(dlg.IconPath, dlg.IconIndex);
                Image image = icon?.ToBitmap();
                if (image == null) return;
                item.Image = image;
                item.IconPath = dlg.IconPath;
                item.IconIndex = dlg.IconIndex;
                item.IconLocation = $"{dlg.IconPath},{dlg.IconIndex}";
            };
        }
    }
}