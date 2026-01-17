using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System;
using System.Drawing;
using System.Xml;

namespace ContextMenuManager.Controls
{
    internal sealed class EnhanceMenuList : SwitchDicList // 其他菜单 增强菜单
    {
        public string ScenePath { get; set; }

        public override void LoadItems()
        {
            base.LoadItems();
            var index = UseUserDic ? 1 : 0;
            var doc = XmlDicHelper.EnhanceMenusDic[index];
            if (doc?.DocumentElement == null) return;
            foreach (XmlNode xn in doc.DocumentElement.ChildNodes)
            {
                try
                {
                    Image image = null;
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
                    if (!string.IsNullOrEmpty(ScenePath) && !path.Equals(ScenePath, StringComparison.OrdinalIgnoreCase)) continue;

                    var iconLocation = xn.SelectSingleNode("Icon")?.InnerText;
                    using (var icon = ResourceIcon.GetIcon(iconLocation))
                    {
                        image = icon?.ToBitmap();
                        image = image ?? AppImage.NotFound;
                    }
                    var groupItem = new FoldGroupItem(path, ObjectPath.PathType.Registry)
                    {
                        Image = image,
                        Text = text
                    };
                    AddItem(groupItem);
                    var shellXN = xn.SelectSingleNode("Shell");
                    var shellExXN = xn.SelectSingleNode("ShellEx");
                    if (shellXN != null) LoadShellItems(shellXN, groupItem);
                    if (shellExXN != null) LoadShellExItems(shellExXN, groupItem);
                    groupItem.SetVisibleWithSubItemCount();
                }
                catch { continue; }
            }
        }

        private void LoadShellItems(XmlNode shellXN, FoldGroupItem groupItem)
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
                var itemText = item.Text;
                var itemVisible = item.ItemVisible;
                AddItem(item);
            }
        }

        private void LoadShellExItems(XmlNode shellExXN, FoldGroupItem groupItem)
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
                var itemText = item.Text;
                var itemVisible = item.ItemVisible;
                AddItem(item);
            }
        }
    }
}