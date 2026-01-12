using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Controls;
using ContextMenuManager.Methods;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace ContextMenuManager
{
    sealed class MainForm : MyMainForm
    {
        public MainForm()
        {
            TopMost = AppConfig.TopMost;
            StartPosition = FormStartPosition.CenterScreen;
            Size = AppConfig.MainFormSize;
            Text = AppString.General.AppName;
            Controls.Add(explorerRestarter);
            ToolBar.AddButtons(ToolBarButtons);
            MainBody.Controls.AddRange(MainControls);
            ToolBarButtons[3].CanBeSelected = false;
            ToolBarButtons[3].MouseDown += (sender, e) => RefreshApp();
            ToolBar.SelectedButtonChanged += (sender, e) => SwitchTab();
            SideBar.HoverIndexChanged += (sender, e) => ShowItemInfo();
            SideBar.SelectIndexChanged += (sender, e) => SwitchItem();
            Shown += (sender, e) => FirstRunDownloadLanguage();
            FormClosing += (sender, e) => CloseMainForm();
            HoveredToShowItemPath();
            DragDropToAnalysis();
            AddContextMenus();
            ResizeSideBar();
            JumpItem(0, 0);
            InitTheme(true);
        }

        readonly MyToolBarButton[] ToolBarButtons =
        {
            new MyToolBarButton(AppImage.Home, AppString.ToolBar.Home),
            new MyToolBarButton(AppImage.Type, AppString.ToolBar.Type),
            new MyToolBarButton(AppImage.Star, AppString.ToolBar.Rule),
            new MyToolBarButton(AppImage.Refresh, AppString.ToolBar.Refresh),
            new MyToolBarButton(AppImage.About, AppString.ToolBar.About)
        };

        private TextBox currentSearchBox;

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
            if(flag1 || flag2 || ToolBar.SelectedIndex == -1)
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
            // 先隐藏所有控件
            foreach(Control ctr in MainControls)
            {
                ctr.Visible = false;
            }
            
            if(SideBar.SelectedIndex == -1) return;
            
            // 根据工具栏和侧边栏索引显示对应的控件
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
            
            // 只在需要搜索的页面添加搜索框
            AddSearchBoxToCurrentList();
            lastItemIndex[ToolBar.SelectedIndex] = SideBar.SelectedIndex;
            
            // 确保控件正确显示在界面上
            EnsureControlVisible();
        }
        
        private void EnsureControlVisible()
        {
            // 查找当前可见的控件
            var visibleControl = MainControls.FirstOrDefault(ctr => ctr.Visible);
            if (visibleControl != null)
            {
                visibleControl.BringToFront();
                visibleControl.Refresh();
                
                // 确保控件位置正确
                visibleControl.Dock = DockStyle.Fill;
            }
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
                    shellNewList.LoadItems(); 
                    shellNewList.Visible = true; 
                    break;
                case 12:
                    sendToList.LoadItems(); 
                    sendToList.Visible = true; 
                    break;
                case 13:
                    openWithList.LoadItems(); 
                    openWithList.Visible = true; 
                    break;
                case 15:
                    winXList.LoadItems(); 
                    winXList.Visible = true; 
                    break;
                default:
                    if (SideBar.SelectedIndex >= 0 && SideBar.SelectedIndex < GeneralShellScenes.Length)
                    {
                        shellList.Scene = GeneralShellScenes[SideBar.SelectedIndex];
                        shellList.LoadItems(); 
                        shellList.Visible = true; 
                    }
                    break;
            }
        }

        private void SwitchTypeItem()
        {
            if (SideBar.SelectedIndex >= 0 && SideBar.SelectedIndex < TypeShellScenes.Length)
            {
                var scene = TypeShellScenes[SideBar.SelectedIndex];
                if (scene != null)
                {
                    shellList.Scene = (Scenes)scene;
                    shellList.LoadItems();
                    shellList.Visible = true;
                }
            }
        }

        private void SwitchOtherRuleItem()
        {
            switch(SideBar.SelectedIndex)
            {
                case 0:
                    enhanceMenusList.ScenePath = null; 
                    enhanceMenusList.LoadItems(); 
                    enhanceMenusList.Visible = true; 
                    break;
                case 1:
                    detailedEditList.GroupGuid = Guid.Empty; 
                    detailedEditList.LoadItems(); 
                    detailedEditList.Visible = true; 
                    break;
                case 3:
                    shellList.Scene = Scenes.DragDrop; 
                    shellList.LoadItems(); 
                    shellList.Visible = true; 
                    break;
                case 4:
                    shellList.Scene = Scenes.PublicReferences; 
                    shellList.LoadItems(); 
                    shellList.Visible = true; 
                    break;
                case 5:
                    iEList.LoadItems(); 
                    iEList.Visible = true; 
                    break;
                case 7:
                    guidBlockedList.LoadItems(); 
                    guidBlockedList.Visible = true; 
                    break;
                case 8:
                    shellList.Scene = Scenes.CustomRegPath; 
                    shellList.LoadItems(); 
                    shellList.Visible = true; 
                    break;
            }
        }

        private void SwitchAboutItem()
        {
            switch(SideBar.SelectedIndex)
            {
                case 0:
                    appSettingBox.LoadItems(); 
                    appSettingBox.Visible = true;
                    break;
                case 1:
                    languagesBox.LoadLanguages(); 
                    languagesBox.Visible = true;
                    break;
                case 2:
                    backupListBox.LoadItems(); 
                    backupListBox.Visible = true;
                    break;
                case 3:
                    dictionariesBox.LoadText(); 
                    dictionariesBox.Visible = true;
                    break;
                case 4:
                    if(aboutMeBox.TextLength == 0) aboutMeBox.LoadIni(AppString.Other.AboutApp);
                    aboutMeBox.Visible = true;
                    break;
                case 5:
                    donateBox.Visible = true;
                    break;
            }
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
                        int toolBarIndex = Array.IndexOf(ToolBarButtons, item.Key);
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

        private void AddSearchBoxToCurrentList()
        {
            // 检查当前页面是否需要搜索框
            if (IsSearchablePage())
            {
                var currentList = GetCurrentList();
                if (currentList != null)
                {
                    var searchBox = new SearchBox();
                    searchBox.PlaceholderText = AppString.Dialog.Search;
                    
                    currentList.SearchBox = searchBox;
                    
                    // 从工具栏中移除旧的搜索框（如果存在）
                    var existingSearchPanels = ToolBar.Controls.OfType<Panel>().Where(p => p.Tag?.ToString() == "SearchBoxPanel").ToList();
                    foreach (var panel in existingSearchPanels)
                    {
                        ToolBar.Controls.Remove(panel);
                        panel.Dispose();
                    }
                    
                    // 添加搜索框到工具栏
                    ToolBar.AddSearchBox(searchBox);
                }
            }
            else
            {
                // 移除工具栏中的搜索框
                var existingSearchPanels = ToolBar.Controls.OfType<Panel>().Where(p => p.Tag?.ToString() == "SearchBoxPanel").ToList();
                foreach (var panel in existingSearchPanels)
                {
                    ToolBar.Controls.Remove(panel);
                    panel.Dispose();
                }
            }
        }

        // 检查当前页面是否需要搜索功能
        private bool IsSearchablePage()
        {
            switch (ToolBar.SelectedIndex)
            {
                case 0: // 主页
                    return SideBar.SelectedIndex != 15; // WinX页面可能有特殊处理
                case 1: // 文件类型
                    return true;
                case 2: // 其他规则
                    return SideBar.SelectedIndex != 2 && SideBar.SelectedIndex != 6; // 排除分隔符
                case 4: // 关于
                    return false; // 关于页面不需要搜索
                default:
                    return false;
            }
        }

        // 获取当前显示的可搜索列表
        private MyList GetCurrentList()
        {
            if (ToolBar.SelectedIndex == 0) // 主页
            {
                switch (SideBar.SelectedIndex)
                {
                    case 11: return shellNewList;
                    case 12: return sendToList;
                    case 13: return openWithList;
                    case 15: return winXList;
                    default: return shellList;
                }
            }
            else if (ToolBar.SelectedIndex == 1) // 文件类型
            {
                return shellList;
            }
            else if (ToolBar.SelectedIndex == 2) // 其他规则
            {
                switch (SideBar.SelectedIndex)
                {
                    case 0: return enhanceMenusList;
                    case 1: return detailedEditList;
                    case 3: // DragDrop
                    case 4: // PublicReferences
                    case 8: // CustomRegPath
                        return shellList;
                    case 5: return iEList;
                    case 7: return guidBlockedList;
                    default: return null;
                }
            }
            else // 关于页面或其他
            {
                return null;
            }
        }
    }
}