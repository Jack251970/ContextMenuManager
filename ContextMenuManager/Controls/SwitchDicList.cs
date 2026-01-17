using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.BluePointLilac.Controls;
using ContextMenuManager.Methods;
using System;
using System.Drawing;
using System.Windows.Forms;

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
            var item = new SwitchDicItem { UseUserDic = UseUserDic };
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
        public SwitchDicItem()
        {
            Text = AppString.Other.SwitchDictionaries;
            AddCtr(cmbDic);
            cmbDic.AutosizeDropDownWidth();
            cmbDic.Font = new Font(Font.FontFamily, Font.Size + 1F);
            cmbDic.Items.AddRange(new[] { AppString.Other.WebDictionaries, AppString.Other.UserDictionaries });
            cmbDic.SelectionChangeCommitted += (sender, e) =>
            {
                Focus();
                UseUserDic = cmbDic.SelectedIndex == 1;
            };
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
                Image = UseUserDic ? AppImage.User : AppImage.Web;
                cmbDic.SelectedIndex = value ? 1 : 0;
                if (!flag) UseDicChanged?.Invoke();
            }
        }

        public Action UseDicChanged;

        private readonly RComboBox cmbDic = new()
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 120.DpiZoom()
        };
    }
}