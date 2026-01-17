using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace ContextMenuManager.Controls
{
    internal sealed class DetailedEditList : SwitchDicList // 其他菜单 详细编辑
    {
        public Guid GroupGuid { get; set; }

        public override void LoadItems()
        {
            base.LoadItems();
            var index = UseUserDic ? 1 : 0;
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
                        if (GroupGuid != Guid.Empty && GroupGuid != guid) continue;
                        guids.Add(guid);
                    }
                    if (guidList.Count > 0 && guids.Count == 0) continue;

                    // 获取groupItem列表
                    FoldGroupItem groupItem;
                    var isIniGroup = groupXN.SelectSingleNode("IsIniGroup") != null;
                    var attribute = isIniGroup ? "FilePath" : "RegPath";
                    var pathType = isIniGroup ? ObjectPath.PathType.File : ObjectPath.PathType.Registry;
                    groupItem = new FoldGroupItem(groupXN.SelectSingleNode(attribute)?.InnerText, pathType);
                    foreach (XmlElement textXE in groupXN.SelectNodes("Text"))
                    {
                        if (XmlDicHelper.JudgeCulture(textXE)) groupItem.Text = ResourceString.GetDirectString(textXE.GetAttribute("Value"));
                    }
                    if (guids.Count > 0)
                    {
                        groupItem.Tag = guids;
                        if (groupItem.Text.IsNullOrWhiteSpace()) groupItem.Text = GuidInfo.GetText(guids[0]);
                        groupItem.Image = GuidInfo.GetImage(guids[0]);
                        var filePath = GuidInfo.GetFilePath(guids[0]);
                        var clsidPath = GuidInfo.GetClsidPath(guids[0]);
                        if (filePath != null || clsidPath != null) groupItem.ContextMenuStrip.Items.Add(new RToolStripSeparator());
                        if (filePath != null)
                        {
                            var tsi = new RToolStripMenuItem(AppString.Menu.FileLocation);
                            // 打开文件夹
                            tsi.Click += (sender, e) => ExternalProgram.JumpExplorer(filePath, AppConfig.OpenMoreExplorer);
                            groupItem.ContextMenuStrip.Items.Add(tsi);
                        }
                        if (clsidPath != null)
                        {
                            var tsi = new RToolStripMenuItem(AppString.Menu.ClsidLocation);
                            // 打开注册表
                            tsi.Click += (sender, e) => ExternalProgram.JumpRegEdit(clsidPath, null, AppConfig.OpenMoreRegedit);
                            groupItem.ContextMenuStrip.Items.Add(tsi);
                        }
                    }
                    var iconXN = groupXN.SelectSingleNode("Icon");
                    using (var icon = ResourceIcon.GetIcon(iconXN?.InnerText))
                    {
                        if (icon != null) groupItem.Image = icon.ToBitmap();
                    }
                    AddItem(groupItem);

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
                                            RegPath = GetRuleFullRegPath(ruleXE.GetAttribute("RegPath")),   // 主索引
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
                                }
                            }
                            AddItem(ruleItem);
                            ruleItem.FoldGroupItem = groupItem;
                            ruleItem.HasImage = ruleItem.Image != null;
                            ruleItem.Indent();
                        }
                        catch { continue; }
                    }
                    groupItem.SetVisibleWithSubItemCount();
                }
                catch { continue; }
            }
        }
    }
}