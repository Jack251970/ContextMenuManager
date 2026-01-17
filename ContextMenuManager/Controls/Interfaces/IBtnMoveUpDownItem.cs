using BluePointLilac.Controls;
using ContextMenuManager.Methods;

namespace ContextMenuManager.Controls.Interfaces
{
    internal interface IBtnMoveUpDownItem
    {
        MoveButton BtnMoveUp { get; set; }
        MoveButton BtnMoveDown { get; set; }
    }

    internal sealed class MoveButton : PictureButton
    {
        public MoveButton(IBtnMoveUpDownItem item, bool isUp) : base(isUp ? AppImage.Up : AppImage.Down)
        {
            ((MyListItem)item).AddCtr(this);
        }
    }
}