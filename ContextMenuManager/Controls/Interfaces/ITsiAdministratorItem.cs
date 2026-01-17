using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System.IO;
using System.Windows.Forms;

namespace ContextMenuManager.Controls.Interfaces
{
    internal interface ITsiAdministratorItem
    {
        ContextMenuStrip ContextMenuStrip { get; set; }
        RunAsAdministratorItem TsiAdministrator { get; set; }
        ShellLink ShellLink { get; }
    }

    internal sealed class RunAsAdministratorItem : RToolStripMenuItem
    {
        public RunAsAdministratorItem(ITsiAdministratorItem item) : base(AppString.Menu.RunAsAdministrator)
        {
            item.ContextMenuStrip.Opening += (sender, e) =>
            {
                if (item.ShellLink == null)
                {
                    Enabled = false;
                    return;
                }
                var filePath = item.ShellLink.TargetPath;
                var extension = Path.GetExtension(filePath)?.ToLower();
                Enabled = extension switch
                {
                    ".exe" or ".bat" or ".cmd" => true,
                    _ => false,
                };
                Checked = item.ShellLink.RunAsAdministrator;
            };
            Click += (sender, e) =>
            {
                item.ShellLink.RunAsAdministrator = !Checked;
                item.ShellLink.Save();
                if (item is WinXItem) ExplorerRestarter.Show();
            };
        }
    }
}