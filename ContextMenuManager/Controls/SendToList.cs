using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System;
using System.IO;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    sealed class SendToList : MyList // 主页 发送到
    {
        public static readonly string SendToPath = Environment.ExpandEnvironmentVariables(@"%AppData%\Microsoft\Windows\SendTo");
        public static readonly string DefaultSendToPath = Environment.ExpandEnvironmentVariables(@"%SystemDrive%\Users\Default\AppData\Roaming\Microsoft\Windows\SendTo");

        public void LoadItems()
        {
            foreach(string path in Directory.GetFileSystemEntries(SendToPath))
            {
                if(Path.GetFileName(path).ToLower() == "desktop.ini") continue;
                AddItem(new SendToItem(path));
            }
            SortItemByText();
            AddNewItem();
            AddDirItem();   // 发送到右键菜单Ink文件的文件夹存放位置
            AddItem(new VisibleRegRuleItem(VisibleRegRuleItem.SendToDrive));
            AddItem(new VisibleRegRuleItem(VisibleRegRuleItem.DeferBuildSendTo));
        }

        private void AddNewItem()
        {
            NewItem newItem = new NewItem();
            InsertItem(newItem, 0);
            newItem.AddNewItem += () =>
            {
                using(NewLnkFileDialog dlg = new NewLnkFileDialog())
                {
                    dlg.FileFilter = $"{AppString.Dialog.Program}|*.exe;*.bat;*.cmd;*.vbs;*.vbe;*.js;*.jse;*.wsf";
                    if(dlg.ShowDialog() != DialogResult.OK) return;
                    string lnkPath = $@"{SendToPath}\{ObjectPath.RemoveIllegalChars(dlg.ItemText)}.lnk";
                    lnkPath = ObjectPath.GetNewPathWithIndex(lnkPath, ObjectPath.PathType.File);
                    using(ShellLink shellLink = new ShellLink(lnkPath))
                    {
                        shellLink.TargetPath = dlg.ItemFilePath;
                        shellLink.WorkingDirectory = Path.GetDirectoryName(dlg.ItemFilePath);
                        shellLink.Arguments = dlg.Arguments;
                        shellLink.Save();
                    }
                    DesktopIni.SetLocalizedFileNames(lnkPath, dlg.ItemText);
                    InsertItem(new SendToItem(lnkPath), 2);
                }
            };
        }

        private void AddDirItem()
        {
            MyListItem item = new MyListItem
            {
                Text = Path.GetFileNameWithoutExtension(SendToPath),
                Image = ResourceIcon.GetFolderIcon(SendToPath).ToBitmap()
            };
            PictureButton btnPath = new PictureButton(AppImage.Open);
            ToolTipBox.SetToolTip(btnPath, AppString.Menu.FileLocation);
            btnPath.MouseDown += (sender, e) => ExternalProgram.OpenDirectory(SendToPath);
            item.AddCtr(btnPath);
            InsertItem(item, 1);
            item.ContextMenuStrip = new ContextMenuStrip();
            var tsiRestoreDefault = new RToolStripMenuItem(AppString.Menu.RestoreDefault);
            item.ContextMenuStrip.Items.Add(tsiRestoreDefault);
            tsiRestoreDefault.Enabled = Directory.Exists(DefaultSendToPath);
            tsiRestoreDefault.Click += (sender, e) =>
            {
                if(AppMessageBox.Show(AppString.Message.RestoreDefault, MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    File.SetAttributes(SendToPath, FileAttributes.Normal);
                    Directory.Delete(SendToPath, true);
                    Directory.CreateDirectory(SendToPath);
                    File.SetAttributes(SendToPath, File.GetAttributes(DefaultSendToPath));
                    foreach(string srcPath in Directory.GetFiles(DefaultSendToPath))
                    {
                        string dstPath = $@"{SendToPath}\{Path.GetFileName(srcPath)}";
                        File.Copy(srcPath, dstPath);
                    }
                    ClearItems();
                    LoadItems();
                }
            };
        }
    }
}