using ContextMenuManager.Controls.Interfaces;
using ContextMenuManager.Methods;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Controls;

namespace ContextMenuManager.Controls
{
    internal sealed class OpenWithItem : MyListItem, IChkVisibleItem, IBtnShowMenuItem, ITsiTextItem,
        ITsiCommandItem, ITsiWebSearchItem, ITsiFilePathItem, ITsiRegPathItem, ITsiRegDeleteItem, ITsiRegExportItem, ISearchable
    {
        public ContextMenu ContextMenu
        {
            get => Control.ContextMenu;
            set => Control.ContextMenu = value;
        }

        public OpenWithItem(MyList list, string regPath) : base(list)
        {
            RegPath = regPath;
            if (list != null) InitializeComponents();
        }

        private string regPath;
        public string RegPath
        {
            get => regPath;
            set
            {
                regPath = value;
                ItemFilePath = ObjectPath.ExtractFilePath(ItemCommand);
                Text = ResourceString.StripMnemonics(ItemText);
                if (List != null) Image = ItemIcon.ToBitmap();
            }
        }
        public string ValueName => null;
        private string ShellPath => RegistryEx.GetParentPath(RegPath);
        private string AppPath => RegistryEx.GetParentPath(RegistryEx.GetParentPath(ShellPath));
        private bool NameEquals => RegistryEx.GetKeyName(AppPath).Equals(Path.GetFileName(ItemFilePath), StringComparison.OrdinalIgnoreCase);
        private Icon ItemIcon => Icon.ExtractAssociatedIcon(ItemFilePath);

        public string ItemText
        {
            get
            {
                string name = null;
                if (NameEquals)
                {
                    name = Registry.GetValue(AppPath, "FriendlyAppName", null)?.ToString();
                    name = ResourceString.GetDirectString(name);
                }
                if (string.IsNullOrEmpty(name)) name = FileVersionInfo.GetVersionInfo(ItemFilePath).FileDescription;
                if (string.IsNullOrEmpty(name)) name = Path.GetFileName(ItemFilePath);
                return name;
            }
            set
            {
                Registry.SetValue(AppPath, "FriendlyAppName", value);
                Text = ResourceString.StripMnemonics(ResourceString.GetDirectString(value));
            }
        }

        public string ItemCommand
        {
            get => Registry.GetValue(RegPath, "", null)?.ToString();
            set
            {
                if (ObjectPath.ExtractFilePath(value) != ItemFilePath)
                {
                    AppMessageBox.Show(AppString.Message.CannotChangePath);
                }
                else Registry.SetValue(RegPath, "", value);
            }
        }

        public bool ItemVisible
        {
            get => Registry.GetValue(AppPath, "NoOpenWith", null) == null;
            set
            {
                if (value) RegistryEx.DeleteValue(AppPath, "NoOpenWith");
                else Registry.SetValue(AppPath, "NoOpenWith", "");
            }
        }

        public string SearchText => $"{AppString.SideBar.OpenWith} {Text}";
        public string ItemFilePath { get; private set; }
        public string ItemFileName => Path.GetFileName(ItemFilePath);

        public VisibleCheckBox ChkVisible { get; set; }
        public MenuButton BtnShowMenu { get; set; }
        public ChangeTextMenuItem TsiChangeText { get; set; }
        public ChangeCommandMenuItem TsiChangeCommand { get; set; }
        public WebSearchMenuItem TsiSearch { get; set; }
        public FilePropertiesMenuItem TsiFileProperties { get; set; }
        public FileLocationMenuItem TsiFileLocation { get; set; }
        public RegLocationMenuItem TsiRegLocation { get; set; }
        public DeleteMeMenuItem TsiDeleteMe { get; set; }
        public RegExportMenuItem TsiRegExport { get; set; }

        private RToolStripMenuItem TsiDetails { get; set; }

        private void InitializeComponents()
        {
            BtnShowMenu = new MenuButton(this);
            ChkVisible = new VisibleCheckBox(this);
            TsiChangeText = new ChangeTextMenuItem(this);
            TsiChangeCommand = new ChangeCommandMenuItem(this);
            TsiSearch = new WebSearchMenuItem(this);
            TsiFileProperties = new FilePropertiesMenuItem(this);
            TsiFileLocation = new FileLocationMenuItem(this);
            TsiRegLocation = new RegLocationMenuItem(this);
            TsiRegExport = new RegExportMenuItem(this);
            TsiDeleteMe = new DeleteMeMenuItem(this);
            TsiDetails = new(AppString.Menu.Details);

            foreach (var item in new Control[] { TsiChangeText,
                new RToolStripSeparator(), TsiDetails, new RToolStripSeparator(), TsiDeleteMe })
            {
                Control.ContextMenu.Items.Add(item);
            }

            foreach (var item in new Control[] { TsiSearch, new RToolStripSeparator(),
                TsiChangeCommand, TsiFileProperties, TsiFileLocation, TsiRegLocation, TsiRegExport })
            {
                TsiDetails.Items.Add(item);
            }

            ContextMenu.Opened += (sender, e) => TsiChangeText.IsEnabled = NameEquals;
        }

        public void DeleteMe()
        {
            RegistryEx.DeleteKeyTree(RegPath);
            using var key = RegistryEx.GetRegistryKey(ShellPath);
            if (key.GetSubKeyNames().Length == 0) RegistryEx.DeleteKeyTree(AppPath);
        }

        public string[] GetSearchKeywords()
        {
            var keywords = new List<string>();

            if (!string.IsNullOrEmpty(RegPath))
            {
                keywords.Add(RegPath);
            }

            if (!string.IsNullOrEmpty(ItemFilePath))
            {
                keywords.Add(ItemFilePath);
                var fileName = Path.GetFileName(ItemFilePath);
                if (!string.IsNullOrEmpty(fileName))
                {
                    keywords.Add(fileName);
                    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    if (!string.IsNullOrEmpty(fileNameWithoutExt))
                    {
                        keywords.Add(fileNameWithoutExt);
                    }
                }
            }

            if (!string.IsNullOrEmpty(ItemCommand))
            {
                keywords.Add(ItemCommand);
            }

            return keywords.ToArray();
        }

        public int GetSearchPriority()
        {
            return ItemVisible ? 10 : 0;
        }
    }
}