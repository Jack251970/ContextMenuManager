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
    internal sealed class WinXList : MyList // 主页 Win+X
    {
        public static readonly string WinXPath = Environment.ExpandEnvironmentVariables(@"%LocalAppData%\Microsoft\Windows\WinX");
        public static readonly string BackupWinXPath = Environment.ExpandEnvironmentVariables(@"%LocalAppData%\Microsoft\Windows\-WinX");
        public static readonly string DefaultWinXPath = Environment.ExpandEnvironmentVariables(@"%SystemDrive%\Users\Default\AppData\Local\Microsoft\Windows\WinX");
        public static readonly string WinXDefaultPath = Environment.ExpandEnvironmentVariables(@"%LocalAppData%\Microsoft\Windows\WinXDefault");

        public void LoadItems()
        {
            if (WinOsVersion.Current >= WinOsVersion.Win8)
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
            var dirPaths1 = Directory.Exists(WinXPath) ? Directory.GetDirectories(WinXPath) : new string[] { };
            var dirPaths2 = Directory.Exists(BackupWinXPath) ? Directory.GetDirectories(BackupWinXPath) : new string[] { };
            // 两处WinX目录下的文件夹名称合并，去重，排序，反序
            var dirKeyPaths = new List<string> { };
            foreach (var dirPath in dirPaths1)
            {
                var keyName = Path.GetFileNameWithoutExtension(dirPath);
                dirKeyPaths.Add(keyName);
            }
            foreach (var dirPath in dirPaths2)
            {
                var keyName = Path.GetFileNameWithoutExtension(dirPath);
                if (!dirKeyPaths.Contains(keyName)) dirKeyPaths.Add(keyName);
            }
            dirKeyPaths.Sort();
            dirKeyPaths.Reverse();

            // 检查WinX项目是否排序并初始化界面
            var sorted = false;
            foreach (var dirKeyPath in dirKeyPaths)
            {
                var dirPath1 = $@"{WinXPath}\{dirKeyPath}";
                var dirPath2 = $@"{BackupWinXPath}\{dirKeyPath}";

                var groupItem = new WinXGroupItem(dirPath1);
                AddItem(groupItem);

                List<string> lnkPaths;
                if (AppConfig.WinXSortable)
                {
                    lnkPaths = GetSortedPaths(dirKeyPath, out var flag);
                    if (flag) sorted = true;
                }
                else
                {
                    lnkPaths = GetInkFiles(dirKeyPath);
                }

                foreach (var path in lnkPaths)
                {
                    var winXItem = new WinXItem(path, groupItem);
                    winXItem.BtnMoveDown.Visible = winXItem.BtnMoveUp.Visible = AppConfig.WinXSortable;
                    AddItem(winXItem);
                    groupItem.AddWinXItem(winXItem);
                }
            }
            if (sorted)
            {
                ExplorerRestarter.Show();
                AppMessageBox.Show(AppString.Message.WinXSorted);
            }
        }

        public static List<string> GetInkFiles(string dirKeyPath)
        {
            if (WinOsVersion.Current >= WinOsVersion.Win11)
            {
                var lnkPaths = new List<string> { };

                // 获取两处WinX目录下的所有lnk文件路径
                var dirPath1 = $@"{WinXPath}\{dirKeyPath}";
                var dirPath2 = $@"{BackupWinXPath}\{dirKeyPath}";
                var lnkPaths1 = Directory.Exists(dirPath1) ? Directory.GetFiles(dirPath1, "*.lnk") : new string[] { };
                var lnkPaths2 = Directory.Exists(dirPath2) ? Directory.GetFiles(dirPath2, "*.lnk") : new string[] { };

                // 两处WinX目录下的lnk文件路径合并，排序，反序
                var editedlnkPaths = new List<string> { };
                foreach (var filePath in lnkPaths1)
                {
                    editedlnkPaths.Add(filePath);
                }
                foreach (var filePath in lnkPaths2)
                {
                    var editFilePath = filePath.Replace(BackupWinXPath, WinXPath);
                    if (editedlnkPaths.Contains(editFilePath)) continue;
                    editFilePath += "-";
                    editedlnkPaths.Add(editFilePath);
                }
                editedlnkPaths.Sort();
                editedlnkPaths.Reverse();

                // 获取之前的路径元素
                foreach (var lnkKeyPath in editedlnkPaths)
                {
                    lnkPaths.Add(lnkKeyPath.EndsWith("-") ? lnkKeyPath[..^1].Replace(WinXPath, BackupWinXPath) : lnkKeyPath);
                }
                return lnkPaths;
            }
            else
            {
                var dirPath = $@"{WinXPath}\{dirKeyPath}";
                var lnkPaths = Directory.GetFiles(dirPath, "*.lnk");
                Array.Reverse(lnkPaths);
                return lnkPaths.ToList();
            }
        }

        public static List<string> GetSortedPaths(string dirKeyPath, out bool resorted)
        {
            static void ResortPaths(int index, string name, string path, string lnkFilePath, bool isWinX, out string dstPath)
            {
                var itemVisible = lnkFilePath[..WinXPath.Length].Equals(WinXPath, StringComparison.OrdinalIgnoreCase);
                if (!isWinX && !itemVisible)    // Default处菜单且禁用无需重新编号
                {
                    dstPath = null;
                    return;
                }

                var startPath = itemVisible ? WinXPath : BackupWinXPath;

                var meFilePath = isWinX ? lnkFilePath : lnkFilePath.Replace(startPath, DefaultWinXPath);
                dstPath = $@"{(isWinX ? startPath : DefaultWinXPath)}\{path}\{(index + 1).ToString().PadLeft(2, '0')} - {name}";
                dstPath = ObjectPath.GetNewPathWithIndex(dstPath, ObjectPath.PathType.File);

                string value;
                using (var srcLnk = new ShellLink(meFilePath))
                {
                    value = srcLnk.Description?.Trim();
                }
                if (string.IsNullOrEmpty(value)) value = DesktopIni.GetLocalizedFileNames(meFilePath);
                if (string.IsNullOrEmpty(value)) value = Path.GetFileNameWithoutExtension(name);
                DesktopIni.DeleteLocalizedFileNames(meFilePath);
                DesktopIni.SetLocalizedFileNames(dstPath, value);
                File.Move(meFilePath, dstPath);
                using var dstLnk = new ShellLink(dstPath);
                dstLnk.Description = value;
                dstLnk.Save();
            }

            resorted = false;
            var sortedPaths = new List<string>();
            var lnkFilePaths = GetInkFiles(dirKeyPath);

            var i = lnkFilePaths.Count - 1;
            foreach (var lnkFilePath in lnkFilePaths)
            {
                var name = Path.GetFileName(lnkFilePath);
                var index = name.IndexOf(" - ");

                // 序号正确且为两位以上数字无需进行重新编号
                if (index >= 2 && int.TryParse(name[..index], out var num) && num == i + 1)
                {
                    sortedPaths.Add(lnkFilePath); i--; continue;
                }

                // 序号不正确或数字位数不足则进行重新编号
                if (index >= 0) name = name[(index + 3)..];
                ResortPaths(i, name, dirKeyPath, lnkFilePath, true, out var dstPath);
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
            var newItem = new NewItem();
            AddItem(newItem);
            var btnCreateDir = new PictureButton(AppImage.NewFolder);
            ToolTipBox.SetToolTip(btnCreateDir, AppString.Tip.CreateGroup);
            newItem.AddCtr(btnCreateDir);
            btnCreateDir.MouseDown += (sender, e) => CreateNewGroup();
            newItem.AddNewItem += () =>
            {
                using var dlg1 = new NewLnkFileDialog();
                void AddNewLnkFile(string dirName, string itemText, string targetPath, string arguments, bool isWinX)
                {
                    var dirPath = $@"{(isWinX ? WinXPath : DefaultWinXPath)}\{dirName}";
                    var workDir = Path.GetDirectoryName(targetPath);
                    var extension = Path.GetExtension(targetPath).ToLower();
                    var fileName = Path.GetFileNameWithoutExtension(targetPath);
                    var count = Directory.GetFiles(dirPath, "*.lnk").Length;
                    var index = (count + 1).ToString().PadLeft(2, '0');
                    var lnkName = $"{index} - {fileName}.lnk";
                    var lnkPath = $@"{dirPath}\{lnkName}";

                    using (var shellLink = new ShellLink(lnkPath))
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
                            var item = new WinXItem(lnkPath, groupItem) { Visible = !groupItem.IsFold };
                            item.BtnMoveDown.Visible = item.BtnMoveUp.Visible = AppConfig.WinXSortable;
                            InsertItem(item, GetItemIndex(groupItem) + 1);
                            groupItem.AddWinXItem(item);
                            break;
                        }
                    }
                    WinXHasher.HashLnk(lnkPath);
                }

                if (dlg1.ShowDialog() != DialogResult.OK) return;
                using var dlg2 = new SelectDialog();
                dlg2.Title = AppString.Dialog.SelectGroup;
                dlg2.Items = GetGroupNames();
                if (dlg2.ShowDialog() != DialogResult.OK) return;

                AddNewLnkFile(dlg2.Selected, dlg1.ItemText, dlg1.ItemFilePath, dlg1.Arguments, true);
                if (WinOsVersion.Current >= WinOsVersion.Win11)
                {
                    AddNewLnkFile(dlg2.Selected, dlg1.ItemText, dlg1.ItemFilePath, dlg1.Arguments, false);
                }

                ExplorerRestarter.Show();
            };
        }

        private void CreateNewGroup()
        {
            static void CreateGroupPath(string path)
            {
                // 创建目录文件夹
                Directory.CreateDirectory(path);
                File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly);

                // 初始化desktop.ini文件
                var iniPath = $@"{path}\desktop.ini";
                File.WriteAllText(iniPath, string.Empty, Encoding.Unicode);
                File.SetAttributes(iniPath, File.GetAttributes(iniPath) | FileAttributes.Hidden | FileAttributes.System);
            }

            var dirPath = ObjectPath.GetNewPathWithIndex($@"{WinXPath}\Group", ObjectPath.PathType.Directory, 1);
            CreateGroupPath(dirPath);
            if (WinOsVersion.Current >= WinOsVersion.Win11)
            {
                var defaultDirPath = dirPath.Replace(WinXPath, DefaultWinXPath);
                CreateGroupPath(defaultDirPath);
            }
            InsertItem(new WinXGroupItem(dirPath), 1);
        }

        public static string[] GetGroupNames()
        {
            var items = new List<string>();
            var winxDi = new DirectoryInfo(WinXPath);
            foreach (var di in winxDi.GetDirectories()) items.Add(di.Name);
            items.Reverse();
            return items.ToArray();
        }

        private sealed class WinXSortableItem : MyListItem
        {
            private readonly MyCheckBox chkWinXSortable = new();

            public WinXSortableItem(WinXList list)
            {
                Text = AppString.Other.WinXSortable;
                Image = AppImage.Sort;
                AddCtr(chkWinXSortable);
                chkWinXSortable.Checked = AppConfig.WinXSortable;
                chkWinXSortable.CheckChanged += () =>
                {
                    AppConfig.WinXSortable = chkWinXSortable.Checked; list.ClearItems(); list.LoadItems();
                };
            }
        }
    }
}