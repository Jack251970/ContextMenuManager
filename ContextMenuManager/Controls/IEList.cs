using BluePointLilac.Controls;
using BluePointLilac.Methods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    internal sealed class IEList : MyList // 其他规则 IE浏览器
    {
        public const string IEPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Internet Explorer";

        public void LoadItems()
        {
            AddNewItem();
            LoadIEItems();
        }

        private void LoadIEItems()
        {
            var names = new List<string>();
            using var ieKey = RegistryEx.GetRegistryKey(IEPath);
            if (ieKey == null) return;
            foreach (var part in IEItem.MeParts)
            {
                using var meKey = ieKey.OpenSubKey(part);
                if (meKey == null) continue;
                foreach (var keyName in meKey.GetSubKeyNames())
                {
                    if (names.Contains(keyName, StringComparer.OrdinalIgnoreCase)) continue;
                    using var key = meKey.OpenSubKey(keyName);
                    if (!string.IsNullOrEmpty(key.GetValue("")?.ToString()))
                    {
                        AddItem(new IEItem(key.Name));
                        names.Add(keyName);
                    }
                }
            }
        }

        private void AddNewItem()
        {
            var newItem = new NewItem();
            AddItem(newItem);
            newItem.AddNewItem += () =>
            {
                using var dlg = new NewIEDialog();
                if (dlg.ShowDialog() != DialogResult.OK) return;
                InsertItem(new IEItem(dlg.RegPath), 1);
            };
        }
    }
}