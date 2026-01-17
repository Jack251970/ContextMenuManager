using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System.Drawing;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    internal sealed class SubItemsForm : RForm
    {
        public SubItemsForm()
        {
            SuspendLayout();
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = MaximizeBox = MinimizeBox = false;
            MinimumSize = Size = new Size(646, 419).DpiZoom();
            Controls.AddRange(new Control[] { listBox, statusBar });
            statusBar.CanMoveForm();
            this.AddEscapeButton();
            ResumeLayout();
            InitTheme();
        }

        private readonly MyListBox listBox = new()
        { Dock = DockStyle.Fill };
        private readonly MyStatusBar statusBar = new();

        public void AddList(MyList myList)
        {
            myList.Owner = listBox;
            myList.HoveredItemChanged += (sender, e) =>
            {
                if (!AppConfig.ShowFilePath) return;
                var item = myList.HoveredItem;
                foreach (var prop in new[] { "ItemFilePath", "RegPath", "GroupPath" })
                {
                    var path = item.GetType().GetProperty(prop)?.GetValue(item, null)?.ToString();
                    if (!path.IsNullOrWhiteSpace()) { statusBar.Text = path; return; }
                }
                statusBar.Text = item.Text;
            };
        }
    }
}