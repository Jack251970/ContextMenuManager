using BluePointLilac.Methods;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public sealed class ReadOnlyTextBox : TextBox
    {
        public ReadOnlyTextBox()
        {
            ReadOnly = true;
            Multiline = true;
            ShortcutsEnabled = false;
            ForeColor = MyMainForm.foreMain;
            BackColor = MyMainForm.formBack;
            Font = SystemFonts.MenuFont;
            Font = new Font(Font.FontFamily, Font.Size + 1F);
        }

        const int WM_SETFOCUS = 0x0007;
        const int WM_KILLFOCUS = 0x0008;
        protected override void WndProc(ref Message m)
        {
            switch(m.Msg)
            {
                case WM_SETFOCUS:
                    m.Msg = WM_KILLFOCUS; break;
            }
            base.WndProc(ref m);
        }

        private bool firstEnter = true;

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            if(firstEnter) Focus();
            firstEnter = false;
        }
    }

    public sealed class ReadOnlyRichTextBox : RichTextBox
    {
        public ReadOnlyRichTextBox()
        {
            ReadOnly = true;
            Dock = DockStyle.Fill;
            BorderStyle = BorderStyle.None;
            ForeColor = MyMainForm.foreMain;
            BackColor = MyMainForm.formBack;
            Font = SystemFonts.MenuFont;
            Font = new Font(Font.FontFamily, Font.Size + 1F);
        }

        const int WM_SETFOCUS = 0x0007;
        const int WM_KILLFOCUS = 0x0008;

        protected override void WndProc(ref Message m)
        {
            switch(m.Msg)
            {
                case WM_SETFOCUS:
                    m.Msg = WM_KILLFOCUS; break;
            }
            base.WndProc(ref m);
        }

        private bool firstEnter = true;

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            if(firstEnter) Focus();
            firstEnter = false;
        }

        protected override void OnLinkClicked(LinkClickedEventArgs e)
        {
            base.OnLinkClicked(e);
            ExternalProgram.OpenWebUrl(e.LinkText);
        }
    }
}