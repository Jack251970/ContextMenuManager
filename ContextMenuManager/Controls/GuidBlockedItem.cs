using ContextMenuManager.Methods;
using ContextMenuManager.Controls.Interfaces;
using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows;

namespace ContextMenuManager.Controls
{
    internal class GuidBlockedItem : MyListItem, IBtnShowMenuItem, ITsiWebSearchItem, ITsiFilePathItem, ITsiGuidItem, ITsiRegPathItem
    {
        public ContextMenu ContextMenu
        {
            get => Control.ContextMenu;
            set => Control.ContextMenu = value;
        }

        public new GuidBlockedList List;

        public GuidBlockedItem(GuidBlockedList list, string value) : base(list)
        {
            Value = value;
            if (list != null)
            {
                List = list;
                InitializeComponents();
            }
            
            if (GuidEx.TryParse(value, out var guid))
            {
                Guid = guid;
                if (list != null) Image = GuidInfo.GetImage(guid);
                ItemFilePath = GuidInfo.GetFilePath(Guid);
            }
            else
            {
                Guid = Guid.Empty;
                if (list != null) Image = AppImage.SystemFile;
            }

            Text = ItemText;
        }

        public string Value { get; set; }
        public Guid Guid { get; set; }
        public string SearchText => Value;
        public string ValueName => Value;
        public string RegPath
        {
            get
            {
                foreach (var path in GuidBlockedList.BlockedPaths)
                {
                    using var key = RegistryEx.GetRegistryKey(path);
                    if (key == null) continue;
                    if (key.GetValueNames().Contains(Value, StringComparer.OrdinalIgnoreCase)) return path;
                }
                return null;
            }
        }

        public string ItemText
        {
            get
            {
                string text;
                if (GuidEx.TryParse(Value, out var guid)) text = GuidInfo.GetText(guid);
                else text = AppString.Message.MalformedGuid;
                text += "\n" + Value;
                return text;
            }
        }

        public string ItemFilePath { get; set; }
        public MenuButton BtnShowMenu { get; set; }
        public DetailedEditButton BtnDetailedEdit { get; set; }
        public WebSearchMenuItem TsiSearch { get; set; }
        public FileLocationMenuItem TsiFileLocation { get; set; }
        public FilePropertiesMenuItem TsiFileProperties { get; set; }
        public HandleGuidMenuItem TsiHandleGuid { get; set; }
        public RegLocationMenuItem TsiRegLocation { get; set; }

        private RToolStripMenuItem TsiDetails { get; set; }
        private RToolStripMenuItem TsiDelete { get; set; }

        private void InitializeComponents()
        {
            BtnShowMenu = new MenuButton(this);
            BtnDetailedEdit = new DetailedEditButton(this);
            TsiSearch = new WebSearchMenuItem(this);
            TsiFileProperties = new FilePropertiesMenuItem(this);
            TsiFileLocation = new FileLocationMenuItem(this);
            TsiRegLocation = new RegLocationMenuItem(this);
            TsiHandleGuid = new HandleGuidMenuItem(this);
            TsiDetails = new(AppString.Menu.Details);
            TsiDelete = new(AppString.Menu.Delete);

            foreach (var item in new Control[] {TsiHandleGuid,
                new RToolStripSeparator(), TsiDetails, new RToolStripSeparator(), TsiDelete })
            {
                Control.ContextMenu.Items.Add(item);
            }
            
            foreach (var item in new Control[] { TsiSearch,
                new RToolStripSeparator(), TsiFileProperties, TsiFileLocation, TsiRegLocation})
            {
                TsiDetails.Items.Add(item);
            }

            TsiDelete.Click += (sender, e) => DeleteMe();
        }

        public void DeleteMe()
        {
            if (AppMessageBox.Show(AppString.Message.ConfirmDelete, null, MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            Array.ForEach(GuidBlockedList.BlockedPaths, path => RegistryEx.DeleteValue(path, Value));
            if (!Guid.Equals(Guid.Empty)) ExplorerRestarter.Show();
            var index = List.GetItemIndex(this);
            index -= (index < List.Controls.Count - 1) ? 0 : 1;
            List.Controls.Remove(Control);
            List.Controls[index]?.Focus();
            Dispose();
        }
    }
}
