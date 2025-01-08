using BluePointLilac.Controls;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    internal class RToolStripMenuItem : ToolStripMenuItem
    {
        public RToolStripMenuItem() : base()
        {
            ForeColor = MyMainForm.FormFore;
            BackColor = MyMainForm.FormBack;
            MouseHover += RToolStripMenuItem_MouseHover;
        }

        private void RToolStripMenuItem_MouseHover(object sender, EventArgs e)
        {

        }

        public RToolStripMenuItem(string text) : base(text)
        {
            ForeColor = MyMainForm.FormFore;
            BackColor = MyMainForm.FormBack;
        }

        public RToolStripMenuItem(string text, Image image) : base(text, image)
        {
            ForeColor = MyMainForm.FormFore;
            BackColor = MyMainForm.FormBack;
        }

        public RToolStripMenuItem(string text, Image image, EventHandler onClick) : base(text, image, onClick)
        {
            ForeColor = MyMainForm.FormFore;
            BackColor = MyMainForm.FormBack;
        }

        public RToolStripMenuItem(string text, Image image, EventHandler onClick, string name) : base(text, image, onClick, name)
        {
            ForeColor = MyMainForm.FormFore;
            BackColor = MyMainForm.FormBack;
        }
    }

    public class RToolStripSeparator : ToolStripSeparator
    {
        public RToolStripSeparator() : base()
        {
            ForeColor = MyMainForm.FormFore;
            BackColor = MyMainForm.FormBack;
        }
    }
}
