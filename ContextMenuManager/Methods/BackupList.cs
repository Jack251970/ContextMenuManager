using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace ContextMenuManager.Methods
{
    public sealed class BackupList
    {
        // 元数据缓存区
        public static MetaData metaData = new();

        // 备份列表/恢复列表缓存区
        private static List<BackupItem> backupRestoreList = new();

        // 备份查找表
        private static readonly Dictionary<string, BackupItem> backupLookup = new();

        // 单场景恢复列表暂存区
        public static List<BackupItem> sceneRestoreList = new();

        // 创建一个XmlSerializer实例并设置根节点
        private static readonly XmlSerializer backupDataSerializer = new(typeof(BackupData),
            new XmlRootAttribute("ContextMenuManager"));
        // 自定义命名空间
        private static readonly XmlSerializerNamespaces namespaces = new();

        // 创建一个XmlSerializer实例并设置根节点
        private static readonly XmlSerializer metaDataSerializer = new(typeof(MetaData),
            new XmlRootAttribute("MetaData"));

        static BackupList()
        {
            // 禁用默认命名空间
            namespaces.Add(string.Empty, string.Empty);
        }

        private static string GetLookupKey(Scenes scene, string keyName, BackupItemType type)
        {
            var normalizedKeyName = keyName ?? string.Empty;
            var encodedKeyName = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(normalizedKeyName));
            return $"{scene}|{encodedKeyName}|{type}";
        }

        public static BackupItem GetItem(Scenes scene, string keyName, BackupItemType type)
        {
            var key = GetLookupKey(scene, keyName, type);
            return backupLookup.TryGetValue(key, out var item) ? item : null;
        }

        public static void AddItem(string keyName, BackupItemType backupItemType, string itemData, Scenes scene)
        {
            var item = new BackupItem
            {
                KeyName = keyName,
                ItemType = backupItemType,
                ItemData = itemData,
                BackupScene = scene,
            };
            var key = GetLookupKey(scene, keyName, backupItemType);
            if (backupLookup.TryGetValue(key, out var existingItem))
            {
                // Replace the existing item in both the list and the lookup to keep them consistent.
                int index = backupRestoreList.IndexOf(existingItem);
                if (index >= 0)
                {
                    backupRestoreList[index] = item;
                }
                backupLookup[key] = item;
            }
            else
            {
                backupRestoreList.Add(item);
                backupLookup.Add(key, item);
            }
        }

        public static void AddItem(string keyName, BackupItemType backupItemType, bool itemData, Scenes scene)
        {
            AddItem(keyName, backupItemType, itemData.ToString(), scene);
        }

        public static void AddItem(string keyName, BackupItemType backupItemType, int itemData, Scenes scene)
        {
            AddItem(keyName, backupItemType, itemData.ToString(), scene);
        }

        public static int GetBackupListCount()
        {
            return backupRestoreList.Count;
        }

        public static void ClearBackupList()
        {
            backupRestoreList.Clear();
            backupLookup.Clear();
        }

        public static void SaveBackupList(string filePath)
        {
            // 创建一个父对象，并将BackupList和MetaData对象包装到其中
            var myData = new BackupData()
            {
                MetaData = metaData,
                BackupList = backupRestoreList,
            };

            // 序列化root对象并保存到XML文档
            using var stream = new FileStream(filePath, FileMode.Create);
            backupDataSerializer.Serialize(stream, myData, namespaces);
        }

        public static void LoadBackupList(string filePath)
        {
            // 反序列化XML文件并获取根对象
            BackupData myData;
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                myData = (BackupData)backupDataSerializer.Deserialize(stream);
            }

            // 获取MetaData对象
            metaData = myData.MetaData;

            // 清理backupRestoreList变量
            backupRestoreList.Clear();
            backupRestoreList = null;

            // 获取BackupList对象
            backupRestoreList = myData.BackupList;
            backupLookup.Clear();
            foreach (var item in backupRestoreList)
            {
                var key = GetLookupKey(item.BackupScene, item.KeyName, item.ItemType);
                if (!backupLookup.ContainsKey(key)) backupLookup.Add(key, item);
            }
        }

        public static void LoadTempRestoreList(Scenes scene)
        {
            sceneRestoreList.Clear();
            // 根据backupScene加载列表
            foreach (var item in backupRestoreList)
            {
                if (item.BackupScene == scene)
                {
                    sceneRestoreList.Add(item);
                }
            }
        }

        public static void LoadBackupDataMetaData(string filePath)
        {
            // 反序列化root对象并保存到XML文档
            using var stream = new FileStream(filePath, FileMode.Open);
            // 读取 <MetaData> 节点
            using var reader = XmlReader.Create(stream);
            // 寻找第一个<MetaData>节点
            reader.ReadToFollowing("MetaData");

            // 清理metaData变量
            metaData = null;

            // 反序列化<MetaData>节点为MetaData对象
            metaData = (MetaData)metaDataSerializer.Deserialize(reader);
        }
    }
}
