using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace ContextMenuManager.Methods
{
    internal static class BitmapExtension
    {
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

        public static BitmapSource ToBitmapSource(this Bitmap bitmap)
        {
            if (bitmap == null) return null;
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

        public static BitmapSource ToBitmapSource(this Image image)
        {
            return (image as Bitmap)?.ToBitmapSource();
        }
    }
}
