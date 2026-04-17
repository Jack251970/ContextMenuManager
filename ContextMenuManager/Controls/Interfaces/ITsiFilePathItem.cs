using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using System.IO;

namespace ContextMenuManager.Controls.Interfaces
{
    internal interface ITsiFilePathItem
    {
        string ItemFilePath { get; }
        MenuFlyout Flyout { get; set; }
        FileLocationMenuItem TsiFileLocation { get; set; }
        FilePropertiesMenuItem TsiFileProperties { get; set; }
    }

    internal sealed class FileLocationMenuItem : RToolStripMenuItem
    {
        public FileLocationMenuItem(ITsiFilePathItem item) : base(AppString.Menu.FileLocation)
        {
            item.Flyout.Opened += (sender, e) =>
            {
                Visible = item.ItemFilePath != null;
            };
            Click += (sender, e) => ExternalProgram.JumpExplorer(item.ItemFilePath, AppConfig.OpenMoreExplorer);
        }
    }

    internal sealed class FilePropertiesMenuItem : RToolStripMenuItem
    {
        public FilePropertiesMenuItem(ITsiFilePathItem item) : base(AppString.Menu.FileProperties)
        {
            item.Flyout.Opened += (sender, e) =>
            {
                var path = item.ItemFilePath;
                Visible = Directory.Exists(path) || File.Exists(path);
            };
            Click += (sender, e) => ExternalProgram.ShowPropertiesDialog(item.ItemFilePath);
        }
    }
}
