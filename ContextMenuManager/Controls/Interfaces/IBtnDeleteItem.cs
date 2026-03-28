using ContextMenuManager.Methods;
using System.Windows;

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
            Click += (sender, e) =>
            {
                if (AppMessageBox.Show(AppString.Message.ConfirmDelete, null, MessageBoxButton.YesNo) == MessageBoxResult.Yes) item.DeleteMe();
            };
        }
    }
}
