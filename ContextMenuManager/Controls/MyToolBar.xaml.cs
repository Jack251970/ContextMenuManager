using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using iNKORE.UI.WPF.Modern.Controls;

namespace ContextMenuManager.Controls
{
    public partial class MyToolBar : UserControl
    {
        public const double SelectedOpacity = 0.8;
        public const double HoveredOpacity = 0.4;
        public const double UnselectedOpacity = 0.0;

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
                    _selectedButton.AnimateOpacity(UnselectedOpacity);
                    _selectedButton.Cursor = Cursors.Hand;
                }

                _selectedButton = value;

                if (_selectedButton != null)
                {
                    _selectedButton.AnimateOpacity(SelectedOpacity);
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
                {
                    button.AnimateOpacity(HoveredOpacity);
                }
            };

            button.MouseLeave += (sender, e) =>
            {
                if (button != SelectedButton)
                {
                    button.AnimateOpacity(UnselectedOpacity);
                }
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

    public class MyToolBarButton : ContentControl
    {
        private readonly Rectangle _highlightRect;
        private readonly TextBlock _labelText;

        public bool CanBeSelected { get; set; } = true;

        /// <summary>
        /// 使用 FontIcon glyph 创建工具栏按钮
        /// </summary>
        public MyToolBarButton(string glyph, string text)
        {
            Width = 72;
            Height = 72;
            Cursor = Cursors.Hand;
            Background = Brushes.Transparent;

            _highlightRect = new Rectangle
            {
                RadiusX = 10,
                RadiusY = 10,
                Fill = new SolidColorBrush(Colors.White),
                Opacity = 0,
                IsHitTestVisible = false
            };

            var icon = new FontIcon
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
                Margin = new Thickness(0, 4, 0, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
            };

            var contentPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            contentPanel.Children.Add(icon);
            contentPanel.Children.Add(_labelText);

            var grid = new Grid();
            grid.Children.Add(_highlightRect);
            grid.Children.Add(contentPanel);

            Content = grid;
        }

        public void AnimateOpacity(double targetOpacity)
        {
            var animation = new DoubleAnimation
            {
                To = targetOpacity,
                Duration = TimeSpan.FromMilliseconds(150),
                FillBehavior = FillBehavior.HoldEnd
            };

            _highlightRect.BeginAnimation(Rectangle.OpacityProperty, animation);
        }
    }
}
