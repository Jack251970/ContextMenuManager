using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System;
using System.Collections.Generic;

namespace ContextMenuManager.Controls
{
    sealed class FileExtensionDialog : SelectDialog
    {
        public string Extension
        {
            get => Selected.Trim();
            set => Selected = value?.Trim();
        }

        public FileExtensionDialog()
        {
            CanEdit = true;
            Title = AppString.Dialog.SelectExtension;
            Items = FileExtensionItems.ToArray();
        }

        public static List<string> FileExtensionItems
        {
            get
            {
                List<string> items = new List<string>();
                using (var key = RegistryEx.GetRegistryKey(FileExtension.FILEEXTSPATH))
                {
                    if (key != null)
                    {
                        foreach (string keyName in key.GetSubKeyNames())
                        {
                            if (keyName.StartsWith(".")) items.Add(keyName.Substring(1));
                        }
                    }
                }
                return items;
            }
        }

        protected override bool RunDialog(IntPtr hwndOwner)
        {
            bool flag = base.RunDialog(hwndOwner);
            if(flag)
            {
                string extension = ObjectPath.RemoveIllegalChars(Extension);
                int index = extension.LastIndexOf('.');
                if(index >= 0) Extension = extension.Substring(index);
                else Extension = $".{extension}";
            }
            return flag;
        }
    }
}
