using ContextMenuManager.Methods;
using System.Collections.Generic;

namespace ContextMenuManager.Controls
{
    internal sealed class FileExtensionDialog : SelectDialog
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
                var items = new List<string>();
                using (var key = RegistryEx.GetRegistryKey(FileExtension.FILEEXTSPATH))
                {
                    if (key != null)
                    {
                        foreach (var keyName in key.GetSubKeyNames())
                        {
                            if (keyName.StartsWith(".")) items.Add(keyName[1..]);
                        }
                    }
                }
                return items;
            }
        }

        public new bool ShowDialog()
        {
            return RunDialog(null);
        }

        public new bool RunDialog(MainWindow owner)
        {
            var flag = base.RunDialog(owner);
            if (flag)
            {
                var extension = ObjectPath.RemoveIllegalChars(Extension);
                var index = extension.LastIndexOf('.');
                if (index >= 0) Extension = extension[index..];
                else Extension = $".{extension}";
            }
            return flag;
        }
    }
}
