using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using System.Windows;

namespace ContextMenuManager.Controls.Interfaces
{
    internal interface IBtnShowMenuItem
    {
        MenuFlyout Flyout { get; set; }
        MenuButton BtnShowMenu { get; set; }
    }

    internal sealed class MenuButton : PictureButton
    {
        public MenuButton(IBtnShowMenuItem item) : base(AppImage.Setting)
        {
            item.Flyout = new MenuFlyout();
            var listItem = (MyListItem)item;
            listItem.AddCtr(this);

            Click += (sender, e) =>
            {
                item.Flyout?.ShowAt(this);
            };

            listItem.Control.MouseRightButtonUp += (sender, e) =>
            {
                item.Flyout?.ShowAt(listItem.Control);
                e.Handled = true;
            };
        }
    }
}
