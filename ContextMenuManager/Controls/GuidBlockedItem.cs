using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Controls.Interfaces;
using ContextMenuManager.Methods;
using System;
using System.Linq;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    class GuidBlockedItem : MyListItem, IBtnShowMenuItem, ITsiWebSearchItem, ITsiFilePathItem, ITsiGuidItem, ITsiRegPathItem
    {
        public GuidBlockedItem(string value)
        {
            InitializeComponents();
            Value = value;
            if (GuidEx.TryParse(value, out Guid guid))
            {
                Guid = guid;
                Image = GuidInfo.GetImage(guid);
                ItemFilePath = GuidInfo.GetFilePath(Guid);
            }
            else
            {
                Guid = Guid.Empty;
                Image = AppImage.SystemFile;
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
                foreach (string path in GuidBlockedList.BlockedPaths)
                {
                    using (var key = RegistryEx.GetRegistryKey(path))
                    {
                        if (key == null) continue;
                        if (key.GetValueNames().Contains(Value, StringComparer.OrdinalIgnoreCase)) return path;
                    }
                }
                return null;
            }
        }

        public string ItemText
        {
            get
            {
                string text;
                if (GuidEx.TryParse(Value, out Guid guid)) text = GuidInfo.GetText(guid);
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

        readonly RToolStripMenuItem TsiDetails = new RToolStripMenuItem(AppString.Menu.Details);
        readonly RToolStripMenuItem TsiDelete = new RToolStripMenuItem(AppString.Menu.Delete);

        private void InitializeComponents()
        {
            BtnShowMenu = new MenuButton(this);
            BtnDetailedEdit = new DetailedEditButton(this);
            TsiSearch = new WebSearchMenuItem(this);
            TsiFileProperties = new FilePropertiesMenuItem(this);
            TsiFileLocation = new FileLocationMenuItem(this);
            TsiRegLocation = new RegLocationMenuItem(this);
            TsiHandleGuid = new HandleGuidMenuItem(this);

            ContextMenuStrip.Items.AddRange(new ToolStripItem[] {TsiHandleGuid,
                new RToolStripSeparator(), TsiDetails, new RToolStripSeparator(), TsiDelete });
            TsiDetails.DropDownItems.AddRange(new ToolStripItem[] { TsiSearch,
                new RToolStripSeparator(), TsiFileProperties, TsiFileLocation, TsiRegLocation});

            TsiDelete.Click += (sender, e) => DeleteMe();
        }

        public void DeleteMe()
        {
            if (AppMessageBox.Show(AppString.Message.ConfirmDelete, MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            Array.ForEach(GuidBlockedList.BlockedPaths, path => RegistryEx.DeleteValue(path, Value));
            if (!Guid.Equals(Guid.Empty)) ExplorerRestarter.Show();
            int index = Parent.Controls.GetChildIndex(this);
            index -= (index < Parent.Controls.Count - 1) ? 0 : 1;
            Parent.Controls[index].Focus();
            Dispose();
        }
    }
}