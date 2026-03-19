using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ContextMenuManager.Methods
{
    internal static partial class OwnerWindowHelper
    {
        public static bool TryGetOwnerBounds(IntPtr hwndOwner, out Rectangle bounds)
        {
            bounds = Rectangle.Empty;
            if (hwndOwner == IntPtr.Zero)
            {
                return false;
            }

            if (!GetWindowRect(hwndOwner, out var rect))
            {
                return false;
            }

            bounds = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
            return bounds.Width > 0 && bounds.Height > 0;
        }

        public static void PositionFormNearOwner(Form form, IntPtr hwndOwner, int topOffset)
        {
            if (!TryGetOwnerBounds(hwndOwner, out var ownerBounds))
            {
                form.StartPosition = FormStartPosition.CenterScreen;
                return;
            }

            form.StartPosition = FormStartPosition.Manual;
            form.Left = ownerBounds.Left + Math.Max(0, (ownerBounds.Width - form.Width) / 2);
            form.Top = ownerBounds.Top + topOffset;
        }

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}
