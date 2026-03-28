using ContextMenuManager.Methods;
using ContextMenuManager.Properties;
using System;
using System.Windows.Controls;

namespace ContextMenuManager.Views
{
    public partial class AboutAppView : UserControl
    {
        private const string GitHubUrl = "https://github.com/Jack251970/ContextMenuManager";
        private const string GiteeUrl = "https://gitee.com/Jack251970/ContextMenuManager";

        public AboutAppView()
        {
            InitializeComponent();
            LogoImage.Source = AppResources.Logo.ToBitmapSource();
            RefreshContent();
        }

        public void RefreshContent()
        {
            AppNameText.Text = AppString.General.AppName;
            GitHubLinkText.Content = $"{AppString.About.GitHub ?? "GitHub"}: {GitHubUrl}";
            GitHubLinkText.NavigateUri = new Uri(GitHubUrl);
            GiteeLinkText.Content = $"{AppString.About.Gitee ?? "Gitee"}: {GiteeUrl}";
            GiteeLinkText.NavigateUri = new Uri(GiteeUrl);
            LicenseText.Text = $"{AppString.About.License ?? "License"}: GPL License";
            CheckUpdateButton.Content = AppString.About.CheckUpdate ?? "Check Update";
        }

        private void CheckUpdateButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
        {
            Updater.Update(true);
        }
    }
}
