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
            ForeColor = DarkModeHelper.FormFore;
            BackColor = DarkModeHelper.FormBack;
        }

        public RToolStripMenuItem(string text) : base(text)
        {
            ForeColor = DarkModeHelper.FormFore;
            BackColor = DarkModeHelper.FormBack;
        }

        public RToolStripMenuItem(string text, Image image) : base(text, image)
        {
            ForeColor = DarkModeHelper.FormFore;
            BackColor = DarkModeHelper.FormBack;
        }

        public RToolStripMenuItem(string text, Image image, EventHandler onClick) : base(text, image, onClick)
        {
            ForeColor = DarkModeHelper.FormFore;
            BackColor = DarkModeHelper.FormBack;
        }

        public RToolStripMenuItem(string text, Image image, EventHandler onClick, string name) : base(text, image, onClick, name)
        {
            ForeColor = DarkModeHelper.FormFore;
            BackColor = DarkModeHelper.FormBack;
        }
    }

    public class RToolStripSeparator : ToolStripSeparator
    {
        public RToolStripSeparator() : base()
        {
            ForeColor = DarkModeHelper.FormFore;
            BackColor = DarkModeHelper.FormBack;
        }
    }
}