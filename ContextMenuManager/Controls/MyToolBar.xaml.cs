using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using iNKORE.UI.WPF.Modern.Controls;

namespace ContextMenuManager.Controls
{
    public partial class MyToolBar : UserControl
    {
        private readonly HashSet<AppBarToggleButton> _nonSelectableButtons = new();
        private AppBarToggleButton _selectedButton;

        public MyToolBar()
        {
            InitializeComponent();
        }

        public AppBarToggleButton SelectedButton
        {
            get => _selectedButton;
            set
            {
                if (_selectedButton == value)
                {
                    if (_selectedButton != null)
                    {
                        _selectedButton.IsChecked = true;
                    }
                    return;
                }

                if (_selectedButton != null)
                {
                    _selectedButton.IsChecked = false;
                    _selectedButton.Cursor = Cursors.Hand;
                }

                _selectedButton = value;

                if (_selectedButton != null)
                {
                    _selectedButton.IsChecked = true;
                    _selectedButton.Cursor = Cursors.Arrow;
                }

                SelectedButtonChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public int SelectedIndex
        {
            get
            {
                if (_selectedButton == null) return -1;
                for (int i = 0; i < ButtonContainer.Children.Count; i++)
                {
                    if (ButtonContainer.Children[i] == _selectedButton) return i;
                }
                return -1;
            }
            set
            {
                if (value < 0 || value >= ButtonContainer.Children.Count)
                {
                    SelectedButton = null;
                }
                else
                {
                    SelectedButton = ButtonContainer.Children[value] as AppBarToggleButton;
                }
            }
        }

        public event EventHandler SelectedButtonChanged;

        public void AddButton(AppBarToggleButton button, bool canBeSelected = true)
        {
            button.Margin = new Thickness(12, 4, 0, 4);
            button.Width = 88;
            button.Height = 88;

            if (!canBeSelected)
            {
                _nonSelectableButtons.Add(button);
            }

            button.Click += (sender, e) =>
            {
                if (_nonSelectableButtons.Contains(button))
                {
                    button.IsChecked = false;
                    return;
                }

                SelectedButton = button;
            };

            ButtonContainer.Children.Add(button);
        }

        public void AddButtons(AppBarToggleButton[] buttons)
        {
            foreach (var button in buttons)
            {
                AddButton(button);
            }
        }

        public void AddSearchBox(UIElement searchBox)
        {
            SearchBoxHost.Content = searchBox;
        }
    }
}
