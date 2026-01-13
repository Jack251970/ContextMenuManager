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
    sealed class MainForm : MyMainForm
    {
        private SearchBox searchBox; // 添加搜索框成员变量
        private Control currentListControl; // 当前显示的列表控件
        private List<Control> originalListItems = new List<Control>(); // 保存原始列表项
        
        public MainForm()
        {
            TopMost = AppConfig.TopMost;
            StartPosition = FormStartPosition.CenterScreen;
            Size = AppConfig.MainFormSize;
            Text = AppString.General.AppName;
            Controls.Add(explorerRestarter);
            ToolBar.AddButtons(ToolBarButtons);
            
            // 创建并添加搜索框到工具栏
            searchBox = new SearchBox();
            searchBox.PlaceholderText = AppString.General.Search ?? "搜索...";
            ToolBar.AddSearchBox(searchBox);
            
            MainBody.Controls.AddRange(MainControls);
            ToolBarButtons[3].CanBeSelected = false;
            ToolBarButtons[3].MouseDown += (sender, e) => RefreshApp();
            ToolBar.SelectedButtonChanged += (sender, e) => 
            {
                searchBox.Clear(); // 切换标签页时清空搜索框
                originalListItems.Clear(); // 清除保存的原始列表项
                SwitchTab();
            };
            SideBar.HoverIndexChanged += (sender, e) => ShowItemInfo();
            SideBar.SelectIndexChanged += (sender, e) => 
            {
                searchBox.Clear(); // 切换侧边栏项时清空搜索框
                originalListItems.Clear(); // 清除保存的原始列表项
                SwitchItem();
            };
            Shown += (sender, e) => FirstRunDownloadLanguage();
            FormClosing += (sender, e) => CloseMainForm();
            
            // 监听搜索框文本变化事件
            searchBox.TextChanged += (sender, e) => FilterItems(searchBox.Text);
            
            HoveredToShowItemPath();
            DragDropToAnalysis();
            AddContextMenus();
            ResizeSideBar();
            JumpItem(0, 0);
        }

        readonly MyToolBarButton[] ToolBarButtons =
        {
            new MyToolBarButton(AppImage.Home, AppString.ToolBar.Home),
            new MyToolBarButton(AppImage.Type, AppString.ToolBar.Type),
            new MyToolBarButton(AppImage.Star, AppString.ToolBar.Rule),
            new MyToolBarButton(AppImage.Refresh, AppString.ToolBar.Refresh),
            new MyToolBarButton(AppImage.About, AppString.ToolBar.About)
        };

        private Control[] MainControls => new Control[]
        {
            shellList, shellNewList, sendToList, openWithList, winXList,
            enhanceMenusList, detailedEditList, guidBlockedList, iEList,
            appSettingBox, languagesBox, dictionariesBox, aboutMeBox, 
            donateBox, backupListBox
        };

        readonly ShellList shellList = new ShellList();
        readonly ShellNewList shellNewList = new ShellNewList();
        readonly SendToList sendToList = new SendToList();
        readonly OpenWithList openWithList = new OpenWithList();
        readonly WinXList winXList = new WinXList();

        readonly EnhanceMenuList enhanceMenusList = new EnhanceMenuList();
        readonly DetailedEditList detailedEditList = new DetailedEditList();
        readonly GuidBlockedList guidBlockedList = new GuidBlockedList();
        readonly IEList iEList = new IEList();

        readonly AppSettingBox appSettingBox = new AppSettingBox();
        readonly LanguagesBox languagesBox = new LanguagesBox();
        readonly DictionariesBox dictionariesBox = new DictionariesBox();
        readonly ReadOnlyRichTextBox aboutMeBox = new ReadOnlyRichTextBox();
        readonly DonateBox donateBox = new DonateBox();
        readonly BackupListBox backupListBox = new BackupListBox();
        readonly ExplorerRestarter explorerRestarter = new ExplorerRestarter();

        // 主页
        static readonly string[] GeneralItems =
        {
            AppString.SideBar.File,
            AppString.SideBar.Folder,
            AppString.SideBar.Directory,
            AppString.SideBar.Background,
            AppString.SideBar.Desktop,
            AppString.SideBar.Drive,
            AppString.SideBar.AllObjects,
            AppString.SideBar.Computer,
            AppString.SideBar.RecycleBin,
            AppString.SideBar.Library,
            null,
            AppString.SideBar.New,
            AppString.SideBar.SendTo,
            AppString.SideBar.OpenWith,
            null,
            AppString.SideBar.WinX
        };
        static readonly string[] GeneralItemInfos =
        {
            AppString.StatusBar.File,
            AppString.StatusBar.Folder,
            AppString.StatusBar.Directory,
            AppString.StatusBar.Background,
            AppString.StatusBar.Desktop,
            AppString.StatusBar.Drive,
            AppString.StatusBar.AllObjects,
            AppString.StatusBar.Computer,
            AppString.StatusBar.RecycleBin,
            AppString.StatusBar.Library,
            null,
            AppString.StatusBar.New,
            AppString.StatusBar.SendTo,
            AppString.StatusBar.OpenWith,
            null,
            AppString.StatusBar.WinX
        };

        // 文件类型
        static readonly string[] TypeItems =
        {
            AppString.SideBar.LnkFile,
            AppString.SideBar.UwpLnk,
            AppString.SideBar.ExeFile,
            AppString.SideBar.UnknownType,
            null,
            AppString.SideBar.CustomExtension,
            AppString.SideBar.PerceivedType,
            AppString.SideBar.DirectoryType,
            null,
            AppString.SideBar.MenuAnalysis
        };
        static readonly string[] TypeItemInfos =
        {
            AppString.StatusBar.LnkFile,
            AppString.StatusBar.UwpLnk,
            AppString.StatusBar.ExeFile,
            AppString.StatusBar.UnknownType,
            null,
            AppString.StatusBar.CustomExtension,
            AppString.StatusBar.PerceivedType,
            AppString.StatusBar.DirectoryType,
            null,
            AppString.StatusBar.MenuAnalysis
        };

        // 其他规则
        static readonly string[] OtherRuleItems =
        {
            AppString.SideBar.EnhanceMenu,
            AppString.SideBar.DetailedEdit,
            null,
            AppString.SideBar.DragDrop,
            AppString.SideBar.PublicReferences,
            AppString.SideBar.IEMenu,
            null,
            AppString.SideBar.GuidBlocked,
            AppString.SideBar.CustomRegPath,
        };
        static readonly string[] OtherRuleItemInfos =
        {
            AppString.StatusBar.EnhanceMenu,
            AppString.StatusBar.DetailedEdit,
            null,
            AppString.StatusBar.DragDrop,
            AppString.StatusBar.PublicReferences,
            AppString.StatusBar.IEMenu,
            null,
            AppString.StatusBar.GuidBlocked,
            AppString.StatusBar.CustomRegPath,
        };

        // 关于
        static readonly string[] AboutItems =
        {
            AppString.SideBar.AppSetting,
            AppString.SideBar.AppLanguage,
            AppString.SideBar.BackupRestore,
            AppString.SideBar.Dictionaries,
            AppString.SideBar.AboutApp,
            AppString.SideBar.Donate,
        };

        static readonly string[] SettingItems =
        {
            AppString.Other.TopMost,
            null,
            AppString.Other.ShowFilePath,
            AppString.Other.HideDisabledItems,
            null,
            AppString.Other.OpenMoreRegedit,
            AppString.Other.OpenMoreExplorer,
        };

        static readonly Scenes[] GeneralShellScenes =
        {
            Scenes.File,
            Scenes.Folder,
            Scenes.Directory,
            Scenes.Background,
            Scenes.Desktop,
            Scenes.Drive,
            Scenes.AllObjects,
            Scenes.Computer,
            Scenes.RecycleBin,
            Scenes.Library
        };

        static readonly Scenes?[] TypeShellScenes =
        {
            Scenes.LnkFile,
            Scenes.UwpLnk,
            Scenes.ExeFile,
            Scenes.UnknownType,
            null,
            Scenes.CustomExtension,
            Scenes.PerceivedType,
            Scenes.DirectoryType,
            null,
            Scenes.MenuAnalysis
        };

        readonly int[] lastItemIndex = new int[5];

        public void JumpItem(int toolBarIndex, int sideBarIndex)
        {
            bool flag1 = ToolBar.SelectedIndex == toolBarIndex;
            bool flag2 = SideBar.SelectedIndex == sideBarIndex;
            lastItemIndex[toolBarIndex] = sideBarIndex;
            ToolBar.SelectedIndex = toolBarIndex;
            if(flag1 || flag2)
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
            switch(ToolBar.SelectedIndex)
            {
                case 0:
                    SideBar.ItemNames = GeneralItems; break;
                case 1:
                    SideBar.ItemNames = TypeItems; break;
                case 2:
                    SideBar.ItemNames = OtherRuleItems; break;
                case 4:
                    SideBar.ItemNames = AboutItems; break;
            }
            SideBar.SelectedIndex = lastItemIndex[ToolBar.SelectedIndex];
        }

        private void SwitchItem()
        {
            // 清空原始列表项缓存
            originalListItems.Clear();
            
            foreach(Control ctr in MainControls)
            {
                ctr.Visible = false;
                if(ctr is MyList list) list.ClearItems();
            }
            if(SideBar.SelectedIndex == -1) return;
            switch(ToolBar.SelectedIndex)
            {
                case 0:
                    SwitchGeneralItem(); break;
                case 1:
                    SwitchTypeItem(); break;
                case 2:
                    SwitchOtherRuleItem(); break;
                case 4:
                    SwitchAboutItem(); break;
            }
            lastItemIndex[ToolBar.SelectedIndex] = SideBar.SelectedIndex;
            SuspendMainBodyWhenMove = MainControls.ToList().Any(ctr => ctr.Controls.Count > 50);
        }

        private void ShowItemInfo()
        {
            if(SideBar.HoveredIndex >= 0)
            {
                int i = SideBar.HoveredIndex;
                switch(ToolBar.SelectedIndex)
                {
                    case 0:
                        StatusBar.Text = GeneralItemInfos[i]; return;
                    case 1:
                        StatusBar.Text = TypeItemInfos[i]; return;
                    case 2:
                        StatusBar.Text = OtherRuleItemInfos[i]; return;
                }
            }
            StatusBar.Text = MyStatusBar.DefaultText;
        }

        private void HoveredToShowItemPath()
        {
            foreach(Control ctr in MainBody.Controls)
            {
                if(ctr is MyList list && list != appSettingBox)
                {
                    list.HoveredItemChanged += (sender, e) =>
                    {
                        if(!AppConfig.ShowFilePath) return;
                        MyListItem item = list.HoveredItem;
                        foreach(string prop in new[] { "ItemFilePath", "RegPath", "GroupPath", "SelectedPath" })
                        {
                            string path = item.GetType().GetProperty(prop)?.GetValue(item, null)?.ToString();
                            if(!path.IsNullOrWhiteSpace()) { StatusBar.Text = path; return; }
                        }
                        StatusBar.Text = item.Text;
                    };
                }
            }
        }

        private void DragDropToAnalysis()
        {
            var droper = new ElevatedFileDroper(this);
            droper.DragDrop += (sender, e) =>
            {
                ShellList.CurrentFileObjectPath = droper.DropFilePaths[0];
                JumpItem(1, 9);
            };
        }

        private void SwitchGeneralItem()
        {
            switch(SideBar.SelectedIndex)
            {
                case 11:
                    shellNewList.LoadItems(); shellNewList.Visible = true;
                    currentListControl = shellNewList;
                    SaveOriginalListItems();
                    break;
                case 12:
                    sendToList.LoadItems(); sendToList.Visible = true;
                    currentListControl = sendToList;
                    SaveOriginalListItems();
                    break;
                case 13:
                    openWithList.LoadItems(); openWithList.Visible = true;
                    currentListControl = openWithList;
                    SaveOriginalListItems();
                    break;
                case 15:
                    winXList.LoadItems(); winXList.Visible = true;
                    currentListControl = winXList;
                    SaveOriginalListItems();
                    break;
                default:
                    shellList.Scene = GeneralShellScenes[SideBar.SelectedIndex];
                    shellList.LoadItems(); shellList.Visible = true;
                    currentListControl = shellList;
                    SaveOriginalListItems();
                    break;
            }
        }

        private void SwitchTypeItem()
        {
            shellList.Scene = (Scenes)TypeShellScenes[SideBar.SelectedIndex];
            shellList.LoadItems();
            shellList.Visible = true;
            currentListControl = shellList;
            SaveOriginalListItems();
        }

        private void SwitchOtherRuleItem()
        {
            switch(SideBar.SelectedIndex)
            {
                case 0:
                    enhanceMenusList.ScenePath = null; enhanceMenusList.LoadItems(); enhanceMenusList.Visible = true;
                    currentListControl = enhanceMenusList;
                    SaveOriginalListItems();
                    break;
                case 1:
                    detailedEditList.GroupGuid = Guid.Empty; detailedEditList.LoadItems(); detailedEditList.Visible = true;
                    currentListControl = detailedEditList;
                    SaveOriginalListItems();
                    break;
                case 3:
                    shellList.Scene = Scenes.DragDrop; shellList.LoadItems(); shellList.Visible = true;
                    currentListControl = shellList;
                    SaveOriginalListItems();
                    break;
                case 4:
                    shellList.Scene = Scenes.PublicReferences; shellList.LoadItems(); shellList.Visible = true;
                    currentListControl = shellList;
                    SaveOriginalListItems();
                    break;
                case 5:
                    iEList.LoadItems(); iEList.Visible = true;
                    currentListControl = iEList;
                    SaveOriginalListItems();
                    break;
                case 7:
                    guidBlockedList.LoadItems(); guidBlockedList.Visible = true;
                    currentListControl = guidBlockedList;
                    SaveOriginalListItems();
                    break;
                case 8:
                    shellList.Scene = Scenes.CustomRegPath; shellList.LoadItems(); shellList.Visible = true;
                    currentListControl = shellList;
                    SaveOriginalListItems();
                    break;
            }
        }

        private void SwitchAboutItem()
        {
            switch(SideBar.SelectedIndex)
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
                    if(aboutMeBox.TextLength == 0) aboutMeBox.LoadIni(AppString.Other.AboutApp);
                    aboutMeBox.Visible = true;
                    break;
                case 5:
                    donateBox.Visible = true;
                    break;
            }
            currentListControl = null;
        }

        // 保存原始列表项
        private void SaveOriginalListItems()
        {
            originalListItems.Clear();
            
            if (currentListControl != null && currentListControl is MyList myList)
            {
                foreach (Control control in myList.Controls)
                {
                    originalListItems.Add(control);
                }
            }
        }

        // 过滤项目
        private void FilterItems(string filterText)
        {
            if (string.IsNullOrWhiteSpace(filterText))
            {
                // 如果搜索框为空，恢复显示所有原始项
                RestoreOriginalListItems();
                return;
            }
            
            string searchText = filterText.ToLower();
            
            // 根据当前列表控件进行过滤
            if (currentListControl != null && currentListControl is MyList myList)
            {
                FilterListItems(myList, searchText);
            }
        }
        
        // 恢复原始列表项
        private void RestoreOriginalListItems()
        {
            if (currentListControl != null && currentListControl is MyList myList)
            {
                // 先清空当前列表
                myList.Controls.Clear();
                
                // 恢复所有原始项
                foreach (var item in originalListItems)
                {
                    myList.Controls.Add(item);
                }
                
                // 恢复状态栏文本
                StatusBar.Text = MyStatusBar.DefaultText;
            }
        }
        
        private void FilterListItems(MyList listControl, string searchText)
        {
            // 遍历列表项并过滤
            var itemsToShow = new List<Control>();
            
            foreach (Control control in listControl.Controls)
            {
                if (control is MyListItem item)
                {
                    bool matches = false;
                    
                    // 检查主文本
                    if (item.Text != null && item.Text.ToLower().Contains(searchText))
                    {
                        matches = true;
                    }
                    
                    // 检查SubText
                    if (!matches && !string.IsNullOrEmpty(item.SubText) && item.SubText.ToLower().Contains(searchText))
                    {
                        matches = true;
                    }
                    
                    // 检查其他可能包含文本的属性
                    if (!matches)
                    {
                        // 可以通过反射检查其他文本属性
                        var properties = item.GetType().GetProperties();
                        foreach (var prop in properties)
                        {
                            if (prop.PropertyType == typeof(string))
                            {
                                // 排除 Text 属性，因为已经检查过了
                                if (prop.Name != "Text" && prop.Name != "SubText")
                                {
                                    var value = prop.GetValue(item) as string;
                                    if (value != null && value.ToLower().Contains(searchText))
                                    {
                                        matches = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    
                    if (matches)
                    {
                        itemsToShow.Add(item);
                    }
                }
            }
            
            // 清除当前列表并添加匹配的项
            listControl.Controls.Clear();
            foreach (var item in itemsToShow)
            {
                listControl.Controls.Add(item);
            }
            
            // 如果没有匹配项，显示提示
            if (itemsToShow.Count == 0 && !string.IsNullOrWhiteSpace(searchText))
            {
                StatusBar.Text = $"{AppString.General.NoResultsFor ?? "没有找到匹配"} \"{searchText}\"";
            }
            else if (itemsToShow.Count > 0)
            {
                StatusBar.Text = $"找到 {itemsToShow.Count} 个匹配项";
            }
        }
        
        // 添加快捷键支持
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // 全局搜索快捷键 Ctrl+F
            if (keyData == (Keys.Control | Keys.F))
            {
                searchBox?.FocusTextBox();
                return true;
            }
            
            // ESC 清除搜索框
            if (keyData == Keys.Escape && searchBox != null && !string.IsNullOrEmpty(searchBox.Text))
            {
                searchBox.Clear();
                return true;
            }
            
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ResizeSideBar()
        {
            SideBar.Width = 0;
            string[] strs = GeneralItems.Concat(TypeItems).Concat(OtherRuleItems).Concat(AboutItems).ToArray();
            Array.ForEach(strs, str => SideBar.Width = Math.Max(SideBar.Width, SideBar.GetItemWidth(str)));
        }

        private void AddContextMenus()
        {
            var dic = new Dictionary<MyToolBarButton, string[]>
            {
                { ToolBarButtons[0], GeneralItems },
                { ToolBarButtons[1], TypeItems },
                { ToolBarButtons[2], OtherRuleItems },
                { ToolBarButtons[4], SettingItems }
            };

            foreach(var item in dic)
            {
                ContextMenuStrip cms = new ContextMenuStrip();
                cms.MouseEnter += (sender, e) =>
                {
                    if(item.Key != ToolBar.SelectedButton) item.Key.Opacity = MyToolBar.HoveredOpacity;
                };
                cms.Closed += (sender, e) =>
                {
                    if(item.Key != ToolBar.SelectedButton) item.Key.Opacity = MyToolBar.UnSelctedOpacity;
                };
                item.Key.MouseDown += (sender, e) =>
                {
                    if(e.Button != MouseButtons.Right) return;
                    if(sender == ToolBar.SelectedButton) return;
                    cms.Show(item.Key, e.Location);
                };
                for(int i = 0; i < item.Value.Length; i++)
                {
                    if(item.Value[i] == null) cms.Items.Add(new RToolStripSeparator());
                    else
                    {
                        var tsi = new RToolStripMenuItem(item.Value[i]);
                        cms.Items.Add(tsi);
                        
                        // 修复：使用 ButtonControls 而不是 Controls
                        int toolBarIndex = ToolBar.ButtonControls.GetChildIndex(item.Key);
                        int index = i;
                        
                        if(toolBarIndex != 4)
                        {
                            tsi.Click += (sender, e) => JumpItem(toolBarIndex, index);
                            cms.Opening += (sender, e) => tsi.Checked = lastItemIndex[toolBarIndex] == index;
                        }
                        else
                        {
                            tsi.Click += (sender, e) =>
                            {
                                switch(index)
                                {
                                    case 0:
                                        AppConfig.TopMost = TopMost = !tsi.Checked; break;
                                    case 2:
                                        AppConfig.ShowFilePath = !tsi.Checked; break;
                                    case 3:
                                        AppConfig.HideDisabledItems = !tsi.Checked; SwitchItem(); break;
                                    case 5:
                                        AppConfig.OpenMoreRegedit = !tsi.Checked; break;
                                    case 6:
                                        AppConfig.OpenMoreExplorer = !tsi.Checked; break;
                                }
                            };
                            cms.Opening += (sender, e) =>
                            {
                                switch(index)
                                {
                                    case 0:
                                        tsi.Checked = TopMost; break;
                                    case 2:
                                        tsi.Checked = AppConfig.ShowFilePath; break;
                                    case 3:
                                        tsi.Checked = AppConfig.HideDisabledItems; break;
                                    case 5:
                                        tsi.Checked = AppConfig.OpenMoreRegedit; break;
                                    case 6:
                                        tsi.Checked = AppConfig.OpenMoreExplorer; break;
                                }
                            };
                        }
                    }
                }
            }
        }

        private void FirstRunDownloadLanguage()
        {
            if(AppConfig.IsFirstRun && CultureInfo.CurrentUICulture.Name != "zh-CN")
            {
                if(AppMessageBox.Show("It is detected that you may be running this program for the first time,\n" +
                    "and your system display language is not simplified Chinese (zh-CN),\n" +
                    "do you need to download another language?",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    JumpItem(4, 1);
                    languagesBox.ShowLanguageDialog();
                }
            }
        }

        private void CloseMainForm()
        {
            if(explorerRestarter.Visible && AppMessageBox.Show(explorerRestarter.Text,
                MessageBoxButtons.OKCancel) == DialogResult.OK) ExternalProgram.RestartExplorer();
            Opacity = 0;
            WindowState = FormWindowState.Normal;
            explorerRestarter.Visible = false;
            AppConfig.MainFormSize = Size;
        }
    }
}