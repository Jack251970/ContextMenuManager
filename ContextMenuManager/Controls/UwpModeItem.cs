using ContextMenuManager.Controls.Interfaces;
using ContextMenuManager.Methods;
using Microsoft.Win32;
using System;
using iNKORE.UI.WPF.Modern.Controls;
using System.Windows.Controls;

namespace ContextMenuManager.Controls
{
    internal sealed class UwpModeItem : MyListItem, IChkVisibleItem, ITsiRegPathItem, ITsiFilePathItem,
        IBtnShowMenuItem, ITsiWebSearchItem, ITsiRegExportItem, ITsiRegDeleteItem, ITsiGuidItem
    {
        public MenuFlyout Flyout { get; set; }

        public UwpModeItem(MyList list, string uwpName, Guid guid) : base(list)
        {
            Guid = guid;
            UwpName = uwpName;
            Text = ItemText;
            if (list != null)
            {
                InitializeComponents();
                Visible = UwpHelper.GetPackageName(uwpName) != null;
                Image = GuidInfo.GetImage(guid);
            }
        }

        public Guid Guid { get; set; }
        public string UwpName { get; set; }

        public bool ItemVisible // 是否显示于右键菜单中
        {
            get
            {
                foreach (var path in GuidBlockedList.BlockedPaths)
                {
                    using var key = RegistryEx.GetRegistryKey(path);
                    if (key == null) continue;
                    if (key.GetValue(Guid.ToString("B")) != null) return false;
                }
                return true;
            }
            set
            {
                foreach (var path in GuidBlockedList.BlockedPaths)
                {
                    if (value)
                    {
                        RegistryEx.DeleteValue(path, Guid.ToString("B"));
                    }
                    else
                    {
                        Registry.SetValue(path, Guid.ToString("B"), "");
                    }
                }
                ExplorerRestarter.Show();
            }
        }

        public string ItemText => GuidInfo.GetText(Guid);
        public string RegPath => UwpHelper.GetRegPath(UwpName, Guid);
        public string ItemFilePath => UwpHelper.GetFilePath(UwpName, Guid);

        public string SearchText => Text;
        public string ValueName => "DllPath";
        public MenuButton BtnShowMenu { get; set; }
        public VisibleCheckBox ChkVisible { get; set; }
        public DetailedEditButton BtnDetailedEdit { get; set; }
        public RegLocationMenuItem TsiRegLocation { get; set; }
        public FileLocationMenuItem TsiFileLocation { get; set; }
        public FilePropertiesMenuItem TsiFileProperties { get; set; }
        public WebSearchMenuItem TsiSearch { get; set; }
        public DeleteMeMenuItem TsiDeleteMe { get; set; }
        public RegExportMenuItem TsiRegExport { get; set; }
        public HandleGuidMenuItem TsiHandleGuid { get; set; }

        private RToolStripMenuItem TsiDetails { get; set; }

        private void InitializeComponents()
        {
            BtnShowMenu = new MenuButton(this);
            ChkVisible = new VisibleCheckBox(this);
            BtnDetailedEdit = new DetailedEditButton(this);
            TsiSearch = new WebSearchMenuItem(this);
            TsiFileLocation = new FileLocationMenuItem(this);
            TsiFileProperties = new FilePropertiesMenuItem(this);
            TsiRegLocation = new RegLocationMenuItem(this);
            TsiDeleteMe = new DeleteMeMenuItem(this);
            TsiRegExport = new RegExportMenuItem(this);
            TsiHandleGuid = new HandleGuidMenuItem(this);
            TsiDetails = new(AppString.Menu.Details);

            foreach (var item in new Control[] { TsiHandleGuid,
                new RToolStripSeparator(), TsiDetails, new RToolStripSeparator(), TsiDeleteMe })
            {
                Flyout.Items.Add(item);
            }

            foreach (var item in new Control[] { TsiSearch, new RToolStripSeparator(),
                TsiFileProperties, TsiFileLocation, TsiRegLocation, TsiRegExport })
            {
                TsiDetails.Items.Add(item);
            }
        }

        public void DeleteMe()
        {
            RegistryEx.DeleteKeyTree(RegPath);
        }
    }
}
