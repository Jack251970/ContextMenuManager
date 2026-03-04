using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Controls;
using ContextMenuManager.Methods;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace ContextMenuManager
{
    internal sealed class MainForm : MyMainForm
    {
        private readonly SearchBox searchBox;
        private Control currentListControl;
        private readonly List<Control> originalListItems = new();
        private static readonly Dictionary<Type, PropertyInfo[]> TextPropertiesCache = new();
        private static readonly string[] PathPropertyNames = { "ItemFilePath", "RegPath", "GroupPath", "SelectedPath" };
        private static readonly Dictionary<Type, PropertyInfo[]> PathPropertiesCache = new();

        private readonly MyToolBarButton[] ToolBarButtons =
        {
            new(AppImage.Home, AppString.ToolBar.Home),
            new(AppImage.Type, AppString.ToolBar.Type),
            new(AppImage.Star, AppString.ToolBar.Rule),
            new(AppImage.Refresh, AppString.ToolBar.Refresh),
            new(AppImage.About, AppString.ToolBar.About)
        };

        private readonly ShellList shellList = new();
        private readonly ShellNewList shellNewList = new();
        private readonly SendToList sendToList = new();
        private readonly OpenWithList openWithList = new();
        private readonly WinXList winXList = new();
        private readonly EnhanceMenuList enhanceMenusList = new();
        private readonly DetailedEditList detailedEditList = new();
        private readonly GuidBlockedList guidBlockedList = new();
        private readonly IEList iEList = new();
        private readonly AppSettingBox appSettingBox = new();
        private readonly LanguagesBox languagesBox = new();
        private readonly DictionariesBox dictionariesBox = new();
        private readonly ReadOnlyRichTextBox aboutMeBox = new();
        private readonly DonateBox donateBox = new();
        private readonly BackupListBox backupListBox = new();
        private readonly ExplorerRestarter explorerRestarter = new();

        private readonly Control[] mainControls;

        private static readonly string[] GeneralItems =
        {
            AppString.SideBar.File, AppString.SideBar.Folder, AppString.SideBar.Directory,
            AppString.SideBar.Background, AppString.SideBar.Desktop, AppString.SideBar.Drive,
            AppString.SideBar.AllObjects, AppString.SideBar.Computer, AppString.SideBar.RecycleBin,
            AppString.SideBar.Library, null, AppString.SideBar.New, AppString.SideBar.SendTo,
            AppString.SideBar.OpenWith, null, AppString.SideBar.WinX
        };

        private static readonly string[] GeneralItemInfos =
        {
            AppString.StatusBar.File, AppString.StatusBar.Folder, AppString.StatusBar.Directory,
            AppString.StatusBar.Background, AppString.StatusBar.Desktop, AppString.StatusBar.Drive,
            AppString.StatusBar.AllObjects, AppString.StatusBar.Computer, AppString.StatusBar.RecycleBin,
            AppString.StatusBar.Library, null, AppString.StatusBar.New, AppString.StatusBar.SendTo,
            AppString.StatusBar.OpenWith, null, AppString.StatusBar.WinX
        };

        private static readonly string[] TypeItems =
        {
            AppString.SideBar.LnkFile, AppString.SideBar.UwpLnk, AppString.SideBar.ExeFile,
            AppString.SideBar.UnknownType, null, AppString.SideBar.CustomExtension,
            AppString.SideBar.PerceivedType, AppString.SideBar.DirectoryType, null, AppString.SideBar.MenuAnalysis
        };

        private static readonly string[] TypeItemInfos =
        {
            AppString.StatusBar.LnkFile, AppString.StatusBar.UwpLnk, AppString.StatusBar.ExeFile,
            AppString.StatusBar.UnknownType, null, AppString.StatusBar.CustomExtension,
            AppString.StatusBar.PerceivedType, AppString.StatusBar.DirectoryType, null, AppString.StatusBar.MenuAnalysis
        };

        private static readonly string[] OtherRuleItems =
        {
            AppString.SideBar.EnhanceMenu, AppString.SideBar.DetailedEdit, null,
            AppString.SideBar.DragDrop, AppString.SideBar.PublicReferences, AppString.SideBar.IEMenu,
            null, AppString.SideBar.GuidBlocked, AppString.SideBar.CustomRegPath
        };

        private static readonly string[] OtherRuleItemInfos =
        {
            AppString.StatusBar.EnhanceMenu, AppString.StatusBar.DetailedEdit, null,
            AppString.StatusBar.DragDrop, AppString.StatusBar.PublicReferences, AppString.StatusBar.IEMenu,
            null, AppString.StatusBar.GuidBlocked, AppString.StatusBar.CustomRegPath
        };

        private static readonly string[] AboutItems =
        {
            AppString.SideBar.AppSetting, AppString.SideBar.AppLanguage, AppString.SideBar.BackupRestore,
            AppString.SideBar.Dictionaries, AppString.SideBar.AboutApp, AppString.SideBar.Donate
        };

        private static readonly string[] SettingItems =
        {
            AppString.Other.TopMost, null, AppString.Other.ShowFilePath,
            AppString.Other.HideDisabledItems, null, AppString.Other.OpenMoreRegedit, AppString.Other.OpenMoreExplorer
        };

        private static readonly Scenes[] GeneralShellScenes =
        {
            Scenes.File, Scenes.Folder, Scenes.Directory, Scenes.Background, Scenes.Desktop,
            Scenes.Drive, Scenes.AllObjects, Scenes.Computer, Scenes.RecycleBin, Scenes.Library
        };

        private static readonly Scenes?[] TypeShellScenes =
        {
            Scenes.LnkFile, Scenes.UwpLnk, Scenes.ExeFile, Scenes.UnknownType, null,
            Scenes.CustomExtension, Scenes.PerceivedType, Scenes.DirectoryType, null, Scenes.MenuAnalysis
        };

        private readonly int[] lastItemIndex = new int[5];

        private readonly Dictionary<int, (Func<bool> GetState, Action<bool> SetState)> settingConfigs;

        private readonly Action[] aboutActions = new Action[6];

        public MainForm()
        {
            TopMost = AppConfig.TopMost;
            StartPosition = FormStartPosition.CenterScreen;
            Size = AppConfig.MainFormSize;
            Text = AppString.General.AppName;
            Controls.Add(explorerRestarter);
            ToolBar.AddButtons(ToolBarButtons);

            searchBox = new SearchBox { PlaceholderText = AppString.General.Search ?? "搜索..." };
            ToolBar.AddSearchBox(searchBox);

            mainControls = new Control[] { shellList, shellNewList, sendToList, openWithList, winXList,
                enhanceMenusList, detailedEditList, guidBlockedList, iEList, appSettingBox, languagesBox,
                dictionariesBox, aboutMeBox, donateBox, backupListBox };

            MainBody.Controls.AddRange(mainControls);
            ToolBarButtons[3].CanBeSelected = false;
            ToolBarButtons[3].MouseDown += (_, _) => RefreshApp();
            ToolBar.SelectedButtonChanged += (_, _) =>
            {
                searchBox.Clear();
                originalListItems.Clear();
                SwitchTab();
            };
            SideBar.HoverIndexChanged += (_, _) => ShowItemInfo();
            SideBar.SelectIndexChanged += (_, _) =>
            {
                searchBox.Clear();
                originalListItems.Clear();
                SwitchItem();
            };
            Shown += (_, _) => FirstRunDownloadLanguage();
            FormClosing += (_, _) => CloseMainForm();
            searchBox.TextChanged += (_, _) => FilterItems(searchBox.Text);

            settingConfigs = new Dictionary<int, (Func<bool> GetState, Action<bool> SetState)>
            {
                { 0, (() => TopMost, v => TopMost = AppConfig.TopMost = v) },
                { 2, (() => AppConfig.ShowFilePath, v => AppConfig.ShowFilePath = v) },
                { 3, (() => AppConfig.HideDisabledItems, v => AppConfig.HideDisabledItems = v) },
                { 5, (() => AppConfig.OpenMoreRegedit, v => AppConfig.OpenMoreRegedit = v) },
                { 6, (() => AppConfig.OpenMoreExplorer, v => AppConfig.OpenMoreExplorer = v) }
            };

            aboutActions[0] = () => { appSettingBox.LoadItems(); appSettingBox.Visible = true; };
            aboutActions[1] = () => { languagesBox.LoadLanguages(); languagesBox.Visible = true; };
            aboutActions[2] = () => { backupListBox.LoadItems(); backupListBox.Visible = true; };
            aboutActions[3] = () => { dictionariesBox.LoadText(); dictionariesBox.Visible = true; };
            aboutActions[4] = () => { aboutMeBox.LoadAboutInfo(); aboutMeBox.Visible = true; };
            aboutActions[5] = () => donateBox.Visible = true;

            HoveredToShowItemPath();
            DragDropToAnalysis();
            AddContextMenus();
            ResizeSideBar();
            JumpItem(0, 0);
        }

        public void JumpItem(int toolBarIndex, int sideBarIndex)
        {
            lastItemIndex[toolBarIndex] = sideBarIndex;
            var needSwitch = ToolBar.SelectedIndex != toolBarIndex || SideBar.SelectedIndex != sideBarIndex;
            ToolBar.SelectedIndex = toolBarIndex;
            if (needSwitch)
            {
                SideBar.SelectedIndex = sideBarIndex;
                SwitchItem();
            }
        }

        private void RefreshApp()
        {
            Cursor = Cursors.WaitCursor;
            ObjectPath.FilePathDic.Clear();
            AppConfig.ReloadConfig();
            GuidInfo.ReloadDics();
            XmlDicHelper.ReloadDics();
            SwitchItem();
            Cursor = Cursors.Default;
        }

        private void SwitchTab()
        {
            SideBar.ItemNames = ToolBar.SelectedIndex switch
            {
                0 => GeneralItems,
                1 => TypeItems,
                2 => OtherRuleItems,
                4 => AboutItems,
                _ => SideBar.ItemNames
            };
            SideBar.SelectedIndex = lastItemIndex[ToolBar.SelectedIndex];
        }

        private void SwitchItem()
        {
            originalListItems.Clear();

            Array.ForEach(mainControls, ctr =>
            {
                ctr.Visible = false;
                if (ctr is MyList list) list.ClearItems();
            });

            if (SideBar.SelectedIndex == -1) return;

            (ToolBar.SelectedIndex switch
            {
                0 => SwitchGeneralItem,
                1 => SwitchTypeItem,
                2 => SwitchOtherRuleItem,
                4 => SwitchAboutItem,
                _ => (Action)(() => { })
            })();

            lastItemIndex[ToolBar.SelectedIndex] = SideBar.SelectedIndex;
            SuspendMainBodyWhenMove = mainControls.Any(ctr => ctr.Controls.Count > 50);
        }

        private void ShowItemInfo()
        {
            StatusBar.Text = SideBar.HoveredIndex < 0 ? MyStatusBar.DefaultText :
                (ToolBar.SelectedIndex switch
                {
                    0 => GeneralItemInfos,
                    1 => TypeItemInfos,
                    2 => OtherRuleItemInfos,
                    _ => null
                })?[SideBar.HoveredIndex] ?? MyStatusBar.DefaultText;
        }

        private void HoveredToShowItemPath()
        {
            foreach (Control ctr in MainBody.Controls)
            {
                if (ctr is not MyList list || list == appSettingBox) continue;

                list.HoveredItemChanged += (_, _) =>
                {
                    if (!AppConfig.ShowFilePath) return;
                    var item = list.HoveredItem;
                    var path = GetFirstValidPath(item);
                    StatusBar.Text = path ?? item.Text;
                };
            }
        }

        private static string GetFirstValidPath(object item)
        {
            var type = item.GetType();

            if (!PathPropertiesCache.TryGetValue(type, out var properties))
            {
                properties = PathPropertyNames
                    .Select(name => type.GetProperty(name))
                    .Where(p => p != null)
                    .ToArray()!;
                PathPropertiesCache[type] = properties;
            }

            return properties
                .Select(prop => prop.GetValue(item, null)?.ToString())
                .FirstOrDefault(path => !path.IsNullOrWhiteSpace());
        }

        private void DragDropToAnalysis()
        {
            var droper = new ElevatedFileDroper(this);
            droper.DragDrop += (_, _) =>
            {
                ShellList.CurrentFileObjectPath = droper.DropFilePaths[0];
                JumpItem(1, 9);
            };
        }

        private void SwitchGeneralItem()
        {
            switch (SideBar.SelectedIndex)
            {
                case 11:
                    shellNewList.LoadItems();
                    ShowListAndSave(shellNewList);
                    break;
                case 12:
                    sendToList.LoadItems();
                    ShowListAndSave(sendToList);
                    break;
                case 13:
                    openWithList.LoadItems();
                    ShowListAndSave(openWithList);
                    break;
                case 15:
                    winXList.LoadItems();
                    ShowListAndSave(winXList);
                    break;
                default:
                    shellList.Scene = GeneralShellScenes[SideBar.SelectedIndex];
                    shellList.LoadItems();
                    ShowListAndSave(shellList);
                    break;
            }
        }

        private void SwitchTypeItem()
        {
            shellList.Scene = (Scenes)TypeShellScenes[SideBar.SelectedIndex];
            shellList.LoadItems();
            ShowListAndSave(shellList);
        }

        private void SwitchOtherRuleItem()
        {
            switch (SideBar.SelectedIndex)
            {
                case 0:
                    enhanceMenusList.ScenePath = null;
                    enhanceMenusList.LoadItems();
                    ShowListAndSave(enhanceMenusList);
                    break;
                case 1:
                    detailedEditList.GroupGuid = Guid.Empty;
                    detailedEditList.LoadItems();
                    ShowListAndSave(detailedEditList);
                    break;
                case 3:
                    shellList.Scene = Scenes.DragDrop;
                    shellList.LoadItems();
                    ShowListAndSave(shellList);
                    break;
                case 4:
                    shellList.Scene = Scenes.PublicReferences;
                    shellList.LoadItems();
                    ShowListAndSave(shellList);
                    break;
                case 5:
                    iEList.LoadItems();
                    ShowListAndSave(iEList);
                    break;
                case 7:
                    guidBlockedList.LoadItems();
                    ShowListAndSave(guidBlockedList);
                    break;
                case 8:
                    shellList.Scene = Scenes.CustomRegPath;
                    shellList.LoadItems();
                    ShowListAndSave(shellList);
                    break;
            }
        }

        private void SwitchAboutItem()
        {
            switch (SideBar.SelectedIndex)
            {
                case 0:
                    appSettingBox.LoadItems(); appSettingBox.Visible = true;
                    break;
                case 1:
                    languagesBox.LoadLanguages(); languagesBox.Visible = true;
                    break;
                case 2:
                    backupListBox.LoadItems(); backupListBox.Visible = true;
                    break;
                case 3:
                    dictionariesBox.LoadText(); dictionariesBox.Visible = true;
                    break;
                case 4:
                    if (aboutMeBox.TextLength == 0) aboutMeBox.LoadIni(AppString.Other.AboutApp);
                    aboutMeBox.Visible = true;
                    break;
                case 5:
                    donateBox.Visible = true;
                    break;
            }
            currentListControl = null;
            if (SideBar.SelectedIndex is >= 0 and <= 5)
                aboutActions[SideBar.SelectedIndex]();
        }

        private void ShowListAndSave(Control list)
        {
            list.Visible = true;
            currentListControl = list;
            originalListItems.Clear();
            if (list is MyList myList)
                originalListItems.AddRange(myList.Controls.Cast<Control>());
        }

        private void FilterItems(string filterText)
        {
            if (currentListControl is not MyList myList) return;

            if (string.IsNullOrWhiteSpace(filterText))
            {
                myList.Controls.Clear();
                myList.Controls.AddRange(originalListItems.ToArray());
                StatusBar.Text = MyStatusBar.DefaultText;
            }
            else
            {
                var searchComparison = StringComparison.OrdinalIgnoreCase;
                var itemsToShow = myList.Controls
                    .OfType<MyListItem>()
                    .Where(item => ContainsText(item.Text, filterText, searchComparison) ||
                                   ContainsText(item.SubText, filterText, searchComparison) ||
                                   ContainsTextInProperties(item, filterText, searchComparison))
                    .Cast<Control>()
                    .ToArray();

                myList.Controls.Clear();
                myList.Controls.AddRange(itemsToShow);

                StatusBar.Text = itemsToShow.Length switch
                {
                    0 => $"{AppString.General.NoResultsFor ?? "没有找到匹配"} \"{filterText}\"",
                    > 0 => $"找到 {itemsToShow.Length} 个匹配项",
                    _ => StatusBar.Text
                };
            }
        }

        private static bool ContainsText(string text, string search, StringComparison comparison)
            => !string.IsNullOrEmpty(text) && text.Contains(search, comparison);

        private static bool ContainsTextInProperties(MyListItem item, string search, StringComparison comparison)
        {
            var type = item.GetType();
            if (!TextPropertiesCache.TryGetValue(type, out var properties))
            {
                properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.PropertyType == typeof(string) && p.Name is not "Text" and not "SubText")
                    .ToArray();
                TextPropertiesCache[type] = properties;
            }

            return properties.Any(prop => prop.GetValue(item) is string value && value.Contains(search, comparison));
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.F) && searchBox != null)
            {
                searchBox.FocusTextBox();
                return true;
            }

            if (keyData == Keys.Escape && searchBox is { Text: { Length: > 0 } })
            {
                searchBox.Clear();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ResizeSideBar()
        {
            SideBar.Width = new[] { GeneralItems, TypeItems, OtherRuleItems, AboutItems }
                .SelectMany(items => items.Where(str => str != null))
                .Max(str => SideBar.GetItemWidth(str!));
        }

        private void AddContextMenus()
        {
            var menuConfigs = new (MyToolBarButton Button, string[] Items)[]
            {
                (ToolBarButtons[0], GeneralItems),
                (ToolBarButtons[1], TypeItems),
                (ToolBarButtons[2], OtherRuleItems),
                (ToolBarButtons[4], SettingItems)
            };

            foreach (var (button, items) in menuConfigs)
            {
                var cms = new ContextMenuStrip();
                var capturedButton = button;

                cms.MouseEnter += (_, _) =>
                {
                    if (capturedButton != ToolBar.SelectedButton)
                        capturedButton.Opacity = MyToolBar.HoveredOpacity;
                };
                cms.Closed += (_, _) =>
                {
                    if (capturedButton != ToolBar.SelectedButton)
                        capturedButton.Opacity = MyToolBar.UnSelctedOpacity;
                };
                button.MouseDown += (sender, e) =>
                {
                    if (e.Button != MouseButtons.Right || sender == ToolBar.SelectedButton) return;
                    cms.Show(button, e.Location);
                };

                for (var i = 0; i < items.Length; i++)
                {
                    if (items[i] == null)
                    {
                        cms.Items.Add(new RToolStripSeparator());
                        continue;
                    }

                    var tsi = new RToolStripMenuItem(items[i]);
                    cms.Items.Add(tsi);
                    var toolBarIndex = ToolBar.ButtonControls.GetChildIndex(button);
                    var index = i;

                    if (toolBarIndex != 4)
                    {
                        tsi.Click += (_, _) => JumpItem(toolBarIndex, index);
                        cms.Opening += (_, _) => tsi.Checked = lastItemIndex[toolBarIndex] == index;
                    }
                    else
                    {
                        SetupSettingMenuItem(tsi, index, cms);
                    }
                }
            }
        }

        private void SetupSettingMenuItem(RToolStripMenuItem tsi, int index, ContextMenuStrip cms)
        {
            tsi.Click += (_, _) =>
            {
                if (settingConfigs.TryGetValue(index, out var setting))
                {
                    setting.SetState(!tsi.Checked);
                    if (index == 3) SwitchItem();
                }
            };
            cms.Opening += (_, _) =>
            {
                tsi.Checked = settingConfigs.TryGetValue(index, out var setting) && setting.GetState();
            };
        }

        private void FirstRunDownloadLanguage()
        {
            if (!AppConfig.IsFirstRun || CultureInfo.CurrentUICulture.Name == "zh-CN") return;

            if (AppMessageBox.Show(
                "It is detected that you may be running this program for the first time,\n" +
                "and your system display language is not simplified Chinese (zh-CN),\n" +
                "do you need to download another language?",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                JumpItem(4, 1);
                languagesBox.ShowLanguageDialog();
            }
        }

        private void CloseMainForm()
        {
            if (explorerRestarter.Visible &&
                AppMessageBox.Show(explorerRestarter.Text, MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                ExternalProgram.RestartExplorer();
            }
            Opacity = 0;
            WindowState = FormWindowState.Normal;
            explorerRestarter.Visible = false;
            AppConfig.MainFormSize = Size;
        }
    }
}
