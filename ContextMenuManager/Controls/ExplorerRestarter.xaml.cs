using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using System.Windows;

namespace ContextMenuManager.Controls
{
    public partial class ExplorerRestarter : InfoBar
    {
        private static ExplorerRestarter current;

        public ExplorerRestarter()
        {
            InitializeComponent();
            Message = AppString.Other.RestartExplorer;
            Loaded += (_, _) =>
            {
                if (current != this)
                {
                    current = this;
                }
            };
            Unloaded += (_, _) =>
            {
                if (current == this)
                {
                    current = null;
                }
            };
        }

        public bool IsPendingRestart => IsOpen = true;

        public static void Show()
        {
            current?.Dispatcher.Invoke(() => current.IsOpen = true);
        }

        public static void Hide()
        {
            current?.Dispatcher.Invoke(() => current.IsOpen = false);
        }

        private void RestartButton_OnClick(object sender, RoutedEventArgs e)
        {
            ExternalProgram.RestartExplorer();
            Hide();
        }
    }
}
