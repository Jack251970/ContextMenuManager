using ContextMenuManager.Methods;
using System.Windows.Controls;
using System.Windows.Input;

namespace ContextMenuManager.Views
{
    public partial class AboutAppView : UserControl
    {
        private const string GitHubUrl = "https://github.com/Jack251970/ContextMenuManager";
        private const string GiteeUrl = "https://gitee.com/Jack251970/ContextMenuManager";

        public AboutAppView()
        {
            InitializeComponent();
            RefreshContent();
        }

        public void RefreshContent()
        {
            AppNameText.Text = AppString.General.AppName;
            DescriptionText.Text = AppString.About.Description;
            ProjectLinksHeader.Text = AppString.SideBar.AboutApp ?? "Project";
            GitHubLinkText.Text = $"{AppString.About.GitHub ?? "GitHub"}: {GitHubUrl}";
            GiteeLinkText.Text = $"{AppString.About.Gitee ?? "Gitee"}: {GiteeUrl}";
            LicenseText.Text = $"{AppString.About.License ?? "License"}: GPL License";
            CheckUpdateButton.Content = AppString.About.CheckUpdate ?? "Check Update";
        }

        private void CheckUpdateButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
        {
            Updater.Update(true);
        }

        private void GitHubLinkText_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ExternalProgram.OpenWebUrl(GitHubUrl);
        }

        private void GiteeLinkText_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ExternalProgram.OpenWebUrl(GiteeUrl);
        }
    }
}
