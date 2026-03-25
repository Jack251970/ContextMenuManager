using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ContextMenuManager.Methods;

namespace ContextMenuManager.Controls
{
    public class PictureButton : Button
    {
        public static readonly DependencyProperty BaseImageProperty =
            DependencyProperty.Register("BaseImage", typeof(System.Drawing.Image), typeof(PictureButton),
                new PropertyMetadata(null, OnBaseImageChanged));

        public System.Drawing.Image BaseImage
        {
            get => (System.Drawing.Image)GetValue(BaseImageProperty);
            set => SetValue(BaseImageProperty, value);
        }

        private readonly Image innerImage = new()
        {
            Stretch = Stretch.Uniform,
            Width = 27,
            Height = 27
        };

        public PictureButton(System.Drawing.Image image)
        {
            Background = Brushes.Transparent;
            BorderThickness = new Thickness(0);
            Padding = new Thickness(0);
            Width = 32;
            Height = 32;
            Content = innerImage;

            BaseImage = image;
        }

        private static void OnBaseImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PictureButton)d).UpdateImage();
        }

        private void UpdateImage()
        {
            if (BaseImage == null)
            {
                innerImage.Source = null;
            }
            else
            {
                innerImage.Source = BaseImage.ToBitmapSource();
            }
        }
    }
}
