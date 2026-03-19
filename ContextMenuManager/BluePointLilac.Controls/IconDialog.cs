using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    /// <summary>
    /// Represents a dialog box that allows the user to select an icon from a file.
    /// </summary>
    /// <remarks>Use the IconDialog class to prompt users to choose an icon from a file, such as an executable
    /// or DLL. The selected icon's path and index are available through the IconPath and IconIndex properties. This
    /// class is sealed and cannot be inherited.</remarks>
    public sealed class IconDialog : CommonDialog
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "#62", SetLastError = true)]
        private static extern bool PickIconDlg(IntPtr hWnd, StringBuilder pszFileName, int cchFileNameMax, ref int pnIconIndex);

        private const int MAXLENGTH = 260;
        private int iconIndex;
        public int IconIndex { get => iconIndex; set => iconIndex = value; }
        public string IconPath { get; set; }

        public override void Reset() { }

        protected override bool RunDialog(IntPtr hwndOwner)
        {
            var sb = new StringBuilder(IconPath, MAXLENGTH);
            var flag = PickIconDlg(hwndOwner, sb, MAXLENGTH, ref iconIndex);
            IconPath = flag ? sb.ToString() : null;
            return flag;
        }
    }
}