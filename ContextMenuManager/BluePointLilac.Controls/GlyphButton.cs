using FluentIcons.Common;
using FluentIcons.Wpf;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ContextMenuManager.Controls
{
    public class GlyphButton : Button
    {
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(Icon), typeof(GlyphButton),
                new PropertyMetadata(default(Icon), OnIconChanged));

        public Icon Icon
        {
            get => (Icon)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        private readonly FluentIcon innerIcon = new() { IconSize = IconSize.Size20, FontSize = 24 };

        public GlyphButton(Icon icon)
        {
            // iNKORE.UI.WPF.Modern's Button style is registered as an implicit Style TargetType="Button",
            // which WPF only matches on exact type — never on subclasses. Pull it in explicitly.
            SetResourceReference(StyleProperty, typeof(Button));

            Background = Brushes.Transparent;
            BorderThickness = new Thickness(0);
            Padding = new Thickness(0);
            Width = 32;
            Height = 32;
            Content = innerIcon;

            Icon = icon;
        }

        private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((GlyphButton)d).innerIcon.Icon = (Icon)e.NewValue;
        }
    }
}
