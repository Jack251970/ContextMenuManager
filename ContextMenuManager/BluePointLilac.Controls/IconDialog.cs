using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Interop;

namespace ContextMenuManager.Controls
{
    public sealed class IconDialog
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "#62", SetLastError = true)]
        private static extern bool PickIconDlg(IntPtr hWnd, StringBuilder pszFileName, int cchFileNameMax, ref int pnIconIndex);

        private const int MAXLENGTH = 260;
        private int iconIndex;
        public int IconIndex { get => iconIndex; set => iconIndex = value; }
        public string IconPath { get; set; }

        public bool ShowDialog()
        {
            return RunDialog(null);
        }

        public bool RunDialog(MainWindow owner)
        {
            var hwndOwner = IntPtr.Zero;
            if (owner != null)
            {
                var helper = new WindowInteropHelper(owner);
                hwndOwner = helper.Handle;
            }

            var sb = new StringBuilder(IconPath, MAXLENGTH);
            var flag = PickIconDlg(hwndOwner, sb, MAXLENGTH, ref iconIndex);
            IconPath = flag ? sb.ToString() : null;
            return flag;
        }
    }
}