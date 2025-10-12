using BluePointLilac.Controls;
using ContextMenuManager.Controls.Interfaces;
using ContextMenuManager.Methods;
using System.IO;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    interface ITsiRestoreFile
    {
        void RestoreItems(string restoreFile);
    }

    sealed class RestoreItem : MyListItem, IBtnShowMenuItem, ITsiFilePathItem, ITsiDeleteItem, ITsiRestoreItem
    {
        public RestoreItem(ITsiRestoreFile item, string filePath, string deviceName, string creatTime)
        {
            InitializeComponents();
            restoreInterface = item;
            FilePath = filePath;
            Text = AppString.Other.RestoreItemText.Replace("%device", deviceName).Replace("%time", creatTime);
            Image = AppImage.BackupItem;
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
        public string ItemFilePath { get { return filePath; } }

        public MenuButton BtnShowMenu { get; set; }
        public FilePropertiesMenuItem TsiFileProperties { get; set; }
        public FileLocationMenuItem TsiFileLocation { get; set; }
        public DeleteMeMenuItem TsiDeleteMe { get; set; }
        public RestoreMeMenuItem TsiRestoreMe { get; set; }

        readonly RToolStripMenuItem TsiDetails = new RToolStripMenuItem(AppString.Menu.Details);

        private void InitializeComponents()
        {
            BtnShowMenu = new MenuButton(this);
            TsiFileLocation = new FileLocationMenuItem(this);
            TsiFileProperties = new FilePropertiesMenuItem(this);
            TsiDeleteMe = new DeleteMeMenuItem(this);
            TsiRestoreMe = new RestoreMeMenuItem(this);

            // 设置菜单：详细信息；删除备份；恢复备份
            ContextMenuStrip.Items.AddRange(new ToolStripItem[] { TsiDetails, new RToolStripSeparator(),
                TsiRestoreMe, new RToolStripSeparator(), TsiDeleteMe });

            // 详细信息
            TsiDetails.DropDownItems.AddRange(new ToolStripItem[] { TsiFileProperties, TsiFileLocation });
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