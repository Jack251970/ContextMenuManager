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
            InitializeForm();
            InitializeEvents();
            InitializeSearch();
            JumpItem(0, 0);
            InitTheme(true);
        }

        readonly MyToolBarButton[] ToolBarButtons = {
            new MyToolBarButton(AppImage.Home, AppString.ToolBar.Home),
            new MyToolBarButton(AppImage.Type, AppString.ToolBar.Type),
            new MyToolBarButton(AppImage.Star, AppString.ToolBar.Rule),
            new MyToolBarButton(AppImage.Refresh, AppString.ToolBar.Refresh),
            new MyToolBarButton(AppImage.About, AppString.ToolBar.About)
        };

        private Control[] MainControls => new Control[] {
            shellList, shellNewList, sendToList, openWithList, winXList,
            enhanceMenusList, detailedEditList, guidBlockedList, iEList,
            appSettingBox, languagesBox, dictionariesBox, aboutMeBox,
            donateBox, backupListBox
        };

        // 控件实例
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

        // 配置数据
        static readonly string[] GeneralItems = {
            AppString.SideBar.File, AppString.SideBar.Folder, AppString.SideBar.Directory,
            AppString.SideBar.Background, AppString.SideBar.Desktop, AppString.SideBar.Drive,
            AppString.SideBar.AllObjects, AppString.SideBar.Computer, AppString.SideBar.RecycleBin,
            AppString.SideBar.Library, null, AppString.SideBar.New, AppString.SideBar.SendTo,
            AppString.SideBar.OpenWith, null, AppString.SideBar.WinX
        };

        static readonly string[] TypeItems = {
            AppString.SideBar.LnkFile, AppString.SideBar.UwpLnk, AppString.SideBar.ExeFile,
            AppString.SideBar.UnknownType, null, AppString.SideBar.CustomExtension,
            AppString.SideBar.PerceivedType, AppString.SideBar.DirectoryType, null,
            AppString.SideBar.MenuAnalysis
        };

        static readonly string[] OtherRuleItems = {
            AppString.SideBar.EnhanceMenu, AppString.SideBar.DetailedEdit, null,
            AppString.SideBar.DragDrop, AppString.SideBar.PublicReferences, AppString.SideBar.IEMenu,
            null, AppString.SideBar.GuidBlocked, AppString.SideBar.CustomRegPath
        };

        static readonly string[] AboutItems = {
            AppString.SideBar.AppSetting, AppString.SideBar.AppLanguage,
            AppString.SideBar.BackupRestore, AppString.SideBar.Dictionaries,
            AppString.SideBar.AboutApp, AppString.SideBar.Donate
        };

        static readonly Scenes[] GeneralShellScenes = {
            Scenes.File, Scenes.Folder, Scenes.Directory, Scenes.Background, Scenes.Desktop,
            Scenes.Drive, Scenes.AllObjects, Scenes.Computer, Scenes.RecycleBin, Scenes.Library
        };

        static readonly Scenes?[] TypeShellScenes = {
            Scenes.LnkFile, Scenes.UwpLnk, Scenes.ExeFile, Scenes.UnknownType, null,
            Scenes.CustomExtension, Scenes.PerceivedType, Scenes.DirectoryType, null,
            Scenes.MenuAnalysis
        };

        readonly int[] lastItemIndex = new int[5];
        private string lastSearchText = string.Empty;
        private bool isSearching = false;

        private void InitializeForm()
        {
            TopMost = AppConfig.TopMost;
            StartPosition = FormStartPosition.CenterScreen;
            Size = AppConfig.MainFormSize;
            Text = AppString.General.AppName;
            Controls.Add(explorerRestarter);
            ToolBar.AddButtons(ToolBarButtons);
            MainBody.Controls.AddRange(MainControls);
            ToolBarButtons[3].CanBeSelected = false;
            ResizeSideBar();
        }

        private void InitializeEvents()
        {
            ToolBarButtons[3].MouseDown += (sender, e) => RefreshApp();
            ToolBar.SelectedButtonChanged += (sender, e) => SwitchTab();
            SideBar.HoverIndexChanged += (sender, e) => ShowItemInfo();
            SideBar.SelectIndexChanged += (sender, e) => SwitchItem();
            Shown += (sender, e) => FirstRunDownloadLanguage();
            FormClosing += (sender, e) => CloseMainForm();
            DragDropToAnalysis();
            AddContextMenus();
        }

        public void JumpItem(int toolBarIndex, int sideBarIndex)
        {
            bool flag1 = ToolBar.SelectedIndex == toolBarIndex;
            bool flag2 = SideBar.SelectedIndex == sideBarIndex;
            lastItemIndex[toolBarIndex] = sideBarIndex;
            ToolBar.SelectedIndex = toolBarIndex;
            if (flag1 || flag2)
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
            switch (ToolBar.SelectedIndex)
            {
                case 0: SideBar.ItemNames = GeneralItems; break;
                case 1: SideBar.ItemNames = TypeItems; break;
                case 2: SideBar.ItemNames = OtherRuleItems; break;
                case 4: SideBar.ItemNames = AboutItems; break;
            }
            SideBar.SelectedIndex = lastItemIndex[ToolBar.SelectedIndex];
        }

        private void SwitchItem()
        {
            if (isSearching) ClearSearch();

            foreach (Control ctr in MainControls)
            {
                ctr.Visible = false;
                if (ctr is MyList list) list.ClearItems();
            }

            if (SideBar.SelectedIndex == -1) return;

            switch (ToolBar.SelectedIndex)
            {
                case 0: SwitchGeneralItem(); break;
                case 1: SwitchTypeItem(); break;
                case 2: SwitchOtherRuleItem(); break;
                case 4: SwitchAboutItem(); break;
            }

            lastItemIndex[ToolBar.SelectedIndex] = SideBar.SelectedIndex;
            SuspendMainBodyWhenMove = MainControls.Any(ctr => ctr.Controls.Count > 50);
        }

        private void ShowItemInfo()
        {
            if (SideBar.HoveredIndex >= 0)
            {
                int i = SideBar.HoveredIndex;
                switch (ToolBar.SelectedIndex)
                {
                    case 0: StatusBar.Text = GetGeneralItemInfo(i); return;
                    case 1: StatusBar.Text = GetTypeItemInfo(i); return;
                    case 2: StatusBar.Text = GetOtherRuleItemInfo(i); return;
                }
            }
            StatusBar.Text = MyStatusBar.DefaultText;
        }

        private string GetGeneralItemInfo(int index)
        {
            string[] infos = {
                AppString.StatusBar.File, AppString.StatusBar.Folder, AppString.StatusBar.Directory,
                AppString.StatusBar.Background, AppString.StatusBar.Desktop, AppString.StatusBar.Drive,
                AppString.StatusBar.AllObjects, AppString.StatusBar.Computer, AppString.StatusBar.RecycleBin,
                AppString.StatusBar.Library, null, AppString.StatusBar.New, AppString.StatusBar.SendTo,
                AppString.StatusBar.OpenWith, null, AppString.StatusBar.WinX
            };
            return index < infos.Length ? infos[index] : MyStatusBar.DefaultText;
        }

        private string GetTypeItemInfo(int index)
        {
            string[] infos = {
                AppString.StatusBar.LnkFile, AppString.StatusBar.UwpLnk, AppString.StatusBar.ExeFile,
                AppString.StatusBar.UnknownType, null, AppString.StatusBar.CustomExtension,
                AppString.StatusBar.PerceivedType, AppString.StatusBar.DirectoryType, null,
                AppString.StatusBar.MenuAnalysis
            };
            return index < infos.Length ? infos[index] : MyStatusBar.DefaultText;
        }

        private string GetOtherRuleItemInfo(int index)
        {
            string[] infos = {
                AppString.StatusBar.EnhanceMenu, AppString.StatusBar.DetailedEdit, null,
                AppString.StatusBar.DragDrop, AppString.StatusBar.PublicReferences, AppString.StatusBar.IEMenu,
                null, AppString.StatusBar.GuidBlocked, AppString.StatusBar.CustomRegPath
            };
            return index < infos.Length ? infos[index] : MyStatusBar.DefaultText;
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
            switch (SideBar.SelectedIndex)
            {
                case 11: ShowList(shellNewList); break;
                case 12: ShowList(sendToList); break;
                case 13: ShowList(openWithList); break;
                case 15: ShowList(winXList); break;
                default: ShowShellList(GeneralShellScenes[SideBar.SelectedIndex]); break;
            }
        }

        private void SwitchTypeItem()
        {
            shellList.Scene = (Scenes)TypeShellScenes[SideBar.SelectedIndex];
            shellList.LoadItems();
            shellList.Visible = true;
        }

        private void SwitchOtherRuleItem()
        {
            switch (SideBar.SelectedIndex)
            {
                case 0: ShowEnhanceMenus(); break;
                case 1: ShowDetailedEdit(); break;
                case 3: ShowShellList(Scenes.DragDrop); break;
                case 4: ShowShellList(Scenes.PublicReferences); break;
                case 5: ShowList(iEList); break;
                case 7: ShowList(guidBlockedList); break;
                case 8: ShowShellList(Scenes.CustomRegPath); break;
            }
        }

        private void SwitchAboutItem()
        {
            switch (SideBar.SelectedIndex)
            {
                case 0: appSettingBox.LoadItems(); appSettingBox.Visible = true; break;
                case 1: languagesBox.LoadLanguages(); languagesBox.Visible = true; break;
                case 2: backupListBox.LoadItems(); backupListBox.Visible = true; break;
                case 3: dictionariesBox.LoadText(); dictionariesBox.Visible = true; break;
                case 4: if (aboutMeBox.TextLength == 0) aboutMeBox.LoadIni(AppString.Other.AboutApp); aboutMeBox.Visible = true; break;
                case 5: donateBox.Visible = true; break;
            }
        }

        // 辅助方法
        private void ShowList(MyList list)
        {
            list.LoadItems();
            list.Visible = true;
        }

        private void ShowShellList(Scenes scene)
        {
            shellList.Scene = scene;
            shellList.LoadItems();
            shellList.Visible = true;
        }

        private void ShowEnhanceMenus()
        {
            enhanceMenusList.ScenePath = null;
            enhanceMenusList.LoadItems();
            enhanceMenusList.Visible = true;
        }

        private void ShowDetailedEdit()
        {
            detailedEditList.GroupGuid = Guid.Empty;
            detailedEditList.LoadItems();
            detailedEditList.Visible = true;
        }

        private void ResizeSideBar()
        {
            SideBar.Width = 0;
            string[] allItems = GeneralItems.Concat(TypeItems).Concat(OtherRuleItems).Concat(AboutItems).ToArray();
            foreach (string item in allItems)
            {
                if (item != null)
                    SideBar.Width = Math.Max(SideBar.Width, SideBar.GetItemWidth(item));
            }
        }

        private void AddContextMenus()
        {
            var dic = new Dictionary<MyToolBarButton, string[]>
            {
                { ToolBarButtons[0], GeneralItems },
                { ToolBarButtons[1], TypeItems },
                { ToolBarButtons[2], OtherRuleItems },
                { ToolBarButtons[4], new[] { AppString.Other.TopMost, null, AppString.Other.ShowFilePath, AppString.Other.HideDisabledItems, null, AppString.Other.OpenMoreRegedit, AppString.Other.OpenMoreExplorer } }
            };

            foreach (var item in dic)
            {
                CreateContextMenu(item.Key, item.Value);
            }
        }

        private void CreateContextMenu(MyToolBarButton button, string[] items)
        {
            ContextMenuStrip cms = new ContextMenuStrip();
            cms.MouseEnter += (sender, e) =>
            {
                if (button != ToolBar.SelectedButton) button.Opacity = MyToolBar.HoveredOpacity;
            };
            cms.Closed += (sender, e) =>
            {
                if (button != ToolBar.SelectedButton) button.Opacity = MyToolBar.UnSelctedOpacity;
            };
            button.MouseDown += (sender, e) =>
            {
                if (e.Button != MouseButtons.Right) return;
                if (sender == ToolBar.SelectedButton) return;
                cms.Show(button, e.Location);
            };

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                    cms.Items.Add(new RToolStripSeparator());
                else
                {
                    var tsi = new RToolStripMenuItem(items[i]);
                    cms.Items.Add(tsi);
                    // 简化的事件处理 - 需要根据实际情况完善
                }
            }
        }

        private void FirstRunDownloadLanguage()
        {
            if (AppConfig.IsFirstRun && CultureInfo.CurrentUICulture.Name != "zh-CN")
            {
                if (AppMessageBox.Show("It is detected that you may be running this program for the first time,\n" +
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
            if (explorerRestarter.Visible && AppMessageBox.Show(explorerRestarter.Text,
                MessageBoxButtons.OKCancel) == DialogResult.OK) ExternalProgram.RestartExplorer();
            Opacity = 0;
            WindowState = FormWindowState.Normal;
            explorerRestarter.Visible = false;
            AppConfig.MainFormSize = Size;
        }

        // 搜索功能
        private void InitializeSearch()
        {
            ToolBar.SearchPerformed += (s, e) => PerformSearch();
            ToolBar.SearchTextChanged += (s, e) =>
            {
                if (string.IsNullOrEmpty(ToolBar.SearchText))
                    ClearSearch();
                else
                    PerformSearch();
            };
        }

        private void PerformSearch()
        {
            string searchText = ToolBar.SearchText;
            if (string.IsNullOrWhiteSpace(searchText))
            {
                ClearSearch();
                return;
            }

            if (searchText == lastSearchText && isSearching) return;

            lastSearchText = searchText;
            isSearching = true;
            Cursor = Cursors.WaitCursor;

            try
            {
                SearchCurrentList(searchText);
                UpdateSearchStatus(searchText);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void SearchCurrentList(string searchText)
        {
            switch (ToolBar.SelectedIndex)
            {
                case 0: SearchGeneralList(searchText); break;
                case 1: SearchTypeList(searchText); break;
                case 2: SearchOtherRuleList(searchText); break;
                case 4: SearchAboutList(searchText); break;
            }
        }

        private void SearchGeneralList(string searchText)
        {
            switch (SideBar.SelectedIndex)
            {
                case 11: SafeSearchItems(shellNewList, searchText); break;
                case 12: SafeSearchItems(sendToList, searchText); break;
                case 13: SafeSearchItems(openWithList, searchText); break;
                case 15: SafeSearchItems(winXList, searchText); break;
                default: SafeSearchItems(shellList, searchText); break;
            }
        }

        private void SearchTypeList(string searchText)
        {
            SafeSearchItems(shellList, searchText);
        }

        private void SearchOtherRuleList(string searchText)
        {
            switch (SideBar.SelectedIndex)
            {
                case 0: SafeSearchItems(enhanceMenusList, searchText); break;
                case 1: SafeSearchItems(detailedEditList, searchText); break;
                case 3: case 4: case 8: SafeSearchItems(shellList, searchText); break;
                case 5: SafeSearchItems(iEList, searchText); break;
                case 7: SafeSearchItems(guidBlockedList, searchText); break;
            }
        }

        private void SearchAboutList(string searchText)
        {
            switch (SideBar.SelectedIndex)
            {
                case 2: SafeSearchItems(backupListBox, searchText); break;
                case 3: SafeSearchItems(dictionariesBox, searchText); break;
            }
        }

        private void SafeSearchItems(Control control, string searchText)
        {
            try
            {
                var method = control.GetType().GetMethod("SearchItems");
                if (method != null)
                {
                    method.Invoke(control, new object[] { searchText });
                }
            }
            catch
            {
                // 忽略搜索错误
            }
        }

        private void UpdateSearchStatus(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                StatusBar.Text = MyStatusBar.DefaultText;
            else
                StatusBar.Text = $"搜索 '{searchText}' - 找到结果";
        }

        private void ClearSearch()
        {
            if (!isSearching) return;

            foreach (Control ctr in MainControls)
                SafeSearchItems(ctr, string.Empty);

            isSearching = false;
            lastSearchText = string.Empty;
            ToolBar.ClearSearch();
            ShowItemInfo();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.F))
            {
                ToolBar.FocusSearchBox();
                return true;
            }

            if (keyData == Keys.Escape && isSearching)
            {
                ClearSearch();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}