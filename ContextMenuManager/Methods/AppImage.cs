using System.Drawing;
using ContextMenuManager.Properties;

namespace ContextMenuManager.Methods
{
    internal static class AppImage
    {
        ///<summary>设置按钮</summary>
        public static readonly Image Setting = AppResources.Setting;
        ///<summary>编辑子项</summary>
        public static readonly Image SubItems = AppResources.SubItems;
        ///<summary>删除</summary>
        public static readonly Image Delete = AppResources.Delete;
        ///<summary>添加</summary>
        public static readonly Image AddNewItem = AppResources.Add;
        ///<summary>添加已有项目</summary>
        public static readonly Image AddExisting = AppResources.AddExisting;
        ///<summary>添加分割线</summary>
        public static readonly Image AddSeparator = AppResources.AddSeparator;
        ///<summary>添加增强菜单</summary>
        public static readonly Image Enhance = AppResources.Enhance;
        ///<summary>打开</summary>
        public static readonly Image Open = AppResources.Open;
        ///<summary>菜单风格</summary>
        public static readonly Image ContextMenuStyle = AppResources.ContextMenuStyle;
        ///<summary>排序</summary>
        public static readonly Image Sort = AppResources.Sort;
        ///<summary>上</summary>
        public static readonly Image Up = AppResources.Up;
        ///<summary>下</summary>
        public static readonly Image Down = AppResources.Down;
        ///<summary>新建项目</summary>
        public static readonly Image NewItem = AppResources.NewItem;
        ///<summary>备份项目</summary>
        public static readonly Image BackupItem = AppResources.BackupItem;
        ///<summary>新建文件夹</summary>
        public static readonly Image NewFolder = AppResources.NewFolder;
        ///<summary>自定义</summary>
        public static readonly Image Custom = AppResources.Custom;
        ///<summary>选择</summary>
        public static readonly Image Select = AppResources.Select;
        ///<summary>跳转</summary>
        public static readonly Image Jump = AppResources.Jump;
        ///<summary>Microsoft Store</summary>
        public static readonly Image MicrosoftStore = AppResources.MicrosoftStore;
        ///<summary>用户</summary>
        public static readonly Image User = AppResources.User;
        ///<summary>网络</summary>
        public static readonly Image Web = AppResources.Web;
        ///<summary>系统文件</summary>
        public static readonly Image SystemFile = GetIconImage("imageres.dll", -67);
        ///<summary>资源不存在</summary>
        public static readonly Image NotFound = GetIconImage("imageres.dll", -2);
        ///<summary>管理员小盾牌</summary>
        public static readonly Image Shield = GetIconImage("imageres.dll", -78);
        ///<summary>资源管理器</summary>
        public static readonly Image Explorer = GetIconImage("explorer.exe", 0);
        ///<summary>重启Explorer</summary>
        public static readonly Image RestartExplorer = GetIconImage("shell32.dll", 238);
        ///<summary>网络驱动器</summary>
        public static readonly Image NetworkDrive = GetIconImage("imageres.dll", -33);
        ///<summary>发送到</summary>
        public static readonly Image SendTo = GetIconImage("imageres.dll", -185);
        ///<summary>回收站</summary>
        public static readonly Image RecycleBin = GetIconImage("imageres.dll", -55);
        ///<summary>磁盘</summary>
        public static readonly Image Drive = GetIconImage("imageres.dll", -30);
        ///<summary>文件</summary>
        public static readonly Image File = GetIconImage("imageres.dll", -19);
        ///<summary>文件夹</summary>
        public static readonly Image Folder = GetIconImage("imageres.dll", -3);
        ///<summary>目录</summary>
        public static readonly Image Directory = GetIconImage("imageres.dll", -162);
        ///<summary>所有对象</summary>
        public static readonly Image AllObjects = GetIconImage("imageres.dll", -117);
        ///<summary>锁定</summary>
        public static readonly Image Lock = GetIconImage("imageres.dll", -59);
        ///<summary>快捷方式图标</summary>
        public static readonly Image LnkFile = GetIconImage("shell32.dll", -16769);
        ///<summary>搜索</summary>
        public static readonly Image Search = GetIconImage("shell32.dll", -23);

        private static Image GetIconImage(string dllName, int iconIndex)
        {
            using var icon = ResourceIcon.GetIcon(dllName, iconIndex); return icon?.ToBitmap();
        }
    }
}
