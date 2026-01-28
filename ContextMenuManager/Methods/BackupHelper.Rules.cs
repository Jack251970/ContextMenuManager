using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Xml;

namespace ContextMenuManager.Methods
{
    internal sealed partial class BackupHelper
    {
        /*******************************DetailedEditList.cs************************************/

        private void GetDetailedEditListItems()
        {
            for (var index = 0; index < 2; index++)
            {
                // 获取系统字典或用户字典
                var doc = XmlDicHelper.DetailedEditDic[index];
                if (doc?.DocumentElement == null) return;
                // 遍历所有子节点
                foreach (XmlNode groupXN in doc.DocumentElement.ChildNodes)
                {
                    try
                    {
                        // 获取Guid列表
                        var guids = new List<Guid>();
                        var guidList = groupXN.SelectNodes("Guid");
                        foreach (XmlNode guidXN in guidList)
                        {
                            if (!GuidEx.TryParse(guidXN.InnerText, out var guid)) continue;
                            if (!File.Exists(GuidInfo.GetFilePath(guid))) continue;
                            guids.Add(guid);
                        }
                        if (guidList.Count > 0 && guids.Count == 0) continue;

                        // 获取groupItem列表
                        FoldGroupItem groupItem;
                        var isIniGroup = groupXN.SelectSingleNode("IsIniGroup") != null;
                        var attribute = isIniGroup ? "FilePath" : "RegPath";
                        var pathType = isIniGroup ? ObjectPath.PathType.File : ObjectPath.PathType.Registry;
                        groupItem = new FoldGroupItem(groupXN.SelectSingleNode(attribute)?.InnerText, pathType);

                        string GetRuleFullRegPath(string regPath)
                        {
                            if (string.IsNullOrEmpty(regPath)) regPath = groupItem.GroupPath;
                            else if (regPath.StartsWith("\\")) regPath = groupItem.GroupPath + regPath;
                            return regPath;
                        }
                        ;

                        // 遍历groupItem内所有Item节点
                        foreach (XmlElement itemXE in groupXN.SelectNodes("Item"))
                        {
                            try
                            {
                                if (!XmlDicHelper.JudgeOSVersion(itemXE)) continue;
                                RuleItem ruleItem;
                                var info = new ItemInfo();

                                // 获取文本、提示文本
                                foreach (XmlElement textXE in itemXE.SelectNodes("Text"))
                                {
                                    if (XmlDicHelper.JudgeCulture(textXE)) info.Text = ResourceString.GetDirectString(textXE.GetAttribute("Value"));
                                }
                                foreach (XmlElement tipXE in itemXE.SelectNodes("Tip"))
                                {
                                    if (XmlDicHelper.JudgeCulture(tipXE)) info.Tip = ResourceString.GetDirectString(tipXE.GetAttribute("Value"));
                                }
                                info.RestartExplorer = itemXE.SelectSingleNode("RestartExplorer") != null;

                                // 如果是数值类型的，初始化默认值、最大值、最小值
                                int defaultValue = 0, maxValue = 0, minValue = 0;
                                if (itemXE.SelectSingleNode("IsNumberItem") != null)
                                {
                                    var ruleXE = (XmlElement)itemXE.SelectSingleNode("Rule");
                                    defaultValue = ruleXE.HasAttribute("Default") ? Convert.ToInt32(ruleXE.GetAttribute("Default")) : 0;
                                    maxValue = ruleXE.HasAttribute("Max") ? Convert.ToInt32(ruleXE.GetAttribute("Max")) : int.MaxValue;
                                    minValue = ruleXE.HasAttribute("Min") ? Convert.ToInt32(ruleXE.GetAttribute("Min")) : int.MinValue;
                                }

                                // 建立三种类型的RuleItem
                                if (isIniGroup)
                                {
                                    var ruleXE = (XmlElement)itemXE.SelectSingleNode("Rule");
                                    var iniPath = ruleXE.GetAttribute("FilePath");
                                    if (iniPath.IsNullOrWhiteSpace()) iniPath = groupItem.GroupPath;
                                    var section = ruleXE.GetAttribute("Section");
                                    var keyName = ruleXE.GetAttribute("KeyName");
                                    if (itemXE.SelectSingleNode("IsNumberItem") != null)
                                    {
                                        var rule = new NumberIniRuleItem.IniRule
                                        {
                                            IniPath = iniPath,
                                            Section = section,
                                            KeyName = keyName,
                                            DefaultValue = defaultValue,
                                            MaxValue = maxValue,
                                            MinValue = maxValue
                                        };
                                        ruleItem = new NumberIniRuleItem(rule, info);
                                        var itemName = ruleItem.Text;
                                        var infoText = info.Text;
                                        var itemValue = ((NumberIniRuleItem)ruleItem).ItemValue;
                                        BackupRestoreItem(ruleItem, itemName, infoText, BackupItemType.NumberIniRuleItem, itemValue, currentScene);
                                    }
                                    else if (itemXE.SelectSingleNode("IsStringItem") != null)
                                    {
                                        var rule = new StringIniRuleItem.IniRule
                                        {
                                            IniPath = iniPath,
                                            Secation = section,
                                            KeyName = keyName
                                        };
                                        ruleItem = new StringIniRuleItem(rule, info);
                                        var itemName = ruleItem.Text;
                                        var infoText = info.Text;
                                        var itemValue = ((StringIniRuleItem)ruleItem).ItemValue;
                                        BackupRestoreItem(ruleItem, itemName, infoText, BackupItemType.StringIniRuleItem, itemValue, currentScene);
                                    }
                                    else
                                    {
                                        var rule = new VisbleIniRuleItem.IniRule
                                        {
                                            IniPath = iniPath,
                                            Section = section,
                                            KeyName = keyName,
                                            TurnOnValue = ruleXE.HasAttribute("On") ? ruleXE.GetAttribute("On") : null,
                                            TurnOffValue = ruleXE.HasAttribute("Off") ? ruleXE.GetAttribute("Off") : null,
                                        };
                                        ruleItem = new VisbleIniRuleItem(rule, info);
                                        var infoText = info.Text;
                                        var itemName = ruleItem.Text;
                                        var itemVisible = ((VisbleIniRuleItem)ruleItem).ItemVisible;
                                        BackupRestoreItem(ruleItem, itemName, infoText, BackupItemType.VisbleIniRuleItem, itemVisible, currentScene);
                                    }
                                }
                                else
                                {
                                    if (itemXE.SelectSingleNode("IsNumberItem") != null)
                                    {
                                        var ruleXE = (XmlElement)itemXE.SelectSingleNode("Rule");
                                        var rule = new NumberRegRuleItem.RegRule
                                        {
                                            RegPath = GetRuleFullRegPath(ruleXE.GetAttribute("RegPath")),
                                            ValueName = ruleXE.GetAttribute("ValueName"),
                                            ValueKind = XmlDicHelper.GetValueKind(ruleXE.GetAttribute("ValueKind"), RegistryValueKind.DWord),
                                            DefaultValue = defaultValue,
                                            MaxValue = maxValue,
                                            MinValue = minValue
                                        };
                                        ruleItem = new NumberRegRuleItem(rule, info);
                                        var itemName = ruleItem.Text;
                                        var infoText = info.Text;
                                        var itemValue = ((NumberRegRuleItem)ruleItem).ItemValue;// 备份值
                                        BackupRestoreItem(ruleItem, itemName, infoText, BackupItemType.NumberRegRuleItem, itemValue, currentScene);
                                    }
                                    else if (itemXE.SelectSingleNode("IsStringItem") != null)
                                    {
                                        var ruleXE = (XmlElement)itemXE.SelectSingleNode("Rule");
                                        var rule = new StringRegRuleItem.RegRule
                                        {
                                            RegPath = GetRuleFullRegPath(ruleXE.GetAttribute("RegPath")),
                                            ValueName = ruleXE.GetAttribute("ValueName"),
                                        };
                                        ruleItem = new StringRegRuleItem(rule, info);
                                        var itemName = ruleItem.Text;
                                        var infoText = info.Text;
                                        var itemValue = ((StringRegRuleItem)ruleItem).ItemValue; // 备份值
                                        BackupRestoreItem(ruleItem, itemName, infoText, BackupItemType.StringRegRuleItem, itemValue, currentScene);
                                    }
                                    else
                                    {
                                        var ruleXNList = itemXE.SelectNodes("Rule");
                                        var rules = new VisibleRegRuleItem.RegRule[ruleXNList.Count];
                                        for (var i = 0; i < ruleXNList.Count; i++)
                                        {
                                            var ruleXE = (XmlElement)ruleXNList[i];
                                            rules[i] = new VisibleRegRuleItem.RegRule
                                            {
                                                RegPath = GetRuleFullRegPath(ruleXE.GetAttribute("RegPath")),
                                                ValueName = ruleXE.GetAttribute("ValueName"),
                                                ValueKind = XmlDicHelper.GetValueKind(ruleXE.GetAttribute("ValueKind"), RegistryValueKind.DWord)
                                            };
                                            var turnOn = ruleXE.HasAttribute("On") ? ruleXE.GetAttribute("On") : null;
                                            var turnOff = ruleXE.HasAttribute("Off") ? ruleXE.GetAttribute("Off") : null;
                                            switch (rules[i].ValueKind)
                                            {
                                                case RegistryValueKind.Binary:
                                                    rules[i].TurnOnValue = turnOn != null ? XmlDicHelper.ConvertToBinary(turnOn) : null;
                                                    rules[i].TurnOffValue = turnOff != null ? XmlDicHelper.ConvertToBinary(turnOff) : null;
                                                    break;
                                                case RegistryValueKind.DWord:
                                                    if (turnOn == null) rules[i].TurnOnValue = null;
                                                    else rules[i].TurnOnValue = Convert.ToInt32(turnOn);
                                                    if (turnOff == null) rules[i].TurnOffValue = null;
                                                    else rules[i].TurnOffValue = Convert.ToInt32(turnOff);
                                                    break;
                                                default:
                                                    rules[i].TurnOnValue = turnOn;
                                                    rules[i].TurnOffValue = turnOff;
                                                    break;
                                            }
                                        }
                                        ruleItem = new VisibleRegRuleItem(rules, info);
                                        var itemName = ruleItem.Text;
                                        var infoText = info.Text;
                                        var itemVisible = ((VisibleRegRuleItem)ruleItem).ItemVisible;  // 备份值
                                        BackupRestoreItem(ruleItem, itemName, infoText, BackupItemType.VisibleRegRuleItem, itemVisible, currentScene);
                                    }
                                }
                                groupItem.Dispose();
                            }
                            catch { continue; }
                        }
                    }
                    catch { continue; }
                }
            }
        }

