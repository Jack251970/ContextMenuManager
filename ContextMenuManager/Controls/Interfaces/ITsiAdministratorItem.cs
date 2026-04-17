using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using System.IO;

namespace ContextMenuManager.Controls.Interfaces
{
    internal interface ITsiAdministratorItem
    {
        MenuFlyout Flyout { get; set; }
        RunAsAdministratorItem TsiAdministrator { get; set; }
        ShellLink ShellLink { get; }
    }

    internal sealed class RunAsAdministratorItem : RToolStripMenuItem
    {
        public RunAsAdministratorItem(ITsiAdministratorItem item) : base(AppString.Menu.RunAsAdministrator)
        {
            item.Flyout.Opened += (sender, e) =>
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
