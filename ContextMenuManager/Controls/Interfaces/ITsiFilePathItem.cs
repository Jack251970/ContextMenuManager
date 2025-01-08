using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System.IO;
using System.Windows.Forms;

namespace ContextMenuManager.Controls.Interfaces
{
    interface ITsiFilePathItem
    {
        string ItemFilePath { get; }
        ContextMenuStrip ContextMenuStrip { get; set; }
        FileLocationMenuItem TsiFileLocation { get; set; }
        FilePropertiesMenuItem TsiFileProperties { get; set; }
    }

    sealed class FileLocationMenuItem : RToolStripMenuItem
    {
        public FileLocationMenuItem(ITsiFilePathItem item) : base(AppString.Menu.FileLocation)
        {
            item.ContextMenuStrip.Opening += (sender, e) =>
            {
                Visible = item.ItemFilePath != null;
            };
            Click += (sender, e) => ExternalProgram.JumpExplorer(item.ItemFilePath, AppConfig.OpenMoreExplorer);
        }
    }

    sealed class FilePropertiesMenuItem : RToolStripMenuItem
    {
        public FilePropertiesMenuItem(ITsiFilePathItem item) : base(AppString.Menu.FileProperties)
        {
            item.ContextMenuStrip.Opening += (sender, e) =>
            {
                string path = item.ItemFilePath;
                Visible = Directory.Exists(path) || File.Exists(path);
            };
            Click += (sender, e) => ExternalProgram.ShowPropertiesDialog(item.ItemFilePath);
        }
    }
}