        /*******************************EnhanceMenusListList.cs************************************/

        private void GetEnhanceMenuListItems()
        {
            for (var index = 0; index < 2; index++)
            {
                var doc = XmlDicHelper.EnhanceMenusDic[index];
                if (doc?.DocumentElement == null) return;
                foreach (XmlNode xn in doc.DocumentElement.ChildNodes)
                {
                    try
                    {
                        string text = null;
                        var path = xn.SelectSingleNode("RegPath")?.InnerText;
                        foreach (XmlElement textXE in xn.SelectNodes("Text"))
                        {
                            if (XmlDicHelper.JudgeCulture(textXE))
                            {
                                text = ResourceString.GetDirectString(textXE.GetAttribute("Value"));
                            }
                        }
                        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(text)) continue;

                        var groupItem = new FoldGroupItem(path, ObjectPath.PathType.Registry)
                        {
                            Image = null,
                            Text = text
                        };
                        var shellXN = xn.SelectSingleNode("Shell");
                        var shellExXN = xn.SelectSingleNode("ShellEx");
                        if (shellXN != null) GetEnhanceMenuListShellItems(shellXN, groupItem);
                        if (shellExXN != null) GetEnhanceMenuListShellExItems(shellExXN, groupItem);
                        groupItem.Dispose();
                    }
                    catch { continue; }
                }
            }
        }

