using BluePointLilac.Methods;
using ContextMenuManager.Controls.Interfaces;
using ContextMenuManager.Methods;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    internal sealed class ShellExItem : FoldSubItem, IChkVisibleItem, IBtnShowMenuItem, ITsiGuidItem,
        ITsiWebSearchItem, ITsiFilePathItem, ITsiRegPathItem, ITsiRegDeleteItem, ITsiRegExportItem, IProtectOpenItem
    {
        public static Dictionary<string, Guid> GetPathAndGuids(string shellExPath, bool isDragDrop = false)
        {
            var dic = new Dictionary<string, Guid>();
            var parts = isDragDrop ? DdhParts : CmhParts;
            foreach (var part in parts)
            {
                using var cmKey = RegistryEx.GetRegistryKey($@"{shellExPath}\{part}");
                if (cmKey == null) continue;
                foreach (var keyName in cmKey.GetSubKeyNames())
                {
                    try
                    {
                        using var key = cmKey.OpenSubKey(keyName);
                        if (!GuidEx.TryParse(key.GetValue("")?.ToString(), out var guid))
                            GuidEx.TryParse(keyName, out guid);
                        if (!guid.Equals(Guid.Empty))
                            dic.Add(key.Name, guid);
                    }
                    catch { continue; }
                }
            }
            return dic;
        }

        public static readonly string[] DdhParts = { "DragDropHandlers", "-DragDropHandlers" };
        public static readonly string[] CmhParts = { "ContextMenuHandlers", "-ContextMenuHandlers" };
        public static readonly Guid LnkOpenGuid = new("00021401-0000-0000-c000-000000000046");

        public ShellExItem(Guid guid, string regPath)
        {
            InitializeComponents();
            Guid = guid;
            RegPath = regPath;
        }

        private string regPath;
        public string RegPath
        {
            get => regPath;
            set
            {
                regPath = value;
                Text = ItemText;
                Image = GuidInfo.GetImage(Guid);
            }
        }

        public Guid Guid { get; set; }
        public string ValueName => null;
        public string SearchText => Text;
        public string ItemFilePath => GuidInfo.GetFilePath(Guid);
        private string KeyName => RegistryEx.GetKeyName(RegPath);
        private string ParentPath => RegistryEx.GetParentPath(RegPath);
        private string ShellExPath => RegistryEx.GetParentPath(ParentPath);
        private string ParentKeyName => RegistryEx.GetKeyName(ParentPath);
        private string DefaultValue => Registry.GetValue(RegPath, "", null)?.ToString();
        public string ItemText => GuidInfo.GetText(Guid) ?? (KeyName.Equals(Guid.ToString("B"), StringComparison.OrdinalIgnoreCase) ? DefaultValue : KeyName);
        public bool IsDragDropItem => ParentKeyName.EndsWith(DdhParts[0], StringComparison.OrdinalIgnoreCase);

        private string BackupPath
        {
            get
            {
                var parts = IsDragDropItem ? DdhParts : CmhParts;
                return $@"{ShellExPath}\{(ItemVisible ? parts[1] : parts[0])}\{KeyName}";
            }
        }

        public bool ItemVisible // 是否显示于右键菜单中
        {
            get
            {
                var parts = IsDragDropItem ? DdhParts : CmhParts;
                return ParentKeyName.Equals(parts[0], StringComparison.OrdinalIgnoreCase);
            }
            set
            {
                try
                {
                    RegistryEx.MoveTo(RegPath, BackupPath);
                }
                catch
                {
                    AppMessageBox.Show(AppString.Message.AuthorityProtection);
                    return;
                }
                RegPath = BackupPath;
            }
        }

        public MenuButton BtnShowMenu { get; set; }
        public VisibleCheckBox ChkVisible { get; set; }
        public DetailedEditButton BtnDetailedEdit { get; set; }
        public WebSearchMenuItem TsiSearch { get; set; }
        public FilePropertiesMenuItem TsiFileProperties { get; set; }
        public FileLocationMenuItem TsiFileLocation { get; set; }
        public RegLocationMenuItem TsiRegLocation { get; set; }
        public DeleteMeMenuItem TsiDeleteMe { get; set; }
        public RegExportMenuItem TsiRegExport { get; set; }
        public HandleGuidMenuItem TsiHandleGuid { get; set; }

        private readonly RToolStripMenuItem TsiDetails = new(AppString.Menu.Details);

        private void InitializeComponents()
        {
            BtnShowMenu = new MenuButton(this);
            ChkVisible = new VisibleCheckBox(this);
            BtnDetailedEdit = new DetailedEditButton(this);
            TsiHandleGuid = new HandleGuidMenuItem(this);
            TsiSearch = new WebSearchMenuItem(this);
            TsiFileLocation = new FileLocationMenuItem(this);
            TsiFileProperties = new FilePropertiesMenuItem(this);
            TsiRegLocation = new RegLocationMenuItem(this);
            TsiRegExport = new RegExportMenuItem(this);
            TsiDeleteMe = new DeleteMeMenuItem(this);

            ContextMenuStrip.Items.AddRange(new ToolStripItem[] { TsiHandleGuid, new RToolStripSeparator(),
                TsiDetails, new RToolStripSeparator(), TsiDeleteMe });

            TsiDetails.DropDownItems.AddRange(new ToolStripItem[] { TsiSearch, new RToolStripSeparator(),
                TsiFileProperties, TsiFileLocation, TsiRegLocation, TsiRegExport});

            ContextMenuStrip.Opening += (sender, e) => TsiDeleteMe.Enabled = !(Guid.Equals(LnkOpenGuid) && AppConfig.ProtectOpenItem);
            ChkVisible.PreCheckChanging += TryProtectOpenItem;
        }

        public bool TryProtectOpenItem()
        {
            if (!ChkVisible.Checked || !Guid.Equals(LnkOpenGuid) || !AppConfig.ProtectOpenItem) return true;
            return AppMessageBox.Show(AppString.Message.PromptIsOpenItem, MessageBoxButtons.YesNo) == DialogResult.Yes;
        }

        public void DeleteMe()
        {
            RegistryEx.DeleteKeyTree(RegPath, true);
            RegistryEx.DeleteKeyTree(BackupPath);
        }
    }
}