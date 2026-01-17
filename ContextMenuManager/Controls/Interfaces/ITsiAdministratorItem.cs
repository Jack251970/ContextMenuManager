using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System.IO;
using System.Windows.Forms;

namespace ContextMenuManager.Controls.Interfaces
{
    interface ITsiAdministratorItem
    {
        ContextMenuStrip ContextMenuStrip { get; set; }
        RunAsAdministratorItem TsiAdministrator { get; set; }
        ShellLink ShellLink { get; }
    }

    sealed class RunAsAdministratorItem : RToolStripMenuItem
    {
        public RunAsAdministratorItem(ITsiAdministratorItem item) : base(AppString.Menu.RunAsAdministrator)
        {
            item.ContextMenuStrip.Opening += (sender, e) =>
            {
                if(item.ShellLink == null)
                {
                    Enabled = false;
                    return;
                }
                string filePath = item.ShellLink.TargetPath;
                string extension = Path.GetExtension(filePath)?.ToLower();
                switch(extension)
                {
                    case ".exe":
                    case ".bat":
                    case ".cmd":
                        Enabled = true;
                        break;
                    default:
                        Enabled = false;
                        break;
                }
                Checked = item.ShellLink.RunAsAdministrator;
            };
            Click += (sender, e) =>
            {
                item.ShellLink.RunAsAdministrator = !Checked;
                item.ShellLink.Save();
                if(item is WinXItem) ExplorerRestarter.Show();
            };
        }
    }
}