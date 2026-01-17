using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Controls.Interfaces;
using ContextMenuManager.Methods;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    internal sealed class ShellSubMenuDialog : CommonDialog
    {
        public Icon Icon { get; set; }
        public string Text { get; set; }
        /// <summary>子菜单的父菜单的注册表路径</summary>
        public string ParentPath { get; set; }
        public override void Reset() { }

        protected override bool RunDialog(IntPtr hwndOwner)
        {
            var isPublic = true;
            var value = Microsoft.Win32.Registry.GetValue(ParentPath, "SubCommands", null)?.ToString();
            if (value == null) isPublic = false;
            else if (value.IsNullOrWhiteSpace())
            {
                using var shellKey = RegistryEx.GetRegistryKey($@"{ParentPath}\shell");
                if (shellKey != null && shellKey.GetSubKeyNames().Length > 0) isPublic = false;
                else
                {
                    var modes = new[] { ResourceString.Cancel, AppString.Dialog.Private, AppString.Dialog.Public };
                    var mode = MessageBoxEx.Show(AppString.Message.SelectSubMenuMode, AppString.General.AppName,
                        modes, MessageBoxImage.Question, null, modes[1]);
                    if (mode == modes[2]) isPublic = true;
                    else if (mode == modes[1]) isPublic = false;
                    else return false;
                }
            }

            using var frm = new SubItemsForm();
            frm.Text = Text;
            frm.Icon = Icon;
            frm.TopMost = true;

            if (isPublic)
            {
                frm.Text += $"({AppString.Dialog.Public})";
                var list = new PulicMultiItemsList();
                frm.AddList(list);
                list.ParentPath = ParentPath;
                list.LoadItems();
            }
            else
            {
                frm.Text += $"({AppString.Dialog.Private})";
                var list = new PrivateMultiItemsList();
                frm.AddList(list);
                list.ParentPath = ParentPath;
                list.LoadItems();
            }

            frm.ShowDialog();
            return false;
        }

        private sealed class PulicMultiItemsList : MyList
        {
            private readonly List<string> SubKeyNames = new();
            /// <summary>子菜单的父菜单的注册表路径</summary>
            public string ParentPath { get; set; }
            /// <summary>菜单所处环境注册表路径</summary>
            private string ScenePath => RegistryEx.GetParentPath(RegistryEx.GetParentPath(ParentPath));

            private readonly SubNewItem subNewItem = new(true);

            /// <param name="parentPath">子菜单的父菜单的注册表路径</param>
            public void LoadItems()
            {
                AddItem(subNewItem);
                subNewItem.AddNewItem += () => AddNewItem();
                subNewItem.AddExisting += () => AddReference();
                subNewItem.AddSeparator += () => AddSeparator();

                var value = Microsoft.Win32.Registry.GetValue(ParentPath, "SubCommands", null)?.ToString();
                Array.ForEach(value.Split(';'), cmd => SubKeyNames.Add(cmd.TrimStart()));
                SubKeyNames.RemoveAll(string.IsNullOrEmpty);

                using var shellKey = RegistryEx.GetRegistryKey(ShellItem.CommandStorePath, false, true);
                foreach (var keyName in SubKeyNames)
                {
                    using var key = shellKey.OpenSubKey(keyName);
                    MyListItem item;
                    if (key != null) item = new SubShellItem(this, keyName);
                    else if (keyName == "|") item = new SeparatorItem(this);
                    else item = new InvalidItem(this, keyName);
                    AddItem(item);
                }
            }

            private void AddNewItem()
            {
                if (!SubShellTypeItem.CanAddMore(this)) return;
                using var dlg = new NewShellDialog();
                dlg.ScenePath = ScenePath;
                dlg.ShellPath = ShellItem.CommandStorePath;
                if (dlg.ShowDialog() != DialogResult.OK) return;
                SubKeyNames.Add(dlg.NewItemKeyName);
                SaveSorting();
                AddItem(new SubShellItem(this, dlg.NewItemKeyName));
            }

            private void AddReference()
            {
                if (!SubShellTypeItem.CanAddMore(this)) return;
                using var dlg = new ShellStoreDialog();
                dlg.IsReference = true;
                dlg.ShellPath = ShellItem.CommandStorePath;
                dlg.Filter = new Func<string, bool>(itemName => !(AppConfig.HideSysStoreItems
                    && itemName.StartsWith("Windows.", StringComparison.OrdinalIgnoreCase)));
                if (dlg.ShowDialog() != DialogResult.OK) return;
                foreach (var keyName in dlg.SelectedKeyNames)
                {
                    if (!SubShellTypeItem.CanAddMore(this)) return;
                    AddItem(new SubShellItem(this, keyName));
                    SubKeyNames.Add(keyName);
                    SaveSorting();
                }
            }

            private void AddSeparator()
            {
                if (Controls[Controls.Count - 1] is SeparatorItem) return;
                SubKeyNames.Add("|");
                SaveSorting();
                AddItem(new SeparatorItem(this));
            }

            private void SaveSorting()
            {
                Microsoft.Win32.Registry.SetValue(ParentPath, "SubCommands", string.Join(";", SubKeyNames.ToArray()));
            }

            private void MoveItem(MyListItem item, bool isUp)
            {
                var index = GetItemIndex(item);
                if (isUp)
                {
                    if (index > 1)
                    {
                        SetItemIndex(item, index - 1);
                        SubKeyNames.Reverse(index - 2, 2);
                    }
                }
                else
                {
                    if (index < Controls.Count - 1)
                    {
                        SetItemIndex(item, index + 1);
                        SubKeyNames.Reverse(index - 1, 2);
                    }
                }
                SaveSorting();
            }

            private void DeleteItem(MyListItem item)
            {
                var index = GetItemIndex(item);
                SubKeyNames.RemoveAt(index - 1);
                if (index == Controls.Count - 1) index--;
                Controls.Remove(item);
                Controls[index].Focus();
                SaveSorting();
                item.Dispose();
            }

            private sealed class SubShellItem : SubShellTypeItem
            {
                public SubShellItem(PulicMultiItemsList list, string keyName) : base($@"{CommandStorePath}\{keyName}")
                {
                    Owner = list;
                    BtnMoveUp.MouseDown += (sender, e) => Owner.MoveItem(this, true);
                    BtnMoveDown.MouseDown += (sender, e) => Owner.MoveItem(this, false);
                    ContextMenuStrip.Items.Remove(TsiDeleteMe);
                    ContextMenuStrip.Items.Add(TsiDeleteRef);
                    TsiDeleteRef.Click += (sender, e) => DeleteReference();
                }

                private readonly RToolStripMenuItem TsiDeleteRef = new(AppString.Menu.DeleteReference);
                public PulicMultiItemsList Owner { get; private set; }

                private void DeleteReference()
                {
                    if (AppMessageBox.Show(AppString.Message.ConfirmDeleteReference, MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        Owner.DeleteItem(this);
                    }
                }
            }

            private sealed class SeparatorItem : SubSeparatorItem
            {
                public SeparatorItem(PulicMultiItemsList list) : base()
                {
                    Owner = list;
                    BtnMoveUp.MouseDown += (sender, e) => Owner.MoveItem(this, true);
                    BtnMoveDown.MouseDown += (sender, e) => Owner.MoveItem(this, false);
                }

                public PulicMultiItemsList Owner { get; private set; }

                public override void DeleteMe()
                {
                    Owner.DeleteItem(this);
                }
            }

            private sealed class InvalidItem : MyListItem, IBtnDeleteItem, IBtnMoveUpDownItem
            {
                public InvalidItem(PulicMultiItemsList list, string keyName)
                {
                    Owner = list;
                    Text = $"{AppString.Other.InvalidItem} {keyName}";
                    Image = AppImage.NotFound.ToTransparent();
                    BtnDelete = new DeleteButton(this);
                    BtnMoveDown = new MoveButton(this, false);
                    BtnMoveUp = new MoveButton(this, true);
                    BtnMoveUp.MouseDown += (sender, e) => Owner.MoveItem(this, true);
                    BtnMoveDown.MouseDown += (sender, e) => Owner.MoveItem(this, false);
                    ToolTipBox.SetToolTip(this, AppString.Tip.InvalidItem);
                    ToolTipBox.SetToolTip(BtnDelete, AppString.Menu.Delete);
                }

                public DeleteButton BtnDelete { get; set; }
                public PulicMultiItemsList Owner { get; private set; }
                public MoveButton BtnMoveUp { get; set; }
                public MoveButton BtnMoveDown { get; set; }

                public void DeleteMe()
                {
                    Owner.DeleteItem(this);
                }
            }
        }

        private sealed class PrivateMultiItemsList : MyList
        {
            private readonly SubNewItem subNewItem = new(false);

            /// <summary>父菜单的注册表路径</summary>
            public string ParentPath { get; set; }
            /// <summary>子菜单的Shell项注册表路径</summary>
            private string ShellPath { get; set; }
            /// <summary>父菜单的Shell项注册表路径</summary>
            private string ParentShellPath => RegistryEx.GetParentPath(ParentPath);
            /// <summary>菜单所处环境注册表路径</summary>
            private string ScenePath => RegistryEx.GetParentPath(ParentShellPath);
            /// <summary>父菜单的项名</summary>
            private string ParentKeyName => RegistryEx.GetKeyName(ParentPath);

            public void LoadItems()
            {
                AddItem(subNewItem);
                subNewItem.AddNewItem += () => AddNewItem();
                subNewItem.AddSeparator += () => AddSeparator();
                subNewItem.AddExisting += () => AddFromParentMenu();

                var sckValue = Microsoft.Win32.Registry.GetValue(ParentPath, "ExtendedSubCommandsKey", null)?.ToString();
                if (!sckValue.IsNullOrWhiteSpace())
                {
                    ShellPath = $@"{RegistryEx.CLASSES_ROOT}\{sckValue}\shell";
                }
                else
                {
                    ShellPath = $@"{ParentPath}\shell";
                }
                using var shellKey = RegistryEx.GetRegistryKey(ShellPath);
                if (shellKey == null) return;
                RegTrustedInstaller.TakeRegTreeOwnerShip(shellKey.Name);
                foreach (var keyName in shellKey.GetSubKeyNames())
                {
                    var regPath = $@"{ShellPath}\{keyName}";
                    var value = Convert.ToInt32(Microsoft.Win32.Registry.GetValue(regPath, "CommandFlags", 0));
                    if (value % 16 >= 8)
                    {
                        AddItem(new SeparatorItem(this, regPath));
                    }
                    else
                    {
                        AddItem(new SubShellItem(this, regPath));
                    }
                }
            }

            private void AddNewItem()
            {
                if (!SubShellTypeItem.CanAddMore(this)) return;
                using var dlg = new NewShellDialog
                {
                    ScenePath = ScenePath,
                    ShellPath = ShellPath
                };
                if (dlg.ShowDialog() != DialogResult.OK) return;
                AddItem(new SubShellItem(this, dlg.NewItemRegPath));
            }

            private void AddSeparator()
            {
                if (Controls[Controls.Count - 1] is SeparatorItem) return;
                string regPath;
                if (Controls.Count > 1)
                {
                    regPath = GetItemRegPath((MyListItem)Controls[Controls.Count - 1]);
                }
                else
                {
                    regPath = $@"{ShellPath}\Item";
                }
                regPath = ObjectPath.GetNewPathWithIndex(regPath, ObjectPath.PathType.Registry);
                Microsoft.Win32.Registry.SetValue(regPath, "CommandFlags", 0x8);
                AddItem(new SeparatorItem(this, regPath));
            }

            private void AddFromParentMenu()
            {
                if (!SubShellTypeItem.CanAddMore(this)) return;
                using var dlg = new ShellStoreDialog();
                dlg.IsReference = false;
                dlg.ShellPath = ParentShellPath;
                dlg.Filter = new Func<string, bool>(itemName => !itemName.Equals(ParentKeyName, StringComparison.OrdinalIgnoreCase));
                if (dlg.ShowDialog() != DialogResult.OK) return;
                foreach (var keyName in dlg.SelectedKeyNames)
                {
                    if (!SubShellTypeItem.CanAddMore(this)) return;
                    var srcPath = $@"{dlg.ShellPath}\{keyName}";
                    var dstPath = ObjectPath.GetNewPathWithIndex($@"{ShellPath}\{keyName}", ObjectPath.PathType.Registry);

                    RegistryEx.CopyTo(srcPath, dstPath);
                    AddItem(new SubShellItem(this, dstPath));
                }
            }

            public void MoveItem(MyListItem item, bool isUp)
            {
                var index = GetItemIndex(item);
                MyListItem otherItem = null;
                if (isUp)
                {
                    if (index > 1)
                    {
                        otherItem = (MyListItem)Controls[index - 1];
                        SetItemIndex(item, index - 1);
                    }
                }
                else
                {
                    if (index < Controls.Count - 1)
                    {
                        otherItem = (MyListItem)Controls[index + 1];
                        SetItemIndex(item, index + 1);
                    }
                }
                if (otherItem != null)
                {
                    var path1 = GetItemRegPath(item);
                    var path2 = GetItemRegPath(otherItem);
                    var tempPath = ObjectPath.GetNewPathWithIndex(path1, ObjectPath.PathType.Registry);
                    RegistryEx.MoveTo(path1, tempPath);
                    RegistryEx.MoveTo(path2, path1);
                    RegistryEx.MoveTo(tempPath, path2);
                    SetItemRegPath(item, path2);
                    SetItemRegPath(otherItem, path1);
                }
            }

            private string GetItemRegPath(MyListItem item)
            {
                var pi = item.GetType().GetProperty("RegPath");
                return pi.GetValue(item, null).ToString();
            }

            private void SetItemRegPath(MyListItem item, string regPath)
            {
                var pi = item.GetType().GetProperty("RegPath");
                pi.SetValue(item, regPath, null);
            }

            private sealed class SubShellItem : SubShellTypeItem
            {
                public SubShellItem(PrivateMultiItemsList list, string regPath) : base(regPath)
                {
                    Owner = list;
                    BtnMoveUp.MouseDown += (sender, e) => Owner.MoveItem(this, true);
                    BtnMoveDown.MouseDown += (sender, e) => Owner.MoveItem(this, false);
                    SetItemTextValue();
                }

                public PrivateMultiItemsList Owner { get; private set; }

                private void SetItemTextValue()
                {
                    using var key = RegistryEx.GetRegistryKey(RegPath, true);
                    var hasValue = false;
                    foreach (var valueName in new[] { "MUIVerb", "" })
                    {
                        if (key.GetValue(valueName) != null)
                        {
                            hasValue = true; break;
                        }
                    }
                    if (!hasValue) key.SetValue("MUIVerb", ItemText);

                }
            }

            private sealed class SeparatorItem : SubSeparatorItem
            {
                public SeparatorItem(PrivateMultiItemsList list, string regPath)
                {
                    Owner = list;
                    RegPath = regPath;
                    BtnMoveUp.MouseDown += (sender, e) => Owner.MoveItem(this, true);
                    BtnMoveDown.MouseDown += (sender, e) => Owner.MoveItem(this, false);
                }

                public PrivateMultiItemsList Owner { get; private set; }
                public string RegPath { get; private set; }

                public override void DeleteMe()
                {
                    RegistryEx.DeleteKeyTree(RegPath);
                    var index = Parent.Controls.GetChildIndex(this);
                    if (index == Parent.Controls.Count - 1) index--;
                    Parent.Controls[index].Focus();
                    Parent.Controls.Remove(this);
                    Dispose();
                }
            }
        }

        private class SubSeparatorItem : MyListItem, IBtnDeleteItem, IBtnMoveUpDownItem
        {
            public SubSeparatorItem()
            {
                Text = AppString.Other.Separator;
                HasImage = false;
                BtnDelete = new DeleteButton(this);
                BtnMoveDown = new MoveButton(this, false);
                BtnMoveUp = new MoveButton(this, true);
                ToolTipBox.SetToolTip(BtnDelete, AppString.Menu.Delete);
            }

            public DeleteButton BtnDelete { get; set; }
            public MoveButton BtnMoveUp { get; set; }
            public MoveButton BtnMoveDown { get; set; }

            public virtual void DeleteMe() { }
        }

        private class SubShellTypeItem : ShellItem, IBtnMoveUpDownItem
        {
            public SubShellTypeItem(string regPath) : base(regPath)
            {
                BtnMoveDown = new MoveButton(this, false);
                BtnMoveUp = new MoveButton(this, true);
                SetCtrIndex(BtnMoveDown, 1);
                SetCtrIndex(BtnMoveUp, 2);
            }

            public MoveButton BtnMoveUp { get; set; }
            public MoveButton BtnMoveDown { get; set; }

            protected override bool IsSubItem => true;

            public static bool CanAddMore(MyList list)
            {
                var count = 0;
                foreach (Control item in list.Controls)
                {
                    if (item.GetType().BaseType == typeof(SubShellTypeItem)) count++;
                }
                var flag = count < 16;
                if (!flag) AppMessageBox.Show(AppString.Message.CannotAddNewItem);
                return flag;
            }
        }

        private sealed class SubNewItem : NewItem
        {
            public SubNewItem(bool isPublic)
            {
                AddCtrs(new[] { btnAddExisting, btnAddSeparator });
                ToolTipBox.SetToolTip(btnAddExisting, isPublic ? AppString.Tip.AddReference : AppString.Tip.AddFromParentMenu);
                ToolTipBox.SetToolTip(btnAddSeparator, AppString.Tip.AddSeparator);
                btnAddExisting.MouseDown += (sender, e) => AddExisting?.Invoke();
                btnAddSeparator.MouseDown += (sender, e) => AddSeparator?.Invoke();
            }

            private readonly PictureButton btnAddExisting = new(AppImage.AddExisting);
            private readonly PictureButton btnAddSeparator = new(AppImage.AddSeparator);

            public Action AddExisting;
            public Action AddSeparator;
        }
    }
}