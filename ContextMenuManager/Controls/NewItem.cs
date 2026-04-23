using ContextMenuManager.Methods;
using System;

namespace ContextMenuManager.Controls
{
    internal class NewItem : MyListItem
    {
        public NewItem(MyList list) : this(list, AppString.Other.NewItem) { }

        public NewItem(MyList list, string text) : base(list)
        {
            if (list != null)
            {
                Text = text;
                Glyph = AppGlyphs.NewItem;
                AddCtr(BtnAddNewItem);
                ToolTipBox.SetToolTip(BtnAddNewItem, text);
                BtnAddNewItem.Click += (sender, e) => AddNewItem?.Invoke();
                Control.MouseDoubleClick += (sender, e) => AddNewItem?.Invoke();
            }
        }

        public Action AddNewItem;
        private readonly GlyphButton BtnAddNewItem = new(AppGlyphs.Add);
    }
}
