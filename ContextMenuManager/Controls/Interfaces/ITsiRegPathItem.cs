using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;

namespace ContextMenuManager.Controls.Interfaces
{
    internal interface ITsiRegPathItem
    {
        string RegPath { get; }
        string ValueName { get; }
        MenuFlyout Flyout { get; set; }
        RegLocationMenuItem TsiRegLocation { get; set; }
    }

    internal sealed class RegLocationMenuItem : RToolStripMenuItem
    {
        public RegLocationMenuItem(ITsiRegPathItem item) : base(AppString.Menu.RegistryLocation)
        {
            Click += (sender, e) => ExternalProgram.JumpRegEdit(item.RegPath, item.ValueName, AppConfig.OpenMoreRegedit);
            item.Flyout.Opened += (sender, e) =>
            {
                using var key = RegistryEx.GetRegistryKey(item.RegPath);
                Visible = key != null;
            };
        }
    }
}
