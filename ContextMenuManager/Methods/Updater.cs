using BluePointLilac.Controls;
using BluePointLilac.Methods;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace ContextMenuManager.Methods
{
    internal sealed class Updater
    {
        /// <summary>定期检查更新</summary>
        public static void PeriodicUpdate()
        {
            var day = AppConfig.UpdateFrequency;
            if (day == -1) return;//自动检测更新频率为-1则从不自动检查更新
            //如果上次检测更新时间加上时间间隔早于或等于今天以前就进行更新操作
            var time = AppConfig.LastCheckUpdateTime.AddDays(day);
            //time = DateTime.Today;//测试用
            if (time <= DateTime.Today) _ = Task.Run(() => Update(false));
        }

        /// <summary>更新程序以及程序字典</summary>
        /// <param name="isManual">是否为手动点击更新</param>
        public static void Update(bool isManual)
        {
            AppConfig.LastCheckUpdateTime = DateTime.Today;
            UpdateText(isManual);
            UpdateApp(isManual);
        }

        /// <summary>更新程序</summary>
        /// <param name="isManual">是否为手动点击更新</param>
        private static async void UpdateApp(bool isManual)
        {
            using var client = new UAWebClient();
            var url = AppConfig.RequestUseGithub ? AppConfig.GithubLatestApi : AppConfig.GiteeLatestApi;
            var doc = await client.GetWebJsonToXmlAsync(url);
            if (doc == null)
            {
                if (isManual)
                {
                    if (AppMessageBox.Show(AppString.Message.WebDataReadFailed + "\r\n"
                        + AppString.Message.OpenWebUrl, MessageBoxButtons.OKCancel) != DialogResult.OK) return;
                    url = AppConfig.RequestUseGithub ? AppConfig.GithubLatest : AppConfig.GiteeReleases;
                    ExternalProgram.OpenWebUrl(url);
                }
                return;
            }
            var root = doc.FirstChild;
            var tagNameXN = root.SelectSingleNode("tag_name");
            var webVer = new Version(tagNameXN.InnerText);
            var appVer = new Version(Application.ProductVersion);
#if DEBUG
            appVer = new Version(0, 0, 0, 0);//测试用
#endif
            if (appVer >= webVer)
            {
                if (isManual) AppMessageBox.Show(AppString.Message.VersionIsLatest,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                var bodyXN = root.SelectSingleNode("body");
                var info = AppString.Message.UpdateInfo.Replace("%v1", appVer.ToString()).Replace("%v2", webVer.ToString());
                info += "\r\n\r\n" + MachinedInfo(bodyXN.InnerText);
                if (MessageBox.Show(info, AppString.General.AppName,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    var assetsXN = root.SelectSingleNode("assets");
                    foreach (XmlNode itemXN in assetsXN.SelectNodes("item"))
                    {
                        var nameXN = itemXN.SelectSingleNode("name");
                        if (nameXN != null && nameXN.InnerText.Contains(".exe"))
                        {
                            var urlXN = itemXN.SelectSingleNode("browser_download_url");
                            using var dlg = new DownloadDialog();
                            dlg.Url = urlXN?.InnerText;
                            dlg.FilePath = $@"{AppConfig.AppDataDir}\{webVer}.exe";
                            dlg.Text = AppString.General.AppName;
                            if (dlg.ShowDialog() == DialogResult.OK)
                            {
                                AppMessageBox.Show(AppString.Message.UpdateSucceeded,
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                                SingleInstance.Restart(null, dlg.FilePath);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>更新程序字典</summary>
        /// <param name="isManual">是否为手动点击更新</param>
        private static async void UpdateText(bool isManual)
        {
            string dirUrl;
            string[] filePaths;
            async Task WriteFiles(string dirName, string[] paths, Action<string, string> callback)
            {
                string succeeded = "", failed = "";
                foreach (var filePath in paths)
                {
                    using var client = new UAWebClient();
                    var fileUrl = $"{dirUrl}/{Path.GetFileName(filePath)}";
                    var flag = await client.WebStringToFileAsync(filePath, fileUrl);
                    var item = "\r\n ● " + Path.GetFileName(filePath);
                    if (flag) succeeded += item;
                    else failed += item;
                }
                dirName = "\r\n\r\n" + dirName + ":";
                if (succeeded != "") succeeded = dirName + succeeded;
                if (failed != "") failed = dirName + failed;
                callback(succeeded, failed);
            }

            string succeeded1 = "", failed1 = "", succeeded2 = "", failed2 = "";

            dirUrl = AppConfig.RequestUseGithub ? AppConfig.GithubTexts : AppConfig.GiteeTexts;
            filePaths = new[]
            {
                AppConfig.WebGuidInfosDic, AppConfig.WebEnhanceMenusDic,
                AppConfig.WebDetailedEditDic, AppConfig.WebUwpModeItemsDic
            };
            await WriteFiles("Dictionaries", filePaths, (s, f) => { succeeded1 = s; failed1 = f; });

            dirUrl = AppConfig.RequestUseGithub ? AppConfig.GithubLangsRawDir : AppConfig.GiteeLangsRawDir;
            filePaths = Directory.GetFiles(AppConfig.LangsDir, "*.ini");
            await WriteFiles("Languages", filePaths, (s, f) => { succeeded2 = s; failed2 = f; });

            if (isManual)
            {
                var failed = failed1 + failed2;
                var succeeded = succeeded1 + succeeded2;
                if (failed != "") AppMessageBox.Show(AppString.Message.WebDataReadFailed + failed);
                if (succeeded != "") AppMessageBox.Show(AppString.Message.DicUpdateSucceeded + succeeded,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>加工处理更新信息，去掉标题头</summary>
        private static string MachinedInfo(string info)
        {
            var str = string.Empty;
            var lines = info.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            for (var m = 0; m < lines.Length; m++)
            {
                var line = lines[m];
                for (var n = 1; n <= 6; n++)
                {
                    if (line.StartsWith(new string('#', n) + ' '))
                    {
                        line = line[(n + 1)..];
                        break;
                    }
                }
                str += line + "\r\n";
            }
            return str;
        }
    }
}