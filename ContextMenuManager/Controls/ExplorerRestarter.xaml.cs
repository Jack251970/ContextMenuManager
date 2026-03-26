using ContextMenuManager.Methods;
using System.Windows;
using System.Windows.Controls;

namespace ContextMenuManager.Controls
{
    public partial class ExplorerRestarter : UserControl
    {
        private static ExplorerRestarter current;
        private static bool isPendingRestart;
        private static string pendingMessage = AppString.Other.RestartExplorer;

        public ExplorerRestarter()
        {
            InitializeComponent();
            current = this;
            MessageText = AppString.Other.RestartExplorer;
            RestartButton.Content = AppString.Other.RestartExplorer;
            RestartButton.ToolTip = AppString.Tip.RestartExplorer;
            Unloaded += (_, _) =>
            {
                if (ReferenceEquals(current, this))
                {
                    current = null;
                }
            };
        }

        public bool IsPendingRestart => Visibility == Visibility.Visible;
        public static bool PendingRestart => isPendingRestart;
        public static string PendingMessage => pendingMessage;

        public string MessageText
        {
            get => MessageTextBlock.Text;
            set => MessageTextBlock.Text = value;
        }

        public static void Show()
        {
            isPendingRestart = true;
            pendingMessage = AppString.Other.RestartExplorer;
            current?.Dispatcher.Invoke(() =>
            {
                current.MessageText = pendingMessage;
                current.Visibility = Visibility.Visible;
            });
        }

        public static void Hide()
        {
            isPendingRestart = false;
            current?.Dispatcher.Invoke(() => current.Visibility = Visibility.Collapsed);
        }

        private void RestartButton_OnClick(object sender, RoutedEventArgs e)
        {
            ExternalProgram.RestartExplorer();
            Hide();
        }
    }
}
