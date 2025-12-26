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
    sealed class OpenWithList : MyList // 主页 打开方式
    {
        public void LoadItems()
        {
            LoadOpenWithItems();
            SortItemByText();
            AddNewItem();
            //Win8及以上版本系统才有在应用商店中查找应用
            if (WinOsVersion.Current >= WinOsVersion.Win8)
            {
                VisibleRegRuleItem storeItem = new VisibleRegRuleItem(VisibleRegRuleItem.UseStoreOpenWith);
                InsertItem(storeItem, 1);
            }
        }

        private void LoadOpenWithItems()
        {
            using(RegistryKey root = Registry.ClassesRoot)
            using(RegistryKey appKey = root.OpenSubKey("Applications"))
            {
                foreach(string appName in appKey.GetSubKeyNames())
                {
                    if(!appName.Contains('.')) continue;//需要为有扩展名的文件名
                    using(RegistryKey shellKey = appKey.OpenSubKey($@"{appName}\shell"))
                    {
                        if(shellKey == null) continue;

                        List<string> names = shellKey.GetSubKeyNames().ToList();
                        if(names.Contains("open", StringComparer.OrdinalIgnoreCase)) names.Insert(0, "open");

                        string keyName = names.Find(name =>
                        {
                            using(RegistryKey cmdKey = shellKey.OpenSubKey(name))
                                return cmdKey.GetValue("NeverDefault") == null;
                        });
                        if(keyName == null) continue;

                        using(RegistryKey commandKey = shellKey.OpenSubKey($@"{keyName}\command"))
                        {
                            string command = commandKey?.GetValue("")?.ToString();
                            if(ObjectPath.ExtractFilePath(command) != null)
                            {
                                OpenWithItem item = new OpenWithItem(commandKey.Name);
                                AddItem(item);
                            } 
                        }
                    }
                }
            }
        }

        private void AddNewItem()
        {
            NewItem newItem = new NewItem();
            InsertItem(newItem, 0);
            newItem.AddNewItem += () =>
            {
                using(NewOpenWithDialog dlg = new NewOpenWithDialog())
                {
                    if(dlg.ShowDialog() == DialogResult.OK)
                        InsertItem(new OpenWithItem(dlg.RegPath), 2);
                }
            };
        }
    }
}