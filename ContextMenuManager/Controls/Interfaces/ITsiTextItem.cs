using ContextMenuManager.Methods;

namespace ContextMenuManager.Controls.Interfaces
{
    internal interface ITsiTextItem
    {
        string Text { get; set; }
        string ItemText { get; set; }
        ChangeTextMenuItem TsiChangeText { get; set; }
    }

    internal sealed class ChangeTextMenuItem : RToolStripMenuItem
    {
        public ChangeTextMenuItem(ITsiTextItem item) : base(AppString.Menu.ChangeText)
        {
            Click += (sender, e) =>
            {
                var name = ChangeText(item.ItemText ?? item.Text);
                if (name != null) item.ItemText = name;
            };
        }

        private string ChangeText(string text)
        {
            var dlg = new InputDialog { Text = text, Title = AppString.Menu.ChangeText };
            if (dlg.ShowDialog() != true) return null;
            if (dlg.Text.Length == 0)
            {
                AppMessageBox.Show(AppString.Message.TextCannotBeEmpty);
                return ChangeText(text);
            }
            else if (ResourceString.GetDirectString(dlg.Text).Length == 0)
            {
                AppMessageBox.Show(AppString.Message.StringParsingFailed);
                return ChangeText(text);
            }
            else return dlg.Text;
        }
    }
}
