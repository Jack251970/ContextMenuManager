using System;
using System.Windows;
using System.Windows.Controls;

namespace ContextMenuManager.Controls
{
    internal class RToolStripMenuItem : MenuItem
    {
        public RToolStripMenuItem() : base()
        {
        }

        public RToolStripMenuItem(string text) : this()
        {
            Header = text;
        }

        public RToolStripMenuItem(string text, EventHandler onClick) : this(text)
        {
            Click += (s, e) => onClick?.Invoke(s, EventArgs.Empty);
        }

        public RToolStripMenuItem(string text, EventHandler onClick, string name) : this(text, onClick)
        {
            Name = name;
        }

        public bool Enabled
        {
            get => IsEnabled;
            set => IsEnabled = value;
        }

        public new bool Checked
        {
            get => IsChecked;
            set => IsChecked = value;
        }

        public bool Visible
        {
            get => Visibility == Visibility.Visible;
            set => Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public class RToolStripSeparator : Separator
    {
        public RToolStripSeparator() : base()
        {
        }
    }
}
