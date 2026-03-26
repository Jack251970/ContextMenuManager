using ContextMenuManager.Methods;
using System.Windows.Controls;

namespace ContextMenuManager.Controls.Interfaces
{
    internal interface IBtnShowMenuItem
    {
        ContextMenu ContextMenu { get; set; }
        MenuButton BtnShowMenu { get; set; }
    }

    internal sealed class MenuButton : PictureButton
    {
        public MenuButton(IBtnShowMenuItem item) : base(AppImage.Setting)
        {
            item.ContextMenu = new ContextMenu();
            ((MyListItem)item).AddCtr(this);

            Click += (sender, e) =>
            {
                if (item.ContextMenu != null)
                {
                    item.ContextMenu.PlacementTarget = this;
                    item.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                    item.ContextMenu.IsOpen = true;
                }
            };
        }
    }
}
