using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Documents;

namespace ContextMenuManager
{
    internal partial class ReportWindow
    {
        public ReportWindow(Exception exception)
        {
            InitializeComponent();
            ErrorTextbox.Document.Blocks.FirstBlock.Margin = new Thickness(0);
            SetException(exception);
        }

        private void SetException(Exception exception)
        {
            var websiteUrl = "https://github.com/Jack251970/ContextMenuManager/issues";

            var paragraph = Hyperlink("Please open an issue:", websiteUrl);
            ErrorTextbox.Document.Blocks.Add(paragraph);

            var content = new StringBuilder();
            content.AppendLine();
            content.AppendLine($"Date: {DateTime.Now.ToString(CultureInfo.InvariantCulture)}");
            content.AppendLine("Exception:");
            content.AppendLine(exception.ToString());
            paragraph = new Paragraph();
            paragraph.Inlines.Add(content.ToString());
            ErrorTextbox.Document.Blocks.Add(paragraph);
        }

        private static Paragraph Hyperlink(string textBeforeUrl, string url)
        {
            var paragraph = new Paragraph
            {
                Margin = new Thickness(0)
            };

            Hyperlink link = null;
            try
            {
                var uri = new Uri(url);

                link = new Hyperlink
                {
                    IsEnabled = true
                };
                link.Inlines.Add(url);
                link.NavigateUri = uri;
            }
            catch (Exception)
            {
                // Leave link as null if the URL is invalid
            }

            paragraph.Inlines.Add(textBeforeUrl);
            paragraph.Inlines.Add(" ");
            if (link is null)
            {
                // Add the URL as plain text if it is invalid
                paragraph.Inlines.Add(url);
            }
            else
            {
                // Add the hyperlink if it is valid
                paragraph.Inlines.Add(link);
            }
            paragraph.Inlines.Add("\n");

            return paragraph;
        }

        private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
