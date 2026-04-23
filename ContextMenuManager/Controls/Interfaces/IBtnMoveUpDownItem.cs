using ContextMenuManager.Methods;

namespace ContextMenuManager.Controls.Interfaces
{
    internal interface IBtnMoveUpDownItem
    {
        MoveButton BtnMoveUp { get; set; }
        MoveButton BtnMoveDown { get; set; }
    }

    internal sealed class MoveButton : GlyphButton
    {
        public MoveButton(IBtnMoveUpDownItem item, bool isUp) : base(isUp ? AppGlyphs.Up : AppGlyphs.Down)
        {
            ((MyListItem)item).AddCtr(this);
        }
    }
}