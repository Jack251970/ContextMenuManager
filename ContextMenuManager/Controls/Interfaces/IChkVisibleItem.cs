using ContextMenuManager.Methods;
using System;
using System.Windows.Forms;

namespace ContextMenuManager.Controls.Interfaces
{
    internal interface IChkVisibleItem
    {
        bool ItemVisible { get; set; }
        VisibleCheckBox ChkVisible { get; set; }
    }

    internal sealed class VisibleCheckBox : CheckBox
    {
        public Action CheckChanged;
        public Func<bool> PreCheckChanging;

        public VisibleCheckBox(IChkVisibleItem item)
        {
            this.AutoCheck = false;
            this.Cursor = Cursors.Hand;
            var listItem = (MyListItem)item;
            listItem.AddCtr(this);
            this.CheckedChanged += (s, e) => CheckChanged?.Invoke();
            CheckChanged += () => item.ItemVisible = Checked;
            listItem.ParentChanged += (sender, e) =>
            {
                if (listItem.IsDisposed) return;
                if (listItem.Parent == null) return;
                Checked = item.ItemVisible;
                if (listItem is FoldSubItem subItem && subItem.FoldGroupItem != null) return;
                if (listItem.FindForm() is ShellStoreDialog.ShellStoreForm) return;
                if (AppConfig.HideDisabledItems) listItem.Visible = Checked;
            };
        }

        protected override void OnClick(EventArgs e)
        {
            if (PreCheckChanging == null || PreCheckChanging())
            {
                Checked = !Checked;
                base.OnClick(e);
            }
        }
    }
}