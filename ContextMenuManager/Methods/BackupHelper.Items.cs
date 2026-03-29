using ContextMenuManager.Controls;
using ContextMenuManager.Controls.Interfaces;
using Microsoft.Win32;
using System;
using static ContextMenuManager.Controls.ShellList;
using static ContextMenuManager.Methods.BackupList;

namespace ContextMenuManager.Methods
{
    internal sealed partial class BackupHelper
    {
        /*******************************单个Item处理************************************/

        private void BackupRestoreItem(MyListItem item, string itemName, string keyName, BackupItemType backupItemType, bool itemData, Scenes currentScene)
        {
            if (backup)
            {
                // 加入备份列表
                switch (backupMode)
                {
                    case BackupMode.All:
                    default:
                        AddItem(keyName, backupItemType, itemData, currentScene);
                        break;
                    case BackupMode.OnlyVisible:
                        if (itemData) AddItem(keyName, backupItemType, itemData, currentScene);
                        break;
                    case BackupMode.OnlyInvisible:
                        if (!itemData) AddItem(keyName, backupItemType, itemData, currentScene);
                        break;
                }
            }
            else
            {
                // 恢复备份列表（新增备份类别处4）
                if (CheckItemNeedChange(itemName, keyName, backupItemType, itemData))
                {
                    if (item is IChkVisibleItem visibleItem)
                    {
                        visibleItem.ItemVisible = !itemData;
                    }
                }
            }
            // 释放资源
            item.Dispose();
        }

        private bool CheckItemNeedChange(string itemName, string keyName, BackupItemType itemType, bool currentItemData)
        {
            var item = GetItem(currentScene, keyName, itemType);
            if (item != null)
            {
                var itemData = false;
                try
                {
                    itemData = Convert.ToBoolean(item.ItemData);
                }
                catch
                {
                    return false;
                }
                if (itemData != currentItemData)
                {
                    restoreList.Add(new RestoreChangedItem(currentScene, itemName, itemData.ToString()));
                    return true;
                }
                else
                {
                    return false;
                }
            }
            if ((restoreMode == RestoreMode.DisableNotOnList && currentItemData) ||
                (restoreMode == RestoreMode.EnableNotOnList && !currentItemData))
            {
                restoreList.Add(new RestoreChangedItem(currentScene, itemName, (!currentItemData).ToString()));
                return true;
            }
            return false;
        }

        private void BackupRestoreItem(MyListItem item, string itemName, string keyName, BackupItemType backupItemType, int itemData, Scenes currentScene)
        {
            if (backup)
            {
                // 加入备份列表
                AddItem(keyName, backupItemType, itemData, currentScene);
            }
            else
            {
                // 恢复备份列表（新增备份类别处4）
                if (CheckItemNeedChange(itemName, keyName, backupItemType, itemData, out var restoreItemData))
                {
                    switch (backupItemType)
                    {
                        case BackupItemType.NumberIniRuleItem:
                            ((NumberIniRuleItem)item).ItemValue = restoreItemData; break;
                        case BackupItemType.NumberRegRuleItem:
                            ((NumberRegRuleItem)item).ItemValue = restoreItemData; break;
                    }
                }
            }
            // 释放资源
            item.Dispose();
        }

        private bool CheckItemNeedChange(string itemName, string keyName, BackupItemType itemType, int currentItemData, out int restoreItemData)
        {
            var item = GetItem(currentScene, keyName, itemType);
            if (item != null)
            {
                int itemData;
                try
                {
                    itemData = Convert.ToInt32(item.ItemData);
                }
                catch
                {
                    restoreItemData = 0;
                    return false;
                }
                if (itemData != currentItemData)
                {
                    restoreList.Add(new RestoreChangedItem(currentScene, itemName, itemData.ToString()));
                    restoreItemData = itemData;
                    return true;
                }
                else
                {
                    restoreItemData = 0;
                    return false;
                }
            }
            restoreItemData = 0;
            return false;
        }

        private void BackupRestoreItem(MyListItem item, string itemName, string keyName, BackupItemType backupItemType, string itemData, Scenes currentScene)
        {
            if (backup)
            {
                // 加入备份列表
                AddItem(keyName, backupItemType, itemData, currentScene);
            }
            else
            {
                // 恢复备份列表（新增备份类别处4）
                if (CheckItemNeedChange(itemName, keyName, backupItemType, itemData, out var restoreItemData))
                {
                    switch (backupItemType)
                    {
                        case BackupItemType.StringIniRuleItem:
                            ((StringIniRuleItem)item).ItemValue = restoreItemData; break;
                        case BackupItemType.StringRegRuleItem:
                            ((StringRegRuleItem)item).ItemValue = restoreItemData; break;
                    }
                }
            }
            // 释放资源
            item.Dispose();
        }

        private bool CheckItemNeedChange(string itemName, string keyName, BackupItemType itemType, string currentItemData, out string restoreItemData)
        {
            var item = GetItem(currentScene, keyName, itemType);
            if (item != null)
            {
                var itemData = item.ItemData;
                if (itemData != currentItemData)
                {
                    restoreList.Add(new RestoreChangedItem(currentScene, itemName, itemData.ToString()));
                    restoreItemData = itemData;
                    return true;
                }
                else
                {
                    restoreItemData = "";
                    return false;
                }
            }
            restoreItemData = "";
            return false;
        }

        // SelectItem有单独的备份恢复机制
        private void BackupRestoreSelectItem(SelectItem item, string itemData, Scenes currentScene)
        {
            var keyName = "";
            if (backup)
            {
                AddItem(keyName, BackupItemType.SelectItem, itemData, currentScene);
            }
            else
            {
                foreach (var restoreItem in sceneRestoreList)
                {
                    // 成功匹配到后的处理方式：只需检查ItemData和ItemType
                    if (restoreItem.ItemType == BackupItemType.SelectItem)
                    {
                        var restoreItemData = restoreItem.ItemData;
                        if (restoreItemData != itemData)
                        {
                            int.TryParse(restoreItem.KeyName, out var itemDataIndex);
                            switch (currentScene)
                            {
                                case Scenes.DragDrop:
                                    var dropEffect = (DropEffect)itemDataIndex;
                                    if (DefaultDropEffect != dropEffect)
                                    {
                                        DefaultDropEffect = dropEffect;
                                    }
                                    break;
                            }
                            var itemName = keyName;
                            restoreList.Add(new RestoreChangedItem(currentScene, itemName, restoreItemData.ToString()));
                        }
                    }
                }
            }
            item.Dispose();
            return;
        }
    }
}
