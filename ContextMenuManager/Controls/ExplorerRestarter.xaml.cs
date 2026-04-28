using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using System.Windows;

namespace ContextMenuManager.Controls
{
    public partial class ExplorerRestarter : InfoBar
    {
        public static bool IsPendingRestart { get; private set; }
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

        public static void Show()
        {
            IsPendingRestart = true;
            current?.Dispatcher.Invoke(() => current.IsOpen = true);
        }

        public static void Hide()
        {
            IsPendingRestart = false;
            current?.Dispatcher.Invoke(() => current.IsOpen = false);
        }

        private void RestartButton_OnClick(object sender, RoutedEventArgs e)
        {
            ExternalProgram.RestartExplorer();
            Hide();
        }
    }
}
