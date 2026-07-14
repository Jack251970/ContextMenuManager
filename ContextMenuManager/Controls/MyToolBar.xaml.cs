using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using iNKORE.UI.WPF.Modern;
using iNKORE.UI.WPF.Modern.Controls;

namespace ContextMenuManager.Controls
{
    public partial class MyToolBar : UserControl
    {
        private MyToolBarButton _selectedButton;

        public MyToolBar()
        {
            InitializeComponent();
        }

        public MyToolBarButton SelectedButton
        {
            get => _selectedButton;
            set
            {
                if (_selectedButton == value) return;

                if (_selectedButton != null)
                {
                    _selectedButton.SetState(ButtonState.Normal);
                    _selectedButton.Cursor = Cursors.Hand;
                }

                _selectedButton = value;

                if (_selectedButton != null)
                {
                    _selectedButton.SetState(ButtonState.Selected);
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
                    SelectedButton = ButtonContainer.Children[value] as MyToolBarButton;
                }
            }
        }

        public event EventHandler SelectedButtonChanged;

        public void AddButton(MyToolBarButton button)
        {
            button.Margin = new Thickness(12, 4, 0, 0);

            button.MouseDown += (sender, e) =>
            {
                if (e.ChangedButton == MouseButton.Left && button.CanBeSelected)
                {
                    SelectedButton = button;
                }
            };

            button.MouseEnter += (sender, e) =>
            {
                if (button != SelectedButton)
                    button.SetState(ButtonState.Hover);
                else
                    button.SetState(ButtonState.SelectedHover);
            };

            button.MouseLeave += (sender, e) =>
            {
                if (button != SelectedButton)
                    button.SetState(ButtonState.Normal);
                else
                    button.SetState(ButtonState.Selected);
            };

            ButtonContainer.Children.Add(button);
        }

        public void AddButtons(MyToolBarButton[] buttons)
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

    public enum ButtonState
    {
        Normal,
        Hover,
        Selected,
        SelectedHover
    }

    public class MyToolBarButton : ContentControl
    {
        private readonly Border _backgroundBorder;
        private readonly TextBlock _labelText;
        private readonly FontIcon _icon;

        public bool CanBeSelected { get; set; } = true;

        public MyToolBarButton(string glyph, string text)
        {
            Width = 72;
            Height = 72;
            Cursor = Cursors.Hand;
            Background = Brushes.Transparent;

            _backgroundBorder = new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = Brushes.Transparent,
                IsHitTestVisible = false
            };

            _icon = new FontIcon
            {
                Glyph = glyph,
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 6, 0, 0)
            };

            _labelText = new TextBlock
            {
                Text = text,
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            };

            var contentPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            contentPanel.Children.Add(_icon);
            contentPanel.Children.Add(_labelText);

            var grid = new Grid();
            grid.Children.Add(_backgroundBorder);
            grid.Children.Add(contentPanel);

            Content = grid;

            UpdateColors();
            ThemeManager.Current.ActualApplicationThemeChanged += (_, _) => UpdateColors();
        }

        public void SetState(ButtonState state)
        {
            var isDark = ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark;
            var accent = GetSystemAccentColor();
            var accentBrush = new SolidColorBrush(accent);
            var accentDarkBrush = new SolidColorBrush(DarkenColor(accent, 0.85));

            Brush bg;
            Brush fg;

            switch (state)
            {
                case ButtonState.Hover:
                    bg = isDark
                        ? new SolidColorBrush(Color.FromRgb(45, 45, 45))
                        : new SolidColorBrush(Color.FromRgb(238, 238, 238));
                    fg = isDark ? Brushes.White : new SolidColorBrush(Color.FromRgb(51, 51, 51));
                    break;

                case ButtonState.Selected:
                case ButtonState.SelectedHover:
                    bg = state == ButtonState.SelectedHover ? accentDarkBrush : accentBrush;
                    fg = Brushes.White;
                    break;

                default: // Normal
                    bg = Brushes.Transparent;
                    fg = isDark ? Brushes.White : new SolidColorBrush(Color.FromRgb(51, 51, 51));
                    break;
            }

            AnimateBackground(bg);
            _labelText.Foreground = fg;
            _icon.Foreground = fg;
        }

        private void AnimateBackground(Brush targetBrush)
        {
            // If current background is transparent and target is not, fade in
            // If current is solid and target is transparent, fade out
            // If both are solid, crossfade would be ideal but simple swap is acceptable for WinUI style
            _backgroundBorder.Background = targetBrush;
        }

        private void UpdateColors()
        {
            // Re-apply current state with new theme colors
            SetState(ButtonState.Normal);
        }

        private static Color GetSystemAccentColor()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
                if (key != null)
                {
                    var value = key.GetValue("AccentColor");
                    if (value is int intValue)
                    {
                        // DWORD is in BGR format
                        byte b = (byte)(intValue & 0xFF);
                        byte g = (byte)((intValue >> 8) & 0xFF);
                        byte r = (byte)((intValue >> 16) & 0xFF);
                        return Color.FromRgb(r, g, b);
                    }
                }
            }
            catch { }
            // Fallback to default Windows 11 blue
            return Color.FromRgb(0, 120, 212);
        }

        private static Color DarkenColor(Color color, double factor)
        {
            return Color.FromRgb(
                (byte)(color.R * factor),
                (byte)(color.G * factor),
                (byte)(color.B * factor));
        }
    }
}