        private void GetEnhanceMenuListShellItems(XmlNode shellXN, FoldGroupItem groupItem)
        {
            foreach (XmlElement itemXE in shellXN.SelectNodes("Item"))
            {
                if (!XmlDicHelper.FileExists(itemXE)) continue;
                if (!XmlDicHelper.JudgeCulture(itemXE)) continue;
                if (!XmlDicHelper.JudgeOSVersion(itemXE)) continue;
                var keyName = itemXE.GetAttribute("KeyName");
                if (keyName.IsNullOrWhiteSpace()) continue;
                var item = new EnhanceShellItem()
                {
                    RegPath = $@"{groupItem.GroupPath}\shell\{keyName}",
                    FoldGroupItem = groupItem,
                    ItemXE = itemXE
                };
                foreach (XmlElement szXE in itemXE.SelectNodes("Value/REG_SZ"))
                {
                    if (!XmlDicHelper.JudgeCulture(szXE)) continue;
                    if (szXE.HasAttribute("MUIVerb")) item.Text = ResourceString.GetDirectString(szXE.GetAttribute("MUIVerb"));
                    if (szXE.HasAttribute("Icon")) item.Image = ResourceIcon.GetIcon(szXE.GetAttribute("Icon"))?.ToBitmap();
                    else if (szXE.HasAttribute("HasLUAShield")) item.Image = AppImage.Shield;
                }
                if (item.Image == null)
                {
                    var cmdXE = (XmlElement)itemXE.SelectSingleNode("SubKey/Command");
                    if (cmdXE != null)
                    {
                        Icon icon = null;
                        if (cmdXE.HasAttribute("Default"))
                        {
                            var filePath = ObjectPath.ExtractFilePath(cmdXE.GetAttribute("Default"));
                            icon = ResourceIcon.GetIcon(filePath);
                        }
                        else
                        {
                            var fileXE = cmdXE.SelectSingleNode("FileName");
                            if (fileXE != null)
                            {
                                var filePath = ObjectPath.ExtractFilePath(fileXE.InnerText);
                                icon = ResourceIcon.GetIcon(filePath);
                            }
                        }
                        item.Image = icon?.ToBitmap();
                        icon?.Dispose();
                    }
                }
                if (item.Image == null) item.Image = AppImage.NotFound;
                if (item.Text.IsNullOrWhiteSpace()) item.Text = keyName;
                var tip = "";
                foreach (XmlElement tipXE in itemXE.SelectNodes("Tip"))
                {
                    if (XmlDicHelper.JudgeCulture(tipXE)) tip = tipXE.GetAttribute("Value");
                }
                if (itemXE.GetElementsByTagName("CreateFile").Count > 0)
                {
                    if (!tip.IsNullOrWhiteSpace()) tip += "\n";
                    tip += AppString.Tip.CommandFiles;
                }
                ToolTipBox.SetToolTip(item.ChkVisible, tip);
                var itemName = item.Text;
                var regPath = item.RegPath;
                var pathSegments = regPath.Split('\\');
                var index = Array.LastIndexOf(pathSegments, "shell");
                string itemKey;
                if (index != -1 && index < pathSegments.Length - 1)
                {
                    var targetFields = new string[pathSegments.Length - index];
                    Array.Copy(pathSegments, index, targetFields, 0, targetFields.Length);
                    itemKey = string.Join("\\", targetFields);
                }
                else
                {
                    itemKey = regPath;
                }
                var itemVisible = item.ItemVisible;
                BackupRestoreItem(item, itemName, itemKey, BackupItemType.EnhanceShellItem, itemVisible, currentScene);
            }
        }

