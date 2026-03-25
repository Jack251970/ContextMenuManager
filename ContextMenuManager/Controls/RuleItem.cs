using ContextMenuManager.Methods;
using ContextMenuManager.Controls.Interfaces;
using Microsoft.Win32;
using System;
using iNKORE.UI.WPF.Modern.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ContextMenuManager.Controls
{
    internal class RuleItem : FoldSubItem, IBtnShowMenuItem, ITsiWebSearchItem
    {
        public RuleItem(MyList list, ItemInfo info) : base(list)
        {
            RestartExplorer = info.RestartExplorer;
            if (list != null)
            {
                Text = info.Text;
                Image = info.Image;
                BtnShowMenu = new MenuButton(this);
                TsiSearch = new WebSearchMenuItem(this);
                Control.ContextMenu = new ContextMenu();
                Control.ContextMenu.Items.Add(TsiSearch);
            }
        }

        public ContextMenu ContextMenu
        {
            get => Control.ContextMenu;
            set => Control.ContextMenu = value;
        }

        public WebSearchMenuItem TsiSearch { get; set; }
        public MenuButton BtnShowMenu { get; set; }

        public bool RestartExplorer { get; set; }

        public string SearchText
        {
            get
            {
                if (FoldGroupItem == null) return Text;
                else return $"{FoldGroupItem.Text} {Text}";
            }
        }
    }

    public struct ItemInfo
    {
        public string Text { get; set; }
        public System.Drawing.Image Image { get; set; }
        public string Tip { get; set; }
        public bool RestartExplorer { get; set; }
    }

    internal sealed class VisibleRegRuleItem : RuleItem, IChkVisibleItem, ITsiRegPathItem
    {
        public struct RegRule
        {
            public string RegPath { get; set; }
            public string ValueName { get; set; }
            public RegistryValueKind ValueKind { get; set; }
            public object TurnOnValue { get; set; }
            public object TurnOffValue { get; set; }
            public RegRule(string regPath, string valueName, object turnOnValue,
                object turnOffValue, RegistryValueKind valueKind = RegistryValueKind.DWord)
            {
                RegPath = regPath; ValueName = valueName;
                TurnOnValue = turnOnValue; TurnOffValue = turnOffValue;
                ValueKind = valueKind;
            }
        }

        public struct RuleAndInfo
        {
            public RegRule[] Rules { get; set; }
            public ItemInfo ItemInfo { get; set; }
        }

        private VisibleRegRuleItem(MyList list, ItemInfo info) : base(list, info)
        {
            if (list != null)
            {
                ChkVisible = new VisibleCheckBox(this);
                ToolTipBox.SetToolTip(ChkVisible, info.Tip);
                TsiRegLocation = new RegLocationMenuItem(this);
                foreach (var item in new Control[] { new RToolStripSeparator(), TsiRegLocation })
                {
                    ContextMenu.Items.Add(item);
                }
            }
        }

        public VisibleRegRuleItem(MyList list, RegRule[] rules, ItemInfo info)
            : this(list, info) { Rules = rules; }

        public VisibleRegRuleItem(MyList list, RegRule rule, ItemInfo info)
            : this(list, info) { Rules = [rule]; }

        public VisibleRegRuleItem(MyList list, RuleAndInfo ruleAndInfo)
            : this(list, ruleAndInfo.Rules, ruleAndInfo.ItemInfo) { }

        public RegRule[] Rules { get; set; }

        public VisibleCheckBox ChkVisible { get; set; }
        public RegLocationMenuItem TsiRegLocation { get; set; }

        public bool ItemVisible
        {
            get
            {
                for (var i = 0; i < Rules.Length; i++)
                {
                    var rule = Rules[i];
                    using var key = RegistryEx.GetRegistryKey(rule.RegPath);
                    var value = key?.GetValue(rule.ValueName)?.ToString().ToLower();
                    var turnOnValue = rule.TurnOnValue?.ToString().ToLower();
                    var turnOffValue = rule.TurnOffValue?.ToString().ToLower();
                    if (value == null || key.GetValueKind(rule.ValueName) != rule.ValueKind)
                    {
                        if (i < Rules.Length - 1) continue;
                    }
                    if (value == turnOnValue) return true;
                    if (value == turnOffValue) return false;
                }
                return true;
            }
            set
            {
                foreach (var rule in Rules)
                {
                    var data = value ? rule.TurnOnValue : rule.TurnOffValue;
                    if (data != null)
                    {
                        Registry.SetValue(rule.RegPath, rule.ValueName, data, rule.ValueKind);
                    }
                    else
                    {
                        RegistryEx.DeleteValue(rule.RegPath, rule.ValueName);
                    }
                }
                if (RestartExplorer) ExplorerRestarter.Show();
            }
        }

        public string RegPath => Rules[0].RegPath;
        public string ValueName => Rules[0].ValueName;
        private const string LM_SMWCPE = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer";
        private const string CU_SMWCPE = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer";
        private const string LM_SMWCE = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer";
        private const string CU_SMWCE = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer";
        private const string LM_SPMWE = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Explorer";
        private const string CU_SPMWE = @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\Explorer";

        public static readonly RuleAndInfo CustomFolder = new()
        {
            Rules = [
                new RegRule(LM_SMWCPE, "NoCustomizeThisFolder", null, 1),
                new RegRule(LM_SMWCPE, "NoCustomizeWebView", null, 1),
                new RegRule(CU_SMWCPE, "NoCustomizeThisFolder", null, 1),
                new RegRule(CU_SMWCPE, "NoCustomizeWebView", null, 1)
            ],
            ItemInfo = new ItemInfo
            {
                Text = AppString.Other.CustomFolder,
                Image = AppImage.Folder,
                Tip = AppString.Tip.CustomFolder,
                RestartExplorer = true
            }
        };

        public static readonly RuleAndInfo NetworkDrive = new()
        {
            Rules = [
                new RegRule(LM_SMWCPE, "NoNetConnectDisconnect", null, 1),
                new RegRule(CU_SMWCPE, "NoNetConnectDisconnect", null, 1)
            ],
            ItemInfo = new ItemInfo
            {
                Text = $"{ResourceString.GetDirectString("@AppResolver.dll,-8556")} && {ResourceString.GetDirectString("@AppResolver.dll,-8557")}",
                Image = AppImage.NetworkDrive,
                RestartExplorer = true
            }
        };

        public static readonly RuleAndInfo RecycleBinProperties = new()
        {
            Rules = [
                new RegRule(LM_SMWCPE, "NoPropertiesRecycleBin", null, 1),
                new RegRule(CU_SMWCPE, "NoPropertiesRecycleBin", null, 1)
            ],
            ItemInfo = new ItemInfo
            {
                Text = ResourceString.GetDirectString("@AppResolver.dll,-8553"),
                Image = AppImage.RecycleBin,
                RestartExplorer = true
            }
        };

        public static readonly RuleAndInfo SendToDrive = new()
        {
            Rules = [
                new RegRule(LM_SMWCPE, "NoDrivesInSendToMenu", null, 1),
                new RegRule(CU_SMWCPE, "NoDrivesInSendToMenu", null, 1)
            ],
            ItemInfo = new ItemInfo
            {
                Text = ResourceString.GetDirectString("@shell32.dll,-9309"),
                Image = AppImage.Drive,
                Tip = AppString.Tip.SendToDrive,
                RestartExplorer = true
            }
        };

        public static readonly RuleAndInfo DeferBuildSendTo = new()
        {
            Rules = [
                new RegRule(LM_SMWCE, "DelaySendToMenuBuild", null, 1),
                new RegRule(CU_SMWCE, "DelaySendToMenuBuild", null, 1)
            ],
            ItemInfo = new ItemInfo
            {
                Text = AppString.Other.BuildSendtoMenu,
                Image = AppImage.SendTo,
                Tip = AppString.Tip.BuildSendtoMenu
            }
        };

        public static readonly RuleAndInfo UseStoreOpenWith = new()
        {
            Rules = [
                new RegRule(LM_SPMWE, "NoUseStoreOpenWith", null, 1),
                new RegRule(CU_SPMWE, "NoUseStoreOpenWith", null, 1)
            ],
            ItemInfo = new ItemInfo
            {
                Text = ResourceString.GetDirectString("@shell32.dll,-5383"),
                Image = AppImage.MicrosoftStore
            }
        };
    }

    internal sealed class NumberRegRuleItem : RuleItem, ITsiRegPathItem
    {
        public struct RegRule
        {
            public string RegPath { get; set; }
            public string ValueName { get; set; }
            public RegistryValueKind ValueKind { get; set; }
            public int MaxValue { get; set; }
            public int MinValue { get; set; }
            public int DefaultValue { get; set; }
        }

        private readonly NumberBox NudValue;
        public RegLocationMenuItem TsiRegLocation { get; set; }

        public NumberRegRuleItem(MyList list, RegRule rule, ItemInfo info) : base(list, info)
        {
            Rule = rule;
            if (list != null)
            {
                NudValue = new()
                {
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    Width = 120,
                    SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden
                };
                AddCtr(NudValue);
                ToolTipBox.SetToolTip(NudValue, info.Tip);
                TsiRegLocation = new RegLocationMenuItem(this);
                foreach (var item in new Control[] { new RToolStripSeparator(), TsiRegLocation })
                {
                    ContextMenu.Items.Add(item);
                }
                NudValue.Maximum = rule.MaxValue;
                NudValue.Minimum = rule.MinValue;
                NudValue.ValueChanged += (sender, e) =>
                {
                    if (NudValue.Value == Rule.DefaultValue)
                    {
                        NudValue.FontWeight = FontWeights.Bold;
                    }
                    else
                    {
                        NudValue.FontWeight = FontWeights.Normal;
                    }
                    ItemValue = (int)NudValue.Value;
                };
                NudValue.Value = ItemValue;
            }
        }

        public string RegPath => Rule.RegPath;
        public string ValueName => Rule.ValueName;
        public RegRule Rule { get; set; }

        public int ItemValue
        {
            get
            {
                var value = Registry.GetValue(Rule.RegPath, Rule.ValueName, null);
                if (value == null) return Rule.DefaultValue;
                var num = Convert.ToInt32(value);
                if (num > Rule.MaxValue) return Rule.MaxValue;
                if (num < Rule.MinValue) return Rule.MinValue;
                else return num;
            }
            set
            {
                Registry.SetValue(Rule.RegPath, Rule.ValueName, value, Rule.ValueKind);
                if (RestartExplorer) ExplorerRestarter.Show();
            }
        }
    }

    internal sealed class StringRegRuleItem : RuleItem, ITsiRegPathItem
    {
        public struct RegRule
        {
            public string RegPath { get; set; }
            public string ValueName { get; set; }
        }

        private readonly Label LblValue;

        public RegLocationMenuItem TsiRegLocation { get; set; }

        public StringRegRuleItem(MyList list, RegRule rule, ItemInfo info) : base(list, info)
        {
            Rule = rule;
            if (list != null)
            {
                LblValue = new()
                {
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand,
                };
                AddCtr(LblValue);
                ToolTipBox.SetToolTip(LblValue, info.Tip);
                TsiRegLocation = new RegLocationMenuItem(this);
                foreach (var item in new Control[] { new RToolStripSeparator(), TsiRegLocation })
                {
                    ContextMenu.Items.Add(item);
                }
                LblValue.Content = ItemValue;
                LblValue.MouseDown += (sender, e) =>
                {
                    var dlg = new InputDialog
                    {
                        Title = AppString.Menu.ChangeText,
                        Text = ItemValue
                    };
                    if (dlg.ShowDialog() != true) return;
                    ItemValue = (string)(LblValue.Content = dlg.Text);
                };
            }
        }

        public string RegPath => Rule.RegPath;
        public string ValueName => Rule.ValueName;
        public RegRule Rule { get; set; }

        public string ItemValue
        {
            get => Registry.GetValue(Rule.RegPath, Rule.ValueName, null)?.ToString();
            set
            {
                Registry.SetValue(Rule.RegPath, Rule.ValueName, value);
                if (RestartExplorer) ExplorerRestarter.Show();
            }
        }
    }

    internal sealed class VisbleIniRuleItem : RuleItem, IChkVisibleItem
    {
        public struct IniRule
        {
            public string IniPath { get; set; }
            public string Section { get; set; }
            public string KeyName { get; set; }
            public string TurnOnValue { get; set; }
            public string TurnOffValue { get; set; }
        }

        public VisbleIniRuleItem(MyList list, IniRule rule, ItemInfo info) : base(list, info)
        {
            Rule = rule;
            IniWriter = new IniWriter(rule.IniPath);
            if (list != null)
            {
                ChkVisible = new VisibleCheckBox(this);
                ToolTipBox.SetToolTip(ChkVisible, info.Tip);
            }
        }

        public IniRule Rule { get; set; }
        public IniWriter IniWriter { get; set; }
        public VisibleCheckBox ChkVisible { get; set; }
        public bool ItemVisible
        {
            get => IniWriter.GetValue(Rule.Section, Rule.KeyName) == Rule.TurnOnValue;
            set
            {
                IniWriter.SetValue(Rule.Section, Rule.KeyName, value ? Rule.TurnOnValue : Rule.TurnOffValue);
                if (RestartExplorer) ExplorerRestarter.Show();
            }
        }
    }

    internal sealed class NumberIniRuleItem : RuleItem
    {
        public struct IniRule
        {
            public string IniPath { get; set; }
            public string Section { get; set; }
            public string KeyName { get; set; }
            public int MaxValue { get; set; }
            public int MinValue { get; set; }
            public int DefaultValue { get; set; }
        }

        public NumberIniRuleItem(MyList list, IniRule rule, ItemInfo info) : base(list, info)
        {
            Rule = rule;
            IniWriter = new IniWriter(rule.IniPath);
            if (list != null)
            {
                NudValue = new()
                {
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    Width = 120,
                    SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden
                };
                AddCtr(NudValue);
                ToolTipBox.SetToolTip(NudValue, info.Tip);
                NudValue.Maximum = rule.MaxValue;
                NudValue.Minimum = rule.MinValue;
                NudValue.ValueChanged += (sender, e) =>
                {
                    if (NudValue.Value == Rule.DefaultValue)
                    {
                        NudValue.FontWeight = FontWeights.Bold;
                    }
                    else
                    {
                        NudValue.FontWeight = FontWeights.Normal;
                    }
                    ItemValue = (int)NudValue.Value;
                };
                NudValue.Value = ItemValue;
            }
        }

        public IniRule Rule { get; set; }
        public IniWriter IniWriter { get; set; }

        private readonly NumberBox NudValue;

        public int ItemValue
        {
            get
            {
                var value = IniWriter.GetValue(Rule.Section, Rule.KeyName);
                if (string.IsNullOrWhiteSpace(value)) return Rule.DefaultValue;
                var num = Convert.ToInt32(value);
                if (num > Rule.MaxValue) return Rule.MaxValue;
                if (num < Rule.MinValue) return Rule.MinValue;
                else return num;
            }
            set
            {
                IniWriter.SetValue(Rule.Section, Rule.KeyName, value);
                if (RestartExplorer) ExplorerRestarter.Show();
            }
        }
    }

    internal sealed class StringIniRuleItem : RuleItem
    {
        public struct IniRule
        {
            public string IniPath { get; set; }
            public string Secation { get; set; }
            public string KeyName { get; set; }
        }

        private readonly Label LblValue;

        public StringIniRuleItem(MyList list, IniRule rule, ItemInfo info) : base(list, info)
        {
            Rule = rule;
            IniWriter = new IniWriter(rule.IniPath);
            if (list != null)
            {
                LblValue = new()
                {
                    BorderThickness = new Thickness(1),
                    Cursor = System.Windows.Input.Cursors.Hand,
                };
                AddCtr(LblValue);
                ToolTipBox.SetToolTip(LblValue, info.Tip);
                LblValue.Content = ItemValue;
                LblValue.MouseLeftButtonDown += (sender, e) =>
                {
                    var dlg = new InputDialog
                    {
                        Title = AppString.Menu.ChangeText,
                        Text = ItemValue
                    };
                    if (dlg.ShowDialog() != true) return;
                    ItemValue = (string)(LblValue.Content = dlg.Text);
                };
            }
        }

        public IniRule Rule { get; set; }
        public IniWriter IniWriter { get; set; }

        public string ItemValue
        {
            get => IniWriter.GetValue(Rule.Secation, Rule.KeyName);
            set
            {
                IniWriter.SetValue(Rule.Secation, Rule.KeyName, value);
                if (RestartExplorer) ExplorerRestarter.Show();
            }
        }
    }
}
