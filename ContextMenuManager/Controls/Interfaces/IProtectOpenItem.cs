namespace ContextMenuManager.Controls.Interfaces
{
    internal interface IProtectOpenItem
    {
        bool ItemVisible { get; set; }
        bool TryProtectOpenItem();
    }
}