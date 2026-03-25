using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Windows;

namespace ContextMenuManager.Controls.Interfaces
{
    internal interface IChkVisibleItem
    {
        bool ItemVisible { get; set; }
        VisibleCheckBox ChkVisible { get; set; }
    }

    internal sealed class VisibleCheckBox : ToggleSwitch
    {
        public Action CheckChanged;

        public Func<bool> PreCheckChanging;

        private bool _loading;
        private bool _reverted;

        public VisibleCheckBox()
        {
            MinWidth = 0;
            Header = OnContent = OffContent = null;
            Toggled += VisibleCheckBox_Toggled;
        }

        public VisibleCheckBox(IChkVisibleItem item) : this()
        {
            var listItem = (MyListItem)item;
            listItem.AddCtr(this);

            listItem.Loaded += (s, e) =>
            {
                _loading = true;
                IsOn = item.ItemVisible;
                if (listItem is FoldSubItem subItem && subItem.FoldGroupItem != null) return;
                if (AppConfig.HideDisabledItems) listItem.Visible = IsOn;
                CheckChanged += () => item.ItemVisible = IsOn;
                _loading = false;
            };
        }

        private void VisibleCheckBox_Toggled(object sender, RoutedEventArgs e)
        {
            CheckChanged?.Invoke();
        }

        protected override void OnToggled()
        {
            // We are loading the initial state, just set the toggle without invoking CheckChanged
            if (_loading)
            {
                base.OnToggled();
                return;
            }

            // We are reverting the toggle state
            if (_reverted)
            {
                _reverted = false;
                return;
            }

            if (PreCheckChanging == null || PreCheckChanging())
            {
                base.OnToggled();
            }
            // Revert the toggle state and do not invoke OnToggled again
            else if (!_reverted)
            {
                _reverted = true;
                IsOn = !IsOn;
            }
        }
    }
}
