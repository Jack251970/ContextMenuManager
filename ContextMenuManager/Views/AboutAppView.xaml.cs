using ContextMenuManager.Methods;
using ContextMenuManager.Properties;
using System;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace ContextMenuManager.Views
{
    public partial class AboutAppView : UserControl
    {
        private const string GitHubUrl = "https://github.com/Jack251970/ContextMenuManager";
        private const string GiteeUrl = "https://gitee.com/Jack251970/ContextMenuManager";

        public AboutAppView()
        {
            InitializeComponent();
            LogoImage.Source = ToBitmapSource(AppResources.Logo);
            RefreshContent();
        }

        public void RefreshContent()
        {
            AppNameText.Text = AppString.General.AppName;
            ProjectLinksHeader.Text = AppString.SideBar.AboutApp ?? "About";
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

        private static BitmapSource ToBitmapSource(System.Drawing.Bitmap bitmap)
        {
            var hBitmap = bitmap.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);
    }
}
