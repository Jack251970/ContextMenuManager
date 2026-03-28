using ContextMenuManager.Methods;
using System.Drawing;

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
            var dlg = new InputDialog
            {
                Text = command,
                Title = AppString.Menu.ChangeCommand,
                Size = new Size(530, 260)
            };
            if (dlg.ShowDialog() != true) return null;
            if (!CommandCanBeEmpty && string.IsNullOrEmpty(dlg.Text))
            {
                AppMessageBox.Show(AppString.Message.CommandCannotBeEmpty);
                return ChangeCommand(command);
            }
            else return dlg.Text;
        }
    }
}