        private void GetEnhanceMenuListShellExItems(XmlNode shellExXN, FoldGroupItem groupItem)
        {
            foreach (XmlNode itemXN in shellExXN.SelectNodes("Item"))
            {
                if (!XmlDicHelper.FileExists(itemXN)) continue;
                if (!XmlDicHelper.JudgeCulture(itemXN)) continue;
                if (!XmlDicHelper.JudgeOSVersion(itemXN)) continue;
                if (!GuidEx.TryParse(itemXN.SelectSingleNode("Guid")?.InnerText, out var guid)) continue;
                var item = new EnhanceShellExItem
                {
                    FoldGroupItem = groupItem,
                    ShellExPath = $@"{groupItem.GroupPath}\ShellEx",
                    Image = ResourceIcon.GetIcon(itemXN.SelectSingleNode("Icon")?.InnerText)?.ToBitmap() ?? AppImage.SystemFile,
                    DefaultKeyName = itemXN.SelectSingleNode("KeyName")?.InnerText,
                    Guid = guid
                };
                foreach (XmlNode textXE in itemXN.SelectNodes("Text"))
                {
                    if (XmlDicHelper.JudgeCulture(textXE))
                    {
                        item.Text = ResourceString.GetDirectString(textXE.InnerText);
                    }
                }
                if (item.Text.IsNullOrWhiteSpace()) item.Text = GuidInfo.GetText(guid);
                if (item.DefaultKeyName.IsNullOrWhiteSpace()) item.DefaultKeyName = guid.ToString("B");
                var tip = "";
                foreach (XmlElement tipXE in itemXN.SelectNodes("Tip"))
                {
                    if (XmlDicHelper.JudgeCulture(tipXE)) tip = tipXE.GetAttribute("Text");
                }
                ToolTipBox.SetToolTip(item.ChkVisible, tip);
                var itemName = item.Text;
                var regPath = item.RegPath;
                var pathSegments = regPath.Split('\\');
                var index = Array.LastIndexOf(pathSegments, "ShellEx");
                string itemKey;
                if (index != -1 && index < pathSegments.Length - 1)
                {
                    var targetFields = new string[pathSegments.Length - index];
                    Array.Copy(pathSegments, index, targetFields, 0, targetFields.Length);
                    itemKey = string.Join("\\", targetFields);
                }
                else
                {
                    itemKey = regPath;
                }
                var itemVisible = item.ItemVisible;
                BackupRestoreItem(item, itemName, itemKey, BackupItemType.EnhanceShellExItem, itemVisible, currentScene);
            }
        }
    }
}
