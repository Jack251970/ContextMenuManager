using BluePointLilac.Controls;
using ContextMenuManager.Methods;
using System.Windows.Forms;

namespace ContextMenuManager.Controls.Interfaces
{
    internal interface IBtnDeleteItem
    {
        DeleteButton BtnDelete { get; set; }
        void DeleteMe();
    }

    internal sealed class DeleteButton : PictureButton
    {
        public DeleteButton(IBtnDeleteItem item) : base(AppImage.Delete)
        {
            var listItem = (MyListItem)item;
            listItem.AddCtr(this);
            MouseDown += (sender, e) =>
            {
                if (AppMessageBox.Show(AppString.Message.ConfirmDelete, MessageBoxButtons.YesNo) == DialogResult.Yes) item.DeleteMe();
            };
        }
    }
}