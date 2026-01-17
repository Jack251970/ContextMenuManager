using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    internal sealed class GuidBlockedList : MyList // 其他规则 GUID锁
    {
        public const string HKLMBLOCKED = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked";
        public const string HKCUBLOCKED = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked";
        public static readonly string[] BlockedPaths = { HKLMBLOCKED, HKCUBLOCKED };

        public void LoadItems()
        {
            AddNewItem();
            LoadBlockedItems();
        }

        private void LoadBlockedItems()
        {
            var values = new List<string>();
            foreach (var path in BlockedPaths)
            {
                using var key = RegistryEx.GetRegistryKey(path);
                if (key == null) continue;
                foreach (var value in key.GetValueNames())
                {
                    if (values.Contains(value, StringComparer.OrdinalIgnoreCase)) continue;
                    AddItem(new GuidBlockedItem(value));
                    values.Add(value);
                }
            }
        }

        private void AddNewItem()
        {
            var newItem = new NewItem(AppString.Other.AddGuidBlockedItem);
            AddItem(newItem);
            newItem.AddNewItem += () =>
            {
                using var dlg = new InputDialog { Title = AppString.Dialog.InputGuid };
                if (GuidEx.TryParse(Clipboard.GetText(), out var guid)) dlg.Text = guid.ToString();
                if (dlg.ShowDialog() != DialogResult.OK) return;
                if (GuidEx.TryParse(dlg.Text, out guid))
                {
                    var value = guid.ToString("B");
                    Array.ForEach(BlockedPaths, path => Registry.SetValue(path, value, ""));
                    for (var i = 1; i < Controls.Count; i++)
                    {
                        if (((GuidBlockedItem)Controls[i]).Guid.Equals(guid))
                        {
                            AppMessageBox.Show(AppString.Message.HasBeenAdded);
                            return;
                        }
                    }
                    InsertItem(new GuidBlockedItem(value), 1);
                    ExplorerRestarter.Show();
                }
                else AppMessageBox.Show(AppString.Message.MalformedGuid);
            };
        }
    }
}