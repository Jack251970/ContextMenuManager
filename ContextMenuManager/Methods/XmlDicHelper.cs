using BluePointLilac.Methods;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace ContextMenuManager.Methods
{
    internal static class XmlDicHelper
    {
        public static readonly List<XmlDocument> EnhanceMenusDic = new();
        public static readonly List<XmlDocument> DetailedEditDic = new();
        public static readonly List<XmlDocument> UwpModeItemsDic = new();
        public static readonly Dictionary<string, bool> EnhanceMenuPathDic
            = new(StringComparer.OrdinalIgnoreCase);
        public static readonly Dictionary<Guid, bool> DetailedEditGuidDic = new();

        /// <summary>重新加载字典</summary>
        public static void ReloadDics()
        {
            XmlDocument LoadXml(string xmlPath)
            {
                if (!File.Exists(xmlPath)) return null;
                try
                {
                    var doc = new XmlDocument();
                    var encoding = EncodingType.GetType(xmlPath);
                    var xml = File.ReadAllText(xmlPath, encoding).Trim();
                    doc.LoadXml(xml);
                    return doc;
                }
                catch (Exception e)
                {
                    AppMessageBox.Show(e.Message + "\n" + xmlPath);
                    return null;
                }
            }

            void LoadDic(List<XmlDocument> dic, string webPath, string userPath, string defaultContent)
            {
                if (!File.Exists(webPath)) File.WriteAllText(webPath, defaultContent, Encoding.Unicode);
                dic.Clear();
                dic.Add(LoadXml(webPath));
                dic.Add(LoadXml(userPath));
            }

            LoadDic(UwpModeItemsDic, AppConfig.WebUwpModeItemsDic,
                AppConfig.UserUwpModeItemsDic, Properties.Resources.UwpModeItemsDic);
            LoadDic(EnhanceMenusDic, AppConfig.WebEnhanceMenusDic,
                AppConfig.UserEnhanceMenusDic, Properties.Resources.EnhanceMenusDic);
            LoadDic(DetailedEditDic, AppConfig.WebDetailedEditDic,
                AppConfig.UserDetailedEditDic, Properties.Resources.DetailedEditDic);

            EnhanceMenuPathDic.Clear();
            for (var i = 0; i < 2; i++)
            {
                var doc = EnhanceMenusDic[i];
                if (doc?.DocumentElement == null) continue;
                foreach (XmlNode pathXN in doc.SelectNodes("Data/Group/RegPath"))
                {
                    if (EnhanceMenuPathDic.ContainsKey(pathXN.InnerText)) continue;
                    EnhanceMenuPathDic.Add(pathXN.InnerText, i == 1);
                }
            }

            DetailedEditGuidDic.Clear();
            for (var i = 0; i < 2; i++)
            {
                var doc = DetailedEditDic[i];
                if (doc?.DocumentElement == null) continue;
                foreach (XmlNode guidXN in doc.SelectNodes("Data/Group/Guid"))
                {
                    if (GuidEx.TryParse(guidXN.InnerText, out var guid))
                    {
                        if (DetailedEditGuidDic.ContainsKey(guid)) continue;
                        DetailedEditGuidDic.Add(guid, i == 1);
                    }
                }
            }
        }

        public static bool JudgeOSVersion(XmlNode itemXN)
        {
            //return true;//测试用
            static bool JudgeOne(XmlNode osXN)
            {
                var ver = new Version(osXN.InnerText);
                var osVer = Environment.OSVersion.Version;
                var compare = osVer.CompareTo(ver);
                var symbol = ((XmlElement)osXN).GetAttribute("Compare");
                return symbol switch
                {
                    ">" => compare > 0,
                    "<" => compare < 0,
                    "=" => compare == 0,
                    ">=" => compare >= 0,
                    "<=" => compare <= 0,
                    _ => true,
                };
            }

            foreach (XmlNode osXN in itemXN.SelectNodes("OSVersion"))
            {
                if (!JudgeOne(osXN)) return false;
            }
            return true;
        }

        public static bool FileExists(XmlNode itemXN)
        {
            //return true;//测试用
            foreach (XmlNode feXN in itemXN.SelectNodes("FileExists"))
            {
                var path = Environment.ExpandEnvironmentVariables(feXN.InnerText);
                if (!File.Exists(path)) return false;
            }
            return true;
        }

        public static bool JudgeCulture(XmlNode itemXN)
        {
            //return true;//测试用
            var culture = itemXN.SelectSingleNode("Culture")?.InnerText;
            if (string.IsNullOrEmpty(culture)) return true;
            if (culture.Equals(AppConfig.Language, StringComparison.OrdinalIgnoreCase)) return true;
            if (culture.Equals(CultureInfo.CurrentUICulture.Name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static byte[] ConvertToBinary(string value)
        {
            try
            {
                var strs = value.Split(' ');
                var bs = new byte[strs.Length];
                for (var i = 0; i < strs.Length; i++)
                {
                    bs[i] = Convert.ToByte(strs[i], 16);
                }
                return bs;
            }
            catch { return null; }
        }

        public static RegistryValueKind GetValueKind(string type, RegistryValueKind defaultKind)
        {
            return type.ToUpper() switch
            {
                "REG_SZ" => RegistryValueKind.String,
                "REG_BINARY" => RegistryValueKind.Binary,
                "REG_DWORD" => RegistryValueKind.DWord,
                "REG_QWORD" => RegistryValueKind.QWord,
                "REG_MULTI_SZ" => RegistryValueKind.MultiString,
                "REG_EXPAND_SZ" => RegistryValueKind.ExpandString,
                _ => defaultKind,
            };
        }
    }
}