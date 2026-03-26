using ContextMenuManager.Methods;
using ContextMenuManager.Properties;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace ContextMenuManager.Views
{
    public partial class DonateView : UserControl
    {
        private static readonly Bitmap AllQr = new(AppResources.Donate);
        private static readonly Bitmap WechatQr = CropQr(0);
        private static readonly Bitmap AlipayQr = CropQr(1);
        private static readonly Bitmap QqQr = CropQr(2);

        public DonateView()
        {
            InitializeComponent();
            RefreshContent();
        }

        public void RefreshContent()
        {
            DonateInfoText.Text = AppString.Other.Donate;
            SetQrImage(AllQr);
        }

        private void QrImage_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (QrImage.Source is null)
            {
                return;
            }

            if (ReferenceEquals(GetCurrentBitmap(), AllQr))
            {
                var position = e.GetPosition(QrImage);
                var third = QrImage.ActualWidth / 3d;
                if (position.X < third)
                {
                    SetQrImage(WechatQr);
                }
                else if (position.X < third * 2)
                {
                    SetQrImage(AlipayQr);
                }
                else
                {
                    SetQrImage(QqQr);
                }
            }
            else
            {
                SetQrImage(AllQr);
            }
        }

        private Bitmap GetCurrentBitmap()
        {
            return QrImage.Tag as Bitmap;
        }

        private void SetQrImage(Bitmap bitmap)
        {
            QrImage.Tag = bitmap;
            QrImage.Source = ToBitmapSource(bitmap);
        }

        private static Bitmap CropQr(int index)
        {
            var bitmap = new Bitmap(200, 200);
            using var graphics = Graphics.FromImage(bitmap);
            var destRect = new Rectangle(0, 0, 200, 200);
            var srcRect = new Rectangle(index * 200, 0, 200, 200);
            graphics.DrawImage(AllQr, destRect, srcRect, GraphicsUnit.Pixel);
            return bitmap;
        }

        private static BitmapSource ToBitmapSource(Bitmap bitmap)
        {
            var hBitmap = bitmap.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);
    }
}
