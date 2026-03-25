using ContextMenuManager.Controls.Interfaces;
using ContextMenuManager.Methods;
using System.IO;
using System.Windows.Controls;

namespace ContextMenuManager.Controls
{
    internal interface ITsiRestoreFile
    {
        void RestoreItems(string restoreFile);
    }

    internal sealed class RestoreItem : MyListItem, IBtnShowMenuItem, ITsiFilePathItem, ITsiDeleteItem, ITsiRestoreItem
    {
        public RestoreItem(MyList list, ITsiRestoreFile item, string filePath, string deviceName, string creatTime) : base(list)
        {
            if (list != null)
            {
                InitializeComponents();
                restoreInterface = item;
                FilePath = filePath;
                Text = AppString.Other.RestoreItemText.Replace("%device", deviceName).Replace("%time", creatTime);
                Image = AppImage.BackupItem;
            }
        }

        // 恢复函数接口对象
        private readonly ITsiRestoreFile restoreInterface;

        // 备份文件目录
        private string filePath;
        public string FilePath
        {
            get => filePath;
            set => filePath = value;
        }
        public string ItemFilePath => filePath;

        public MenuButton BtnShowMenu { get; set; }
        public FilePropertiesMenuItem TsiFileProperties { get; set; }
        public FileLocationMenuItem TsiFileLocation { get; set; }
        public DeleteMeMenuItem TsiDeleteMe { get; set; }
        public RestoreMeMenuItem TsiRestoreMe { get; set; }

        private RToolStripMenuItem TsiDetails { get; set; }

        private void InitializeComponents()
        {
            BtnShowMenu = new MenuButton(this);
            TsiFileLocation = new FileLocationMenuItem(this);
            TsiFileProperties = new FilePropertiesMenuItem(this);
            TsiDeleteMe = new DeleteMeMenuItem(this);
            TsiRestoreMe = new RestoreMeMenuItem(this);
            TsiDetails = new(AppString.Menu.Details);

            // 设置菜单：详细信息；删除备份；恢复备份
            foreach (var item in new Control[] { TsiDetails, new RToolStripSeparator(),
                TsiRestoreMe, new RToolStripSeparator(), TsiDeleteMe })
            {
                ContextMenu.Items.Add(item);
            }

            // 详细信息
            foreach (var item in new Control[] { TsiFileProperties, TsiFileLocation })
            {
                TsiDetails.Items.Add(item);
            }
        }

        public void DeleteMe()
        {
            File.Delete(filePath);
        }

        public void RestoreMe()
        {
            restoreInterface.RestoreItems(filePath);
        }
    }
}
