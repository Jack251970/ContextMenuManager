using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace ContextMenuManager.Methods
{
    /*******************************外部枚举变量************************************/

    // 右键菜单场景（新增备份类别处1）
    public enum Scenes
    {
        // 主页——第一板块
        File, Folder, Directory, Background, Desktop, Drive, AllObjects, Computer, RecycleBin, Library,
        // 主页——第二板块
        New, SendTo, OpenWith,
        // 主页——第三板块
        WinX,
        // 文件类型——第一板块
        LnkFile, UwpLnk, ExeFile, UnknownType,
        // 文件类型——第二板块
        CustomExtension, PerceivedType, DirectoryType,
        // 其他规则——第一板块
        EnhanceMenu, DetailedEdit,
        // 其他规则——第二板块
        DragDrop, PublicReferences, InternetExplorer,
        // 其他规则——第三板块（不予备份）
        // 不予备份的场景
        MenuAnalysis, CustomRegPath, CustomExtensionPerceivedType,
    };

    // 备份项目类型（新增备份类别处3）
    public enum BackupItemType
    {
        ShellItem, ShellExItem, UwpModelItem, VisibleRegRuleItem, ShellNewItem, SendToItem,
        OpenWithItem, WinXItem, SelectItem, StoreShellItem, IEItem, EnhanceShellItem, EnhanceShellExItem,
        NumberIniRuleItem, StringIniRuleItem, VisbleIniRuleItem, NumberRegRuleItem, StringRegRuleItem,
    }

    // 备份选项
    public enum BackupMode
    {
        All,            // 备份全部菜单项目
        OnlyVisible,    // 仅备份启用的菜单项目
        OnlyInvisible   // 仅备份禁用的菜单项目
    };

    // 恢复模式
    public enum RestoreMode
    {
        NotHandleNotOnList,     // 启用备份列表上可见的菜单项，禁用备份列表上不可见的菜单项，不处理不位于备份列表上的菜单项
        DisableNotOnList,       // 启用备份列表上可见的菜单项，禁用备份列表上不可见以及不位于备份列表上的菜单项
        EnableNotOnList,        // 启用备份列表上可见的菜单项以及不位于备份列表上的菜单项，禁用备份列表上不可见
    };

    // 定义一个类来表示备份数据
    [Serializable, XmlType("BackupData")]
    public sealed class BackupData
    {
        [XmlElement("MetaData")]
        public MetaData MetaData { get; set; }

        [XmlElement("BackupList")]
        public List<BackupItem> BackupList { get; set; }
    }

    // 定义一个类来表示备份项目
    [Serializable, XmlType("BackupItem")]
    public sealed class BackupItem
    {
        [XmlElement("KeyName")]
        public string KeyName { get; set; } // 查询索引名字

        [XmlElement("ItemType")]
        public BackupItemType ItemType { get; set; } // 备份项目类型

        [XmlElement("ItemData")]
        public string ItemData { get; set; } // 备份数据：是否位于右键菜单中，数字，或者字符串

        [XmlElement("Scene")]
        public Scenes BackupScene { get; set; } // 右键菜单位置
    }

    // 定义一个类来表示备份项目的元数据
    [Serializable, XmlType("MetaData")]
    public sealed class MetaData
    {
        [XmlElement("Version")]
        public int Version { get; set; } // 备份版本

        [XmlElement("BackupScenes")]
        public List<Scenes> BackupScenes { get; set; } // 备份场景

        [XmlElement("CreateTime")]
        public DateTime CreateTime { get; set; } // 备份时间

        [XmlElement("Device")]
        public string Device { get; set; } // 备份设备
    }

    // 定义一个类来表示恢复项目
    public sealed class RestoreChangedItem
    {
        public RestoreChangedItem(Scenes scene, string keyName, string itemData)
        {
            BackupScene = scene;
            KeyName = keyName;
            ItemData = itemData;
        }

        public Scenes BackupScene { get; set; } // 右键菜单位置

        public string KeyName { get; set; } // 查询索引名字

        public string ItemData { get; set; } // 备份数据：是否位于右键菜单中，数字，或者字符串
    }
}
