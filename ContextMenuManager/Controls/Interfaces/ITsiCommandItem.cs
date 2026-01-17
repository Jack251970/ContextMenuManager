using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System.Drawing;
using System.Windows.Forms;

namespace ContextMenuManager.Controls.Interfaces
{
    internal interface ITsiCommandItem
    {
        string ItemCommand { get; set; }
        ChangeCommandMenuItem TsiChangeCommand { get; set; }
    }

    internal sealed class ChangeCommandMenuItem : RToolStripMenuItem
    {
        public bool CommandCanBeEmpty { get; set; }

        public ChangeCommandMenuItem(ITsiCommandItem item) : base(AppString.Menu.ChangeCommand)
        {
            Click += (sender, e) =>
            {
                var command = ChangeCommand(item.ItemCommand);
                if (command != null) item.ItemCommand = command;
            };
        }

        private string ChangeCommand(string command)
        {
            using var dlg = new InputDialog();
            dlg.Text = command;
            dlg.Title = AppString.Menu.ChangeCommand;
            dlg.Size = new Size(530, 260).DpiZoom();
            if (dlg.ShowDialog() != DialogResult.OK) return null;
            if (!CommandCanBeEmpty && string.IsNullOrEmpty(dlg.Text))
            {
                AppMessageBox.Show(AppString.Message.CommandCannotBeEmpty);
                return ChangeCommand(command);
            }
            else return dlg.Text;
        }
    }
}