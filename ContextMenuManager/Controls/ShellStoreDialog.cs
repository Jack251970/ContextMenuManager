using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace ContextMenuManager.Controls
{
    internal sealed partial class ShellStoreDialog
    {
        public string[] SelectedKeyNames { get; private set; }
        public Func<string, bool> Filter { get; set; }
        public string ShellPath { get; set; }
        public bool IsReference { get; set; }

        public bool ShowDialog() => RunDialog(null);

        public bool RunDialog(MainWindow owner)
        {
            var list = new MyList();
            var chkSelectAll = new CheckBox
            {
                Content = AppString.Dialog.SelectAll,
                Margin = new Thickness(10, 10, 0, 10),
                MinWidth = 0
            };

            var dialog = ContentDialogHost.CreateDialog(
                IsReference ? AppString.Dialog.CheckReference : AppString.Dialog.CheckCopy, 
                owner);

            var stackPanel = new StackPanel();
            stackPanel.Children.Add(chkSelectAll);
            list.MinHeight = 400;
            list.MinWidth = 500;
            stackPanel.Children.Add(list);
            dialog.Content = stackPanel;

            chkSelectAll.Click += (sender, e) =>
            {
                var flag = chkSelectAll.IsChecked == true;
                foreach (var ctrl in list.Controls)
                {
                    if (ctrl.Item is StoreShellItem item) item.IsSelected = flag;
                }
            };

            using (var shellKey = RegistryEx.GetRegistryKey(ShellPath))
            {
                foreach (var itemName in shellKey.GetSubKeyNames())
                {
                    if (Filter != null && !Filter(itemName)) continue;
                    var regPath = $@"{ShellPath}\{itemName}";
                    var item = new StoreShellItem(list, regPath, IsReference);
                    item.SelectedChanged += () =>
                    {
                        bool allSelected = true;
                        foreach (var ctrl in list.Controls)
                        {
                            if (ctrl.Item is StoreShellItem shellItem && !shellItem.IsSelected)
                            {
                                allSelected = false;
                                break;
                            }
                        }
                        chkSelectAll.IsChecked = allSelected;
                    };
                    list.AddItem(item);
                }
            }

            var result = ContentDialogHost.RunBlocking(dialog.ShowAsync, owner);
            if (result != ContentDialogResult.Primary) return false;

            var names = new List<string>();
            foreach (var ctrl in list.Controls)
            {
                if (ctrl.Item is StoreShellItem item && item.IsSelected) names.Add(item.KeyName);
            }
            SelectedKeyNames = [.. names];
            return true;
        }
    }

    internal sealed class StoreShellItem : ShellItem
    {
        public StoreShellItem(MyList list, string regPath, bool isPublic, bool isSelect = true) : base(list, regPath)
        {
            IsPublic = isPublic;
            if (list != null)
            {
                chkSelected = new()
                {
                    MinWidth = 0
                };
                if (isSelect)
                {
                    ContextMenu = null;
                    AddCtr(chkSelected);
                    ChkVisible.Visibility = BtnShowMenu.Visibility = BtnSubItems.Visibility = Visibility.Collapsed;
                    Control.MouseLeftButtonUp += (sender, e) => chkSelected.IsChecked = !chkSelected.IsChecked;
                    chkSelected.Checked += (sender, e) => SelectedChanged?.Invoke();
                    chkSelected.Unchecked += (sender, e) => SelectedChanged?.Invoke();
                }
            }
            RegTrustedInstaller.TakeRegTreeOwnerShip(regPath);
        }

        public bool IsPublic { get; set; }
        public bool IsSelected
        {
            get => chkSelected.IsChecked == true;
            set => chkSelected.IsChecked = value;
        }

        private readonly CheckBox chkSelected;

        public Action SelectedChanged;

        public override void DeleteMe()
        {
            if (IsPublic && AppMessageBox.Show(AppString.Message.ConfirmDeleteReferenced, null,
                MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            base.DeleteMe();
        }
    }
}
