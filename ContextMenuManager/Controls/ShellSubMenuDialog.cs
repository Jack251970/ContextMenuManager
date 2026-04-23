using ContextMenuManager.Controls.Interfaces;
using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace ContextMenuManager.Controls
{
    internal sealed partial class ShellSubMenuDialog
    {
        public System.Drawing.Icon Icon { get; set; }
        public string Text { get; set; }
        public string ParentPath { get; set; }

        public bool ShowDialog()
        {
            return RunDialog(null);
        }

        public bool RunDialog(MainWindow owner)
        {
            var isPublic = true;
            var value = Microsoft.Win32.Registry.GetValue(ParentPath, "SubCommands", null)?.ToString();
            if (value == null) isPublic = false;
            else if (string.IsNullOrWhiteSpace(value))
            {
                using var shellKey = RegistryEx.GetRegistryKey($@"{ParentPath}\shell");
                if (shellKey != null && shellKey.GetSubKeyNames().Length > 0) isPublic = false;
                else
                {
                    var dialog = new ContentDialog
                    {
                        Title = AppString.General.AppName,
                        PrimaryButtonText = AppString.Dialog.Public,
                        SecondaryButtonText = AppString.Dialog.Private,
                        CloseButtonText = AppString.Dialog.Cancel,
                        DefaultButton = ContentDialogButton.Primary,
                        Content = new TextBlock
                        {
                            Text = AppString.Message.SelectSubMenuMode
                        }
                    };

                    var result = ContentDialogHost.RunBlocking(dialog.ShowAsync, owner);

                    if (result == ContentDialogResult.Primary)
                    {
                        isPublic = true;
                    }
                    else if (result == ContentDialogResult.Secondary)
                    {
                        isPublic = false;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            var dialogTitle = Text;
            if (isPublic)
            {
                dialogTitle += $"({AppString.Dialog.Public})";
                var list = new PulicMultiItemsList
                {
                    ParentPath = ParentPath
                };
                list.LoadItems();
                return ShowListDialog(dialogTitle, list, owner);
            }
            else
            {
                dialogTitle += $"({AppString.Dialog.Private})";
                var list = new PrivateMultiItemsList
                {
                    ParentPath = ParentPath
                };
                list.LoadItems();
                return ShowListDialog(dialogTitle, list, owner);
            }
        }

        private static bool ShowListDialog(string title, MyList list, MainWindow owner)
        {
            var dialog = ContentDialogHost.CreateDialog(title, owner);
            dialog.IsPrimaryButtonEnabled = false;
            dialog.CloseButtonText = AppString.Dialog.OK;

            list.MinWidth = 500;
            list.MinHeight = 400;
            dialog.Content = list;

            ContentDialogHost.RunBlocking(dialog.ShowAsync, owner);
            return false;
        }

        private sealed class PulicMultiItemsList : MyList
        {
            private readonly List<string> SubKeyNames = [];
            /// <summary>子菜单的父菜单的注册表路径</summary>
            public string ParentPath { get; set; }
            /// <summary>菜单所处环境注册表路径</summary>
            private string ScenePath => RegistryEx.GetParentPath(RegistryEx.GetParentPath(ParentPath));

            private readonly SubNewItem subNewItem;

            public PulicMultiItemsList()
            {
                subNewItem = new(this, true);
            }

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
                    if (key != null) item = new SubShellItem(this, keyName, false);
                    else if (keyName == "|") item = new SeparatorItem(this);
                    else item = new InvalidItem(this, keyName);
                    AddItem(item);
                }
            }

            private void AddNewItem()
            {
                if (!SubShellTypeItem.CanAddMore(this)) return;
                var dlg = new NewShellDialog
                {
                    ScenePath = ScenePath,
                    ShellPath = ShellItem.CommandStorePath
                };
                if (!dlg.ShowDialog()) return;
                SubKeyNames.Add(dlg.NewItemKeyName);
                SaveSorting();
                AddItem(new SubShellItem(this, dlg.NewItemKeyName, false));
            }

            private void AddReference()
            {
                if (!SubShellTypeItem.CanAddMore(this)) return;
                var dlg = new ShellStoreDialog
                {
                    IsReference = true,
                    ShellPath = ShellItem.CommandStorePath,
                    Filter = new Func<string, bool>(itemName => !(AppConfig.HideSysStoreItems
                        && itemName.StartsWith("Windows.", StringComparison.OrdinalIgnoreCase)))
                };
                if (dlg.ShowDialog() != true) return;
                foreach (var keyName in dlg.SelectedKeyNames)
                {
                    if (!SubShellTypeItem.CanAddMore(this)) return;
                    AddItem(new SubShellItem(this, keyName, false));
                    SubKeyNames.Add(keyName);
                    SaveSorting();
                }
            }

            private void AddSeparator()
            {
                if (Controls[^1].Item is SeparatorItem) return;
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
                var nextIndex = index;
                if (index == Controls.Count - 1) nextIndex--;
                Controls.Remove(item.Control);
                Controls[nextIndex]?.Focus();
                SaveSorting();
                item.Dispose();
            }

            private sealed class SubShellItem : SubShellTypeItem
            {
                public SubShellItem(PulicMultiItemsList list, string keyName, bool isShellStoreDialog) : base(list, $@"{CommandStorePath}\{keyName}", isShellStoreDialog)
                {
                    List = list;
                    if (list != null)
                    {
                        TsiDeleteRef = new(AppString.Menu.DeleteReference);
                        BtnMoveUp.Click += (sender, e) => List.MoveItem(this, true);
                        BtnMoveDown.Click += (sender, e) => List.MoveItem(this, false);
                        ContextMenu.Items.Remove(TsiDeleteMe);
                        ContextMenu.Items.Add(TsiDeleteRef);
                        TsiDeleteRef.Click += (sender, e) => DeleteReference();
                    }
                }

                private readonly RToolStripMenuItem TsiDeleteRef;

                public new PulicMultiItemsList List;

                private void DeleteReference()
                {
                    if (AppMessageBox.Show(AppString.Message.ConfirmDeleteReference, null, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        List.DeleteItem(this);
                    }
                }
            }

            private sealed class SeparatorItem : SubSeparatorItem
            {
                public SeparatorItem(PulicMultiItemsList list) : base(list)
                {
                    List = list;
                    if (list != null)
                    {
                        BtnMoveUp.Click += (sender, e) => List.MoveItem(this, true);
                        BtnMoveDown.Click += (sender, e) => List.MoveItem(this, false);
                    }
                }

                public new PulicMultiItemsList List;

                public override void DeleteMe()
                {
                    List.DeleteItem(this);
                }
            }

            private sealed class InvalidItem : MyListItem, IBtnDeleteItem, IBtnMoveUpDownItem
            {
                public InvalidItem(PulicMultiItemsList list, string keyName) : base(list)
                {
                    List = list;
                    if (list != null)
                    {
                        Text = $"{AppString.Other.InvalidItem} {keyName}";
                        Image = AppImage.NotFound.ToTransparent();
                        BtnDelete = new DeleteButton(this);
                        BtnMoveDown = new MoveButton(this, false);
                        BtnMoveUp = new MoveButton(this, true);
                        BtnMoveUp.Click += (sender, e) => List.MoveItem(this, true);
                        BtnMoveDown.Click += (sender, e) => List.MoveItem(this, false);
                        ToolTipBox.SetToolTip(Control, AppString.Tip.InvalidItem);
                        ToolTipBox.SetToolTip(BtnDelete, AppString.Menu.Delete);
                    }
                }

                public DeleteButton BtnDelete { get; set; }
                public new PulicMultiItemsList List;
                public MoveButton BtnMoveUp { get; set; }
                public MoveButton BtnMoveDown { get; set; }

                public void DeleteMe()
                {
                    List.DeleteItem(this);
                }
            }
        }

        private sealed class PrivateMultiItemsList : MyList
        {
            private readonly SubNewItem subNewItem;

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

            public PrivateMultiItemsList()
            {
                subNewItem = new(this, false);
            }

            public void LoadItems()
            {
                AddItem(subNewItem);
                subNewItem.AddNewItem += AddNewItem;
                subNewItem.AddSeparator += AddSeparator;
                subNewItem.AddExisting += AddFromParentMenu;

                var sckValue = Microsoft.Win32.Registry.GetValue(ParentPath, "ExtendedSubCommandsKey", null)?.ToString();
                if (!string.IsNullOrWhiteSpace(sckValue))
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
                        AddItem(new SubShellItem(this, regPath, false));
                    }
                }
            }

            private void AddNewItem()
            {
                if (!SubShellTypeItem.CanAddMore(this)) return;
                var dlg = new NewShellDialog
                {
                    ScenePath = ScenePath,
                    ShellPath = ShellPath
                };
                if (!dlg.ShowDialog()) return;
                AddItem(new SubShellItem(this, dlg.NewItemRegPath, false));
            }

            private void AddSeparator()
            {
                if (Controls[^1].Item is SeparatorItem) return;
                string regPath;
                if (Controls.Count > 1)
                {
                    regPath = GetItemRegPath(Controls[^1].Item);
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
                var dlg = new ShellStoreDialog
                {
                    IsReference = false,
                    ShellPath = ParentShellPath,
                    Filter = new Func<string, bool>(itemName => !itemName.Equals(ParentKeyName, StringComparison.OrdinalIgnoreCase))
                };
                if (dlg.ShowDialog() != true) return;
                foreach (var keyName in dlg.SelectedKeyNames)
                {
                    if (!SubShellTypeItem.CanAddMore(this)) return;
                    var srcPath = $@"{dlg.ShellPath}\{keyName}";
                    var dstPath = ObjectPath.GetNewPathWithIndex($@"{ShellPath}\{keyName}", ObjectPath.PathType.Registry);

                    RegistryEx.CopyTo(srcPath, dstPath);
                    AddItem(new SubShellItem(this, dstPath, false));
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
                        otherItem = Controls[index - 1].Item;
                        SetItemIndex(item, index - 1);
                    }
                }
                else
                {
                    if (index < Controls.Count - 1)
                    {
                        otherItem = Controls[index + 1].Item;
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

            private static string GetItemRegPath(MyListItem item)
            {
                var pi = item.GetType().GetProperty("RegPath");
                return pi.GetValue(item, null).ToString();
            }

            private static void SetItemRegPath(MyListItem item, string regPath)
            {
                var pi = item.GetType().GetProperty("RegPath");
                pi.SetValue(item, regPath, null);
            }

            private sealed class SubShellItem : SubShellTypeItem
            {
                public SubShellItem(PrivateMultiItemsList list, string regPath, bool isShellStoreDialog) : base(list, regPath, isShellStoreDialog)
                {
                    List = list;
                    if (list != null)
                    {
                        BtnMoveUp.Click += (sender, e) => List.MoveItem(this, true);
                        BtnMoveDown.Click += (sender, e) => List.MoveItem(this, false);
                    }
                    SetItemTextValue();
                }

                public new PrivateMultiItemsList List;

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
                public SeparatorItem(PrivateMultiItemsList list, string regPath) : base(list)
                {
                    List = list;
                    RegPath = regPath;
                    if (list != null)
                    {
                        BtnMoveUp.Click += (sender, e) => List.MoveItem(this, true);
                        BtnMoveDown.Click += (sender, e) => List.MoveItem(this, false);
                    }
                }

                public new PrivateMultiItemsList List;

                public string RegPath { get; private set; }

                public override void DeleteMe()
                {
                    RegistryEx.DeleteKeyTree(RegPath);
                    var index = List.GetItemIndex(this);
                    if (index == List.Controls.Count - 1) index--;
                    List.Controls[index]?.Focus();
                    List.Controls.Remove(Control);
                    Dispose();
                }
            }
        }

        private class SubSeparatorItem : MyListItem, IBtnDeleteItem, IBtnMoveUpDownItem
        {
            public SubSeparatorItem(MyList list) : base(list)
            {
                if (list != null)
                {
                    Text = AppString.Other.Separator;
                    HasImage = false;
                    BtnDelete = new DeleteButton(this);
                    BtnMoveDown = new MoveButton(this, false);
                    BtnMoveUp = new MoveButton(this, true);
                    ToolTipBox.SetToolTip(BtnDelete, AppString.Menu.Delete);
                }
            }

            public DeleteButton BtnDelete { get; set; }
            public MoveButton BtnMoveUp { get; set; }
            public MoveButton BtnMoveDown { get; set; }

            public virtual void DeleteMe() { }
        }

        private class SubShellTypeItem : ShellItem, IBtnMoveUpDownItem
        {
            public SubShellTypeItem(MyList list, string regPath, bool isShellStoreDialog) : base(list, regPath, isShellStoreDialog)
            {
                if (list != null)
                {
                    BtnMoveDown = new MoveButton(this, false);
                    BtnMoveUp = new MoveButton(this, true);
                    SetCtrIndex(BtnMoveUp, 0);
                    SetCtrIndex(BtnMoveDown, 1);
                }
            }

            public MoveButton BtnMoveUp { get; set; }
            public MoveButton BtnMoveDown { get; set; }

            protected override bool IsSubItem => true;

            public static bool CanAddMore(MyList list)
            {
                var count = 0;
                foreach (var control in list.Controls)
                {
                    if (control.Item is SubShellTypeItem) count++;
                }
                var flag = count < 16;
                if (!flag) AppMessageBox.Show(AppString.Message.CannotAddNewItem);
                return flag;
            }
        }

        private sealed class SubNewItem : NewItem
        {
            public SubNewItem(MyList list, bool isPublic) : base(list)
            {
                if (list != null)
                {
                    btnAddExisting = new(AppGlyphs.AddExisting);
                    btnAddSeparator = new(AppGlyphs.AddSeparator);

                    AddCtrs([btnAddExisting, btnAddSeparator]);
                    ToolTipBox.SetToolTip(btnAddExisting, isPublic ? AppString.Tip.AddReference : AppString.Tip.AddFromParentMenu);
                    ToolTipBox.SetToolTip(btnAddSeparator, AppString.Tip.AddSeparator);
                    btnAddExisting.Click += (sender, e) => AddExisting?.Invoke();
                    btnAddSeparator.Click += (sender, e) => AddSeparator?.Invoke();
                }

            }

            private readonly GlyphButton btnAddExisting;
            private readonly GlyphButton btnAddSeparator;

            public Action AddExisting;
            public Action AddSeparator;
        }
    }
}
