using BluePointLilac.Methods;
using ContextMenuManager.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace ContextMenuManager.Methods
{
    internal static class AppConfig
    {
        static AppConfig()
        {
            CreateDirectory();
            ReloadConfig();
            LoadLanguage();
            BackupWinX();
        }

#if DEBUG
        public const string DebugLogPath = "D:\\log.txt";
        public const bool EnableLog = false;
#endif

        public const string GithubLatest = "https://github.com/Jack251970/ContextMenuManager/releases/latest";
        public const string GithubLatestApi = "https://api.github.com/repos/Jack251970/ContextMenuManager/releases/latest";
        public const string GithubLangsApi = "https://api.github.com/repos/Jack251970/ContextMenuManager/contents/languages";
        public const string GithubLangsRawDir = "https://raw.githubusercontent.com/Jack251970/ContextMenuManager/master/languages";
        public const string GithubShellNewApi = "https://api.github.com/repos/Jack251970/ContextMenuManager/contents/ContextMenuManager/Properties/Resources/ShellNew";
        public const string GithubShellNewRawDir = "https://raw.githubusercontent.com/Jack251970/ContextMenuManager/master/ContextMenuManager/Properties/Resources/ShellNew";
        public const string GithubTexts = "https://raw.githubusercontent.com/Jack251970/ContextMenuManager/master/ContextMenuManager/Properties/Resources/Texts";
        public const string GithubDonateRaw = "https://raw.githubusercontent.com/Jack251970/ContextMenuManager/master/Donate.md";
        public const string GithubDonate = "https://github.com/Jack251970/ContextMenuManager/blob/master/Donate.md";

        public const string GiteeReleases = "https://gitee.com/Jack251970/ContextMenuManager/releases";
        public const string GiteeLatestApi = "https://gitee.com/api/v5/repos/Jack251970/ContextMenuManager/releases/latest";
        public const string GiteeLangsApi = "https://gitee.com/api/v5/repos/Jack251970/ContextMenuManager/contents/languages";
        public const string GiteeLangsRawDir = "https://gitee.com/Jack251970/ContextMenuManager/raw/master/languages";
        public const string GiteeShellNewApi = "https://gitee.com/api/v5/repos/Jack251970/ContextMenuManager/contents/ContextMenuManager/Properties/Resources/ShellNew";
        public const string GiteeShellNewRawDir = "https://gitee.com/Jack251970/ContextMenuManager/raw/master/ContextMenuManager/Properties/Resources/ShellNew";
        public const string GiteeTexts = "https://gitee.com/Jack251970/ContextMenuManager/raw/master/ContextMenuManager/Properties/Resources/Texts";
        public const string GiteeDonateRaw = "https://gitee.com/Jack251970/ContextMenuManager/raw/master/Donate.md";
        public const string GiteeDonate = "https://gitee.com/Jack251970/ContextMenuManager/blob/master/Donate.md";

        public static readonly string AppConfigDir = $@"{Application.StartupPath}\Config";
        public static readonly string AppDataDir = Environment.ExpandEnvironmentVariables(@"%AppData%\ContextMenuManager");
        public static readonly string AppDataConfigDir = $@"{AppDataDir}\Config";
        public static readonly string ConfigDir = Directory.Exists(AppConfigDir) ? AppConfigDir : AppDataConfigDir;
        public static readonly bool SaveToAppDir = ConfigDir == AppConfigDir;
        public static readonly bool IsFirstRun = !Directory.Exists(ConfigDir);
        public static string ConfigIni = $@"{ConfigDir}\Config.ini";
        public static string ComputerHostName = Dns.GetHostName();
        public static string RegBackupDir = $@"{ConfigDir}\RegBackup\{ComputerHostName}";
        public static string MenuBackupRootDir = $@"{ConfigDir}\MenuBackup";
        public static string MenuBackupDir = $@"{MenuBackupRootDir}\{ComputerHostName}";
        public static string LangsDir = $@"{ConfigDir}\Languages";
        public static string ProgramsDir = $@"{ConfigDir}\Programs";
        public static string DicsDir = $@"{ConfigDir}\Dictionaries";
        public static string WebDicsDir = $@"{DicsDir}\Web";
        public static string UserDicsDir = $@"{DicsDir}\User";

        public static string WebGuidInfosDic = $@"{WebDicsDir}\{GUIDINFOSDICINI}";
        public static string WebDetailedEditDic = $@"{WebDicsDir}\{DETAILEDEDITDICXML}";
        public static string WebEnhanceMenusDic = $@"{WebDicsDir}\{ENHANCEMENUSICXML}";
        public static string WebUwpModeItemsDic = $@"{WebDicsDir}\{UWPMODEITEMSDICXML}";

        public static string UserGuidInfosDic = $@"{UserDicsDir}\{GUIDINFOSDICINI}";
        public static string UserDetailedEditDic = $@"{UserDicsDir}\{DETAILEDEDITDICXML}";
        public static string UserEnhanceMenusDic = $@"{UserDicsDir}\{ENHANCEMENUSICXML}";
        public static string UserUwpModeItemsDic = $@"{UserDicsDir}\{UWPMODEITEMSDICXML}";

        public const string ZH_CNINI = "zh-CN.ini";
        public const string GUIDINFOSDICINI = "GuidInfosDic.ini";
        public const string DETAILEDEDITDICXML = "DetailedEditDic.xml";
        public const string ENHANCEMENUSICXML = "EnhanceMenusDic.xml";
        public const string UWPMODEITEMSDICXML = "UwpModeItemsDic.xml";

        public static readonly Dictionary<string, string> EngineUrlsDic = new()
        {
            { "Bing", "https://www.bing.com/search?q=%s" },
            { "Baidu", "https://www.baidu.com/s?wd=%s" },
            { "Google", "https://www.google.com/search?q=%s" },
            { "Yandex", "https://yandex.com/search/?text=%s" },
            { "DuckDuckGo", "https://duckduckgo.com/?q=%s" },
        };

        private static readonly IniReader ConfigReader = new(ConfigIni);
        private static readonly IniWriter ConfigWriter = new(ConfigIni);

        public static void BackupWinX()
        {
            if (WinOsVersion.Current >= WinOsVersion.Win11)
            {
                // 备份默认WinX项目文件与desktop.ini
                var DefaultWinXBackupDir = WinXList.WinXDefaultPath;
                if (!Directory.Exists(DefaultWinXBackupDir))
                {
                    Directory.CreateDirectory(DefaultWinXBackupDir);
                    foreach (var dirPath in Directory.GetDirectories(WinXList.DefaultWinXPath, "*", SearchOption.AllDirectories))
                    {
                        Directory.CreateDirectory(dirPath.Replace(WinXList.DefaultWinXPath, DefaultWinXBackupDir));
                    }
                    foreach (var filePath in Directory.GetFiles(WinXList.DefaultWinXPath, "*.*", SearchOption.AllDirectories))
                    {
                        var dstPath = filePath.Replace(WinXList.DefaultWinXPath, DefaultWinXBackupDir);
                        File.Copy(filePath, dstPath, true);
                    }
                }
            }
        }

        private static string GetGeneralValue(string key)
        {
            return ConfigReader.GetValue("General", key);
        }

        private static void SetGeneralValue(string key, object value)
        {
            ConfigWriter.SetValue("General", key, value);
            ReloadConfig();
        }

        private static string GetWindowValue(string key)
        {
            return ConfigReader.GetValue("Window", key);
        }

        private static void SetWindowValue(string key, object value)
        {
            ConfigWriter.SetValue("Window", key, value);
            ReloadConfig();
        }

        public static void ReloadConfig()
        {
            ConfigReader.LoadFile(ConfigIni);
        }

        private static void CreateDirectory()
        {
            foreach (var dirPath in new[] { AppDataDir, ConfigDir, ProgramsDir, RegBackupDir, MenuBackupDir, LangsDir, DicsDir, WebDicsDir, UserDicsDir })
            {
                Directory.CreateDirectory(dirPath);
                Application.ApplicationExit += (sender, e) =>
                {
                    if (Directory.Exists(dirPath) && Directory.GetFileSystemEntries(dirPath).Length == 0)
                    {
                        try
                        {
                            Directory.Delete(dirPath);
                        }
                        catch
                        {

                        }
                    }
                };
            }
        }

        private static void LoadLanguage()
        {
            language = GetGeneralValue("Language");
            if (language.ToLower() == "default")
            {
                LanguageIniPath = "";
                return;
            }
            if (language == "") language = CultureInfo.CurrentUICulture.Name;
            
#if DEBUG
            // 在开发环境中使用项目根目录下的languages目录
            string devLangsDir = $@"{Application.StartupPath}\..\languages";
            LanguageIniPath = $@"{devLangsDir}\{language}.ini";
            if (!File.Exists(LanguageIniPath))
            {
                // 如果开发目录中的语言文件不存在，回退到Config\Languages目录
                LanguageIniPath = $@"{LangsDir}\{language}.ini";
            }
#else
            // 在发布环境中使用Config\Languages目录
            LanguageIniPath = $@"{LangsDir}\{language}.ini";
#endif
            
            if (!File.Exists(LanguageIniPath))
            {
                LanguageIniPath = "";
                Language = "";
            }
        }

        public static string LanguageIniPath { get; private set; }

        private static string language;
        public static string Language
        {
            get => language;
            set => SetGeneralValue("Language", value);
        }

        public static bool AutoBackup
        {
            get => GetGeneralValue("AutoBackup") != "0";
            set => SetGeneralValue("AutoBackup", value ? 1 : 0);
        }

        public static DateTime LastCheckUpdateTime
        {
            get
            {
                try
                {
                    var time = GetGeneralValue("LastCheckUpdateTime");
                    //二进制数据时间不会受系统时间格式影响
                    return DateTime.FromBinary(Convert.ToInt64(time));
                }
                catch
                {
                    return DateTime.MinValue;
                    //返回文件上次修改时间
                    //return new FileInfo(Application.ExecutablePath).LastWriteTime;
                }
            }
            set => SetGeneralValue("LastCheckUpdateTime", value.ToBinary());
        }

        public static bool ProtectOpenItem
        {
            get => GetGeneralValue("ProtectOpenItem") != "0";
            set => SetGeneralValue("ProtectOpenItem", value ? 1 : 0);
        }

        public static string EngineUrl
        {
            get
            {
                var url = GetGeneralValue("EngineUrl");
                if (string.IsNullOrEmpty(url)) url = EngineUrlsDic.Values.ToArray()[0];
                return url;
            }
            set => SetGeneralValue("EngineUrl", value);
        }

        public static bool ShowFilePath
        {
            get => GetGeneralValue("ShowFilePath") == "1";
            set => SetGeneralValue("ShowFilePath", value ? 1 : 0);
        }

        public static bool WinXSortable
        {
            get => GetGeneralValue("WinXSortable") == "1";
            set => SetGeneralValue("WinXSortable", value ? 1 : 0);
        }

        public static bool OpenMoreRegedit
        {
            get => GetGeneralValue("OpenMoreRegedit") == "1";
            set => SetGeneralValue("OpenMoreRegedit", value ? 1 : 0);
        }

        public static bool OpenMoreExplorer
        {
            get => GetGeneralValue("OpenMoreExplorer") == "1";
            set => SetGeneralValue("OpenMoreExplorer", value ? 1 : 0);
        }

        public static bool HideDisabledItems
        {
            get => GetGeneralValue("HideDisabledItems") == "1";
            set => SetGeneralValue("HideDisabledItems", value ? 1 : 0);
        }

        public static bool HideSysStoreItems
        {
            get => GetGeneralValue("HideSysStoreItems") != "0";
            set => SetGeneralValue("HideSysStoreItems", value ? 1 : 0);
        }

        public static bool RequestUseGithub
        {
            get
            {
                var value = GetGeneralValue("RequestUseGithub");
                if (!string.IsNullOrEmpty(value)) return value == "1";
                if (CultureInfo.CurrentCulture.Name == "zh-CN") return false;
                return true;
            }
            set => SetGeneralValue("RequestUseGithub", value ? 1 : 0);
        }

        public static int UpdateFrequency
        {
            get
            {
                var value = GetGeneralValue("UpdateFrequency");
                if (int.TryParse(value, out var day))
                {
                    if (day is -1 or 7 or 90) return day;
                }
                return 30;
            }
            set => SetGeneralValue("UpdateFrequency", value);
        }

        public static bool TopMost
        {
            get => GetWindowValue("TopMost") == "1";
            set => SetWindowValue("TopMost", value ? 1 : 0);
        }

        public static Size MainFormSize
        {
            get
            {
                var str = GetWindowValue("MainFormSize");
                var index = str.IndexOf(',');
                if (index == -1) return Size.Empty;
                if (int.TryParse(str[..index], out var x))
                    if (int.TryParse(str[(index + 1)..], out var y))
                        return new Size(x, y);
                return Size.Empty;
            }
            set => SetWindowValue("MainFormSize", value.Width + "," + value.Height);
        }
    }
}