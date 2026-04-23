using ContextMenuManager.Methods;
using System;
using System.Windows.Controls;

namespace ContextMenuManager.Controls
{
    internal class SwitchDicList : MyList // 其他菜单 增强菜单
    {
        public bool UseUserDic { get; set; }

        public virtual void LoadItems()
        {
            AddSwitchItem();
        }

        public void AddSwitchItem()
        {
            var item = new SwitchDicItem(this) { UseUserDic = UseUserDic };
            item.UseDicChanged += () =>
            {
                UseUserDic = item.UseUserDic;
                ClearItems();
                LoadItems();
            };
            AddItem(item);
        }
    }

    internal sealed class SwitchDicItem : MyListItem
    {
        public SwitchDicItem(MyList list) : base(list)
        {
            if (list != null)
            {
                cmbDic = new()
                {
                    Width = 120
                };

                Text = AppString.Other.SwitchDictionaries;
                AddCtr(cmbDic);

                cmbDic.Items.Add(AppString.Other.WebDictionaries);
                cmbDic.Items.Add(AppString.Other.UserDictionaries);

                cmbDic.SelectionChanged += (sender, e) =>
                {
                    Control.Focus();
                    UseUserDic = cmbDic.SelectedIndex == 1;
                };
            }
        }

        private bool? useUserDic = null;
        public bool UseUserDic
        {
            get => useUserDic == true;
            set
            {
                if (useUserDic == value) return;
                var flag = useUserDic == null;
                useUserDic = value;
                Glyph = UseUserDic ? AppGlyphs.User : AppGlyphs.Web;
                cmbDic?.SelectedIndex = value ? 1 : 0;
                if (!flag) UseDicChanged?.Invoke();
            }
        }

        public Action UseDicChanged;

        private readonly ComboBox cmbDic;
    }
}