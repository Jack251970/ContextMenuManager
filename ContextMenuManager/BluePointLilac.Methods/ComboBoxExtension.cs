using BluePointLilac.Controls;
using System;
using ContextMenuManager.BluePointLilac.Controls;
using System.Windows.Forms;

namespace BluePointLilac.Methods
{
    internal static class ComboBoxExtension
    {
        public static void AutosizeDropDownWidth(this RComboBox cmb)
        {
            cmb.DropDown += (sender, e) =>
            {
                int maxWidth = 0;
                foreach(var item in cmb.Items)
                {
                    maxWidth = Math.Max(maxWidth, TextRenderer.MeasureText(item.ToString(), cmb.Font).Width);
                }
                maxWidth = Math.Max(maxWidth, cmb.Width);
                cmb.DropDownWidth = maxWidth;
            };
        }
    }
}