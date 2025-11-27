using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    sealed class WinXList : MyList // 主页 Win+X
    {
        public static readonly string WinXPath = Environment.ExpandEnvironmentVariables(@"%LocalAppData%\Microsoft\Windows\WinX");
        public static readonly string BackupWinXPath = Environment.ExpandEnvironmentVariables(@"%LocalAppData%\Microsoft\Windows\-WinX");
        public static readonly string DefaultWinXPath = Environment.ExpandEnvironmentVariables(@"%SystemDrive%\Users\Default\AppData\Local\Microsoft\Windows\WinX");
        public static readonly string WinXDefaultPath = Environment.ExpandEnvironmentVariables(@"%LocalAppData%\Microsoft\Windows\WinXDefault");

        public void LoadItems()
        {
            if(WinOsVersion.Current >= WinOsVersion.Win8)
            {
                AppConfig.BackupWinX();
                AddItem(new WinXSortableItem(this));
                AddNewItem();
                LoadWinXItems();
            }
        }

        private void LoadWinXItems()
        {
            // 获取两处WinX目录下的所有文件夹路径
            string[] dirPaths1 = Directory.Exists(WinXPath) ? Directory.GetDirectories(WinXPath) : new string[] { };
            string[] dirPaths2 = Directory.Exists(BackupWinXPath) ? Directory.GetDirectories(BackupWinXPath) : new string[] { };
            // 两处WinX目录下的文件夹名称合并，去重，排序，反序
            List<string> dirKeyPaths = new List<string> { };
            foreach (string dirPath in dirPaths1)
            {
                string keyName = Path.GetFileNameWithoutExtension(dirPath);
                dirKeyPaths.Add(keyName);
            }
            foreach (string dirPath in dirPaths2)
            {
                string keyName = Path.GetFileNameWithoutExtension(dirPath);
                if (!dirKeyPaths.Contains(keyName)) dirKeyPaths.Add(keyName);
            }
            dirKeyPaths.Sort();
            dirKeyPaths.Reverse();

            // 检查WinX项目是否排序并初始化界面
            bool sorted = false;
            foreach(string dirKeyPath in dirKeyPaths)
            {
                string dirPath1 = $@"{WinXPath}\{dirKeyPath}";
                string dirPath2 = $@"{BackupWinXPath}\{dirKeyPath}";

                WinXGroupItem groupItem = new WinXGroupItem(dirPath1);
                AddItem(groupItem);

                List<string> lnkPaths;
                if(AppConfig.WinXSortable)
                {
                    lnkPaths = GetSortedPaths(dirKeyPath, out bool flag);
                    if(flag) sorted = true;
                }
                else
                {
                    lnkPaths = GetInkFiles(dirKeyPath);
                }

                foreach(string path in lnkPaths)
                {
                    WinXItem winXItem = new WinXItem(path, groupItem);
                    winXItem.BtnMoveDown.Visible = winXItem.BtnMoveUp.Visible = AppConfig.WinXSortable;
                    AddItem(winXItem);
                    groupItem.AddWinXItem(winXItem);
                }
            }
            if(sorted)
            {
                ExplorerRestarter.Show();
                AppMessageBox.Show(AppString.Message.WinXSorted);
            }
        }

        public static List<string> GetInkFiles(string dirKeyPath)
        {
            if (WinOsVersion.Current >= WinOsVersion.Win11)
            {
                List<string> lnkPaths = new List<string> { };

                // 获取两处WinX目录下的所有lnk文件路径
                string dirPath1 = $@"{WinXPath}\{dirKeyPath}";
                string dirPath2 = $@"{BackupWinXPath}\{dirKeyPath}";
                string[] lnkPaths1 = Directory.Exists(dirPath1) ? Directory.GetFiles(dirPath1, "*.lnk") : new string[] { };
                string[] lnkPaths2 = Directory.Exists(dirPath2) ? Directory.GetFiles(dirPath2, "*.lnk") : new string[] { };

                // 两处WinX目录下的lnk文件路径合并，排序，反序
                List<string> editedlnkPaths = new List<string> { };
                foreach (string filePath in lnkPaths1)
                {
                    editedlnkPaths.Add(filePath);
                }
                foreach (string filePath in lnkPaths2)
                {
                    string editFilePath = filePath.Replace(BackupWinXPath, WinXPath);
                    if (editedlnkPaths.Contains(editFilePath)) continue;
                    editFilePath += "-";
                    editedlnkPaths.Add(editFilePath);
                }
                editedlnkPaths.Sort();
                editedlnkPaths.Reverse();

                // 获取之前的路径元素
                foreach (string lnkKeyPath in editedlnkPaths)
                {
                    lnkPaths.Add(lnkKeyPath.EndsWith("-") ? lnkKeyPath.Remove(lnkKeyPath.Length - 1).Replace(WinXPath, BackupWinXPath) : lnkKeyPath);
                }
                return lnkPaths;
            }
            else
            {
                string dirPath = $@"{WinXPath}\{dirKeyPath}";
                string[] lnkPaths = Directory.GetFiles(dirPath, "*.lnk");
                Array.Reverse(lnkPaths);
                return lnkPaths.ToList();
            }
        }

        public static List<string> GetSortedPaths(string dirKeyPath, out bool resorted)
        {
            void ResortPaths(int index, string name, string path, string lnkFilePath, bool isWinX, out string dstPath)
            {
                bool itemVisible = lnkFilePath.Substring(0, WinXPath.Length).Equals(WinXPath, StringComparison.OrdinalIgnoreCase);
                if (!isWinX && !itemVisible)    // Default处菜单且禁用无需重新编号
                {
                    dstPath = null;
                    return;
                }

                string startPath = itemVisible ? WinXPath : BackupWinXPath;

                string meFilePath = isWinX ? lnkFilePath : lnkFilePath.Replace(startPath, DefaultWinXPath);
                dstPath = $@"{(isWinX ? startPath : DefaultWinXPath)}\{path}\{(index + 1).ToString().PadLeft(2, '0')} - {name}";
                dstPath = ObjectPath.GetNewPathWithIndex(dstPath, ObjectPath.PathType.File);

                string value;
                using (ShellLink srcLnk = new ShellLink(meFilePath))
                {
                    value = srcLnk.Description?.Trim();
                }
                if (string.IsNullOrEmpty(value)) value = DesktopIni.GetLocalizedFileNames(meFilePath);
                if (string.IsNullOrEmpty(value)) value = Path.GetFileNameWithoutExtension(name);
                DesktopIni.DeleteLocalizedFileNames(meFilePath);
                DesktopIni.SetLocalizedFileNames(dstPath, value);
                File.Move(meFilePath, dstPath);
                using (ShellLink dstLnk = new ShellLink(dstPath))
                {
                    dstLnk.Description = value;
                    dstLnk.Save();
                }
            }

            resorted = false;
            List<string> sortedPaths = new List<string>();
            List<string> lnkFilePaths = GetInkFiles(dirKeyPath);

            int i = lnkFilePaths.Count - 1;
            foreach (string lnkFilePath in lnkFilePaths)
            {
                string name = Path.GetFileName(lnkFilePath);
                int index = name.IndexOf(" - ");

                // 序号正确且为两位以上数字无需进行重新编号
                if (index >= 2 && int.TryParse(name.Substring(0, index), out int num) && num == i + 1)
                {
                    sortedPaths.Add(lnkFilePath); i--;  continue;
                }

                // 序号不正确或数字位数不足则进行重新编号
                if (index >= 0) name = name.Substring(index + 3);
                ResortPaths(i, name, dirKeyPath, lnkFilePath, true, out string dstPath);
                if (WinOsVersion.Current >= WinOsVersion.Win11)
                {
                    ResortPaths(i, name, dirKeyPath, lnkFilePath, false, out _);
                }

                sortedPaths.Add(dstPath);
                resorted = true;
                i--;
            }
            
            return sortedPaths;
        }

        private void AddNewItem()
        {
            NewItem newItem = new NewItem();
            AddItem(newItem);
            PictureButton btnCreateDir = new PictureButton(AppImage.NewFolder);
            ToolTipBox.SetToolTip(btnCreateDir, AppString.Tip.CreateGroup);
            newItem.AddCtr(btnCreateDir);
            btnCreateDir.MouseDown += (sender, e) => CreateNewGroup();
            newItem.AddNewItem += () =>
            {
                using(NewLnkFileDialog dlg1 = new NewLnkFileDialog())
                {
                    void AddNewLnkFile(string dirName, string itemText, string targetPath, string arguments, bool isWinX)
                    {
                        string dirPath = $@"{(isWinX ? WinXPath : DefaultWinXPath)}\{dirName}";
                        string workDir = Path.GetDirectoryName(targetPath);
                        string extension = Path.GetExtension(targetPath).ToLower();
                        string fileName = Path.GetFileNameWithoutExtension(targetPath);
                        int count = Directory.GetFiles(dirPath, "*.lnk").Length;
                        string index = (count + 1).ToString().PadLeft(2, '0');
                        string lnkName = $"{index} - {fileName}.lnk";
                        string lnkPath = $@"{dirPath}\{lnkName}";

                        using (ShellLink shellLink = new ShellLink(lnkPath))
                        {
                            if (extension == ".lnk")
                            {
                                File.Copy(targetPath, lnkPath);
                                shellLink.Load();
                            }
                            else
                            {
                                shellLink.TargetPath = targetPath;
                                shellLink.Arguments = arguments;
                                shellLink.WorkingDirectory = workDir;
                            }
                            shellLink.Description = itemText;
                            shellLink.Save();
                        }
                        DesktopIni.SetLocalizedFileNames(lnkPath, itemText);
                        foreach (MyListItem ctr in Controls)
                        {
                            if (ctr is WinXGroupItem groupItem && groupItem.Text == dirName)
                            {
                                WinXItem item = new WinXItem(lnkPath, groupItem) { Visible = !groupItem.IsFold };
                                item.BtnMoveDown.Visible = item.BtnMoveUp.Visible = AppConfig.WinXSortable;
                                InsertItem(item, GetItemIndex(groupItem) + 1);
                                groupItem.AddWinXItem(item);
                                break;
                            }
                        }
                        WinXHasher.HashLnk(lnkPath);
                    }

                    if (dlg1.ShowDialog() != DialogResult.OK) return;
                    using(SelectDialog dlg2 = new SelectDialog())
                    {
                        dlg2.Title = AppString.Dialog.SelectGroup;
                        dlg2.Items = GetGroupNames();
                        if(dlg2.ShowDialog() != DialogResult.OK) return;

                        AddNewLnkFile(dlg2.Selected, dlg1.ItemText, dlg1.ItemFilePath, dlg1.Arguments, true);
                        if (WinOsVersion.Current >= WinOsVersion.Win11)
                        {
                            AddNewLnkFile(dlg2.Selected, dlg1.ItemText, dlg1.ItemFilePath, dlg1.Arguments, false);
                        }
                        
                        ExplorerRestarter.Show();
                    }
                }
            };
        }

        private void CreateNewGroup()
        {
            void CreateGroupPath(string path)
            {
                // 创建目录文件夹
                Directory.CreateDirectory(path);
                File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly);

                // 初始化desktop.ini文件
                string iniPath = $@"{path}\desktop.ini";
                File.WriteAllText(iniPath, string.Empty, Encoding.Unicode);
                File.SetAttributes(iniPath, File.GetAttributes(iniPath) | FileAttributes.Hidden | FileAttributes.System);
            }

            string dirPath = ObjectPath.GetNewPathWithIndex($@"{WinXPath}\Group", ObjectPath.PathType.Directory, 1);
            CreateGroupPath(dirPath);
            if (WinOsVersion.Current >= WinOsVersion.Win11)
            {
                string defaultDirPath = dirPath.Replace(WinXPath, DefaultWinXPath);
                CreateGroupPath(defaultDirPath);
            }
            InsertItem(new WinXGroupItem(dirPath), 1);
        }

        public static string[] GetGroupNames()
        {
            List<string> items = new List<string>();
            DirectoryInfo winxDi = new DirectoryInfo(WinXPath);
            foreach(DirectoryInfo di in winxDi.GetDirectories()) items.Add(di.Name);
            items.Reverse();
            return items.ToArray();
        }

        sealed class WinXSortableItem : MyListItem
        {
            readonly MyCheckBox chkWinXSortable = new MyCheckBox();

            public WinXSortableItem(WinXList list)
            {
                Text = AppString.Other.WinXSortable;
                Image = AppImage.Sort;
                AddCtr(chkWinXSortable);
                chkWinXSortable.Checked = AppConfig.WinXSortable;
                chkWinXSortable.CheckChanged += () => { AppConfig.WinXSortable = chkWinXSortable.Checked; list.ClearItems(); list.LoadItems();
                };
            }
        }
    }
}