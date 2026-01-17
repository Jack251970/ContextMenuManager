using BluePointLilac.Methods;
using ContextMenuManager.Controls.Interfaces;
using ContextMenuManager.Methods;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace ContextMenuManager.Controls
{
    internal sealed class EnhanceShellItem : FoldSubItem, IChkVisibleItem
    {
        public string RegPath { get; set; }
        public XmlElement ItemXE { get; set; }
        public VisibleCheckBox ChkVisible { get; set; }

        public bool ItemVisible
        {
            get
            {
                using var key = RegistryEx.GetRegistryKey(RegPath);
                return key != null;
            }
            set
            {
                if (value) WriteSubKeysValue(ItemXE, RegPath);
                else RegistryEx.DeleteKeyTree(RegPath);
            }
        }

        public EnhanceShellItem()
        {
            ChkVisible = new VisibleCheckBox(this);
            Indent();
        }

        private static void WriteAttributesValue(XmlNode valueXN, string regPath)
        {
            if (valueXN == null) return;
            if (!XmlDicHelper.FileExists(valueXN)) return;
            if (!XmlDicHelper.JudgeCulture(valueXN)) return;
            if (!XmlDicHelper.JudgeOSVersion(valueXN)) return;
            using var key = RegistryEx.GetRegistryKey(regPath, true, true);
            foreach (XmlNode xn in valueXN.ChildNodes)
            {
                if (xn is XmlComment) continue;
                if (!XmlDicHelper.FileExists(xn)) continue;
                if (!XmlDicHelper.JudgeCulture(xn)) continue;
                if (!XmlDicHelper.JudgeOSVersion(xn)) continue;
                foreach (XmlAttribute xa in xn.Attributes)
                {
                    switch (xn.Name)
                    {
                        case "REG_SZ":
                            key.SetValue(xa.Name, Environment.ExpandEnvironmentVariables(xa.Value), RegistryValueKind.String);
                            break;
                        case "REG_EXPAND_SZ":
                            key.SetValue(xa.Name, xa.Value, RegistryValueKind.ExpandString);
                            break;
                        case "REG_BINARY":
                            key.SetValue(xa.Name, XmlDicHelper.ConvertToBinary(xa.Value), RegistryValueKind.Binary);
                            break;
                        case "REG_DWORD":
                            var num = xa.Value.ToLower().StartsWith("0x") ? 16 : 10;
                            key.SetValue(xa.Name, Convert.ToInt32(xa.Value, num), RegistryValueKind.DWord);
                            break;
                    }
                }
            }
        }

        private static void WriteSubKeysValue(XmlNode keyXN, string regPath)
        {
            if (keyXN == null) return;
            if (!XmlDicHelper.FileExists(keyXN)) return;
            if (!XmlDicHelper.JudgeCulture(keyXN)) return;
            if (!XmlDicHelper.JudgeOSVersion(keyXN)) return;
            var defaultValue = ((XmlElement)keyXN).GetAttribute("Default");
            if (!defaultValue.IsNullOrWhiteSpace())
            {
                defaultValue = Environment.ExpandEnvironmentVariables(defaultValue);
                Registry.SetValue(regPath, "", defaultValue);
            }
            else if (keyXN.Name == "Command")
            {
                //按照规则Command节点无默认值则创建文件
                WriteCommandValue(keyXN, regPath);
            }
            WriteAttributesValue(keyXN.SelectSingleNode("Value"), regPath);

            var subKeyXN = keyXN.SelectSingleNode("SubKey");
            if (subKeyXN != null)
            {
                foreach (XmlNode xn in subKeyXN.ChildNodes)
                {
                    if (xn is XmlComment) continue;
                    WriteSubKeysValue(xn, $@"{regPath}\{xn.Name}");
                }
            }
        }

        private static void WriteCommandValue(XmlNode cmdXE, string regPath)
        {
            var fnXE = (XmlElement)cmdXE.SelectSingleNode("FileName");
            var argXE = (XmlElement)cmdXE.SelectSingleNode("Arguments");
            var seXE = (XmlElement)cmdXE.SelectSingleNode("ShellExecute");

            string command;
            var fileName = fnXE?.InnerText.Trim();
            var arguments = argXE?.InnerText.Trim();
            if (string.IsNullOrEmpty(fileName)) fileName = CreateCommandFile(fnXE);
            if (string.IsNullOrEmpty(arguments)) arguments = CreateCommandFile(argXE);
            fileName = Environment.ExpandEnvironmentVariables(fileName);
            arguments = Environment.ExpandEnvironmentVariables(arguments);
            var prefix = argXE?.GetAttribute("Prefix");//参数前缀
            var suffix = argXE?.GetAttribute("Suffix");//参数后缀
            arguments = prefix + arguments + suffix;
            if (seXE != null)
            {
                var verb = seXE.HasAttribute("Verb") ? seXE.GetAttribute("Verb") : "open";
                var windowStyle = seXE.HasAttribute("WindowStyle") ? Convert.ToInt32(seXE.GetAttribute("WindowStyle")) : 1;
                var directory = Environment.ExpandEnvironmentVariables(seXE.GetAttribute("Directory"));
                command = ShellExecuteDialog.GetCommand(fileName, arguments, verb, windowStyle, directory);
            }
            else
            {
                command = fileName;
                if (arguments != string.Empty) command += $" {arguments}";
            }
            Registry.SetValue(regPath, "", command);
        }

        private static string CreateCommandFile(XmlNode xe)
        {
            var path = string.Empty;
            if (xe == null) return path;
            foreach (XmlElement cfXE in xe.SelectNodes("CreateFile"))
            {
                if (!XmlDicHelper.FileExists(cfXE)) continue;
                if (!XmlDicHelper.JudgeCulture(cfXE)) continue;
                if (!XmlDicHelper.JudgeOSVersion(cfXE)) continue;
                var fileName = cfXE.GetAttribute("FileName");
                var content = cfXE.GetAttribute("Content");
                var extension = Path.GetExtension(fileName).ToLower();
                var filePath = $@"{AppConfig.ProgramsDir}\{fileName}";
                if (path == string.Empty) path = filePath;
                var encoding = extension switch
                {
                    ".bat" or ".cmd" => Encoding.Default,
                    _ => Encoding.Unicode,
                };
                File.Delete(filePath);
                File.WriteAllText(filePath, content, encoding);

            }
            return path;
        }
    }

    internal sealed class EnhanceShellExItem : FoldSubItem, IChkVisibleItem
    {
        public string RegPath => $@"{ShellExPath}\ContextMenuHandlers\{DefaultKeyName}";
        public string ShellExPath { get; set; }
        public string DefaultKeyName { get; set; }
        public Guid Guid { get; set; }
        public VisibleCheckBox ChkVisible { get; set; }

        public bool ItemVisible
        {
            get => ShellExItem.GetPathAndGuids(ShellExPath).Values.Contains(Guid);
            set
            {
                if (value)
                {
                    var regPath = ObjectPath.GetNewPathWithIndex(RegPath, ObjectPath.PathType.Registry);
                    Registry.SetValue(regPath, "", Guid.ToString("B"));
                }
                else
                {
                    var dic = ShellExItem.GetPathAndGuids(ShellExPath);
                    foreach (var regPath in dic.Keys)
                    {
                        if (dic[regPath].Equals(Guid))
                            RegistryEx.DeleteKeyTree(regPath);
                    }
                }
            }
        }

        public EnhanceShellExItem()
        {
            ChkVisible = new VisibleCheckBox(this);
            Indent();
        }
    }
}