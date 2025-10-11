using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    public abstract class SearchableListBase : UserControl
    {
        public virtual void SearchItems(string searchText)
        {
            // 默认实现：搜索所有 MyListItem 控件
            var items = GetAllItems();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                // 清空搜索，显示所有项
                foreach (var item in items)
                {
                    item.Visible = true;
                    if (item is BluePointLilac.Controls.MyListItem myItem)
                    {
                        myItem.HighlightText = null;
                    }
                }
                return;
            }

            // 搜索所有列表项
            foreach (var item in items)
            {
                bool matches = item.Text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                item.Visible = matches;

                if (item is BluePointLilac.Controls.MyListItem myItem)
                {
                    if (matches)
                    {
                        myItem.HighlightText = searchText;
                    }
                    else
                    {
                        myItem.HighlightText = null;
                    }
                }
            }
        }

        public virtual IEnumerable<Control> GetAllItems()
        {
            // 默认实现：返回所有子控件
            return Controls.Cast<Control>();
        }
    }
}