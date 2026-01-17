using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.BluePointLilac.Controls;
using ContextMenuManager.Methods;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    internal sealed class AppSettingBox : MyList
    {
        public AppSettingBox()
        {
            SuspendLayout();
            Font = SystemFonts.MenuFont;
            Font = new Font(Font.FontFamily, Font.Size + 1F);
            mliConfigDir.AddCtrs(new Control[] { cmbConfigDir, btnConfigDir });
            mliBackup.AddCtrs(new Control[] { chkBackup, btnBackupDir });
            mliUpdate.AddCtrs(new Control[] { cmbUpdate, btnUpdate });
            mliRepo.AddCtr(cmbRepo);
            mliTopMost.AddCtr(chkTopMost);
            mliProtect.AddCtr(chkProtect);
            mliEngine.AddCtr(cmbEngine);
            mliShowFilePath.AddCtr(chkShowFilePath);
            mliOpenMoreRegedit.AddCtr(chkOpenMoreRegedit);
            mliOpenMoreExplorer.AddCtr(chkOpenMoreExplorer);
            mliHideDisabledItems.AddCtr(chkHideDisabledItems);
            mliHideSysStoreItems.AddCtr(chkHideSysStoreItems);

            ToolTipBox.SetToolTip(btnUpdate, AppString.Tip.ImmediatelyCheck);
            ToolTipBox.SetToolTip(cmbConfigDir, AppString.Tip.ConfigPath);
            ToolTipBox.SetToolTip(btnConfigDir, AppString.Menu.FileLocation);
            ToolTipBox.SetToolTip(btnBackupDir, AppString.Menu.FileLocation);

            cmbRepo.Items.AddRange(new[] { "Github", "Gitee" });
            cmbConfigDir.Items.AddRange(new[] { AppString.Other.AppDataDir, AppString.Other.AppDir });
            cmbEngine.Items.AddRange(AppConfig.EngineUrlsDic.Keys.ToArray());
            cmbEngine.Items.Add(AppString.Other.CustomEngine);
            cmbUpdate.Items.AddRange(new[] { AppString.Other.OnceAWeek, AppString.Other.OnceAMonth,
                AppString.Other.OnceASeason, AppString.Other.NeverCheck });

            cmbConfigDir.Width = cmbEngine.Width = cmbUpdate.Width = cmbRepo.Width = 120.DpiZoom();
            cmbConfigDir.DropDownStyle = cmbEngine.DropDownStyle = cmbUpdate.DropDownStyle
                = cmbRepo.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbConfigDir.AutosizeDropDownWidth();
            cmbEngine.AutosizeDropDownWidth();
            cmbUpdate.AutosizeDropDownWidth();
            cmbRepo.AutosizeDropDownWidth();

            btnUpdate.MouseDown += (sender, e) =>
            {
                Cursor = Cursors.WaitCursor;
                Updater.Update(true);
                Cursor = Cursors.Default;
            };
            btnConfigDir.MouseDown += (sender, e) => ExternalProgram.OpenDirectory(AppConfig.ConfigDir);
            btnBackupDir.MouseDown += (sender, e) => ExternalProgram.OpenDirectory(AppConfig.RegBackupDir);
            chkBackup.CheckChanged += () => AppConfig.AutoBackup = chkBackup.Checked;
            chkProtect.CheckChanged += () => AppConfig.ProtectOpenItem = chkProtect.Checked;
            chkOpenMoreRegedit.CheckChanged += () => AppConfig.OpenMoreRegedit = chkOpenMoreRegedit.Checked;
            chkTopMost.CheckChanged += () => AppConfig.TopMost = FindForm().TopMost = chkTopMost.Checked;
            chkOpenMoreExplorer.CheckChanged += () => AppConfig.OpenMoreExplorer = chkOpenMoreExplorer.Checked;
            chkHideDisabledItems.CheckChanged += () => AppConfig.HideDisabledItems = chkHideDisabledItems.Checked;
            chkHideSysStoreItems.CheckChanged += () => AppConfig.HideSysStoreItems = chkHideSysStoreItems.Checked;
            cmbRepo.SelectionChangeCommitted += (sender, e) => AppConfig.RequestUseGithub = cmbRepo.SelectedIndex == 0;
            chkShowFilePath.CheckChanged += () => AppConfig.ShowFilePath = chkShowFilePath.Checked;
            cmbUpdate.SelectionChangeCommitted += (sender, e) => ChangeUpdateFrequency();
            cmbConfigDir.SelectionChangeCommitted += (sender, e) => ChangeConfigDir();
            cmbEngine.SelectionChangeCommitted += (sender, e) => ChangeEngineUrl();
            ResumeLayout();
        }

        private readonly MyListItem mliConfigDir = new()
        {
            Text = AppString.Other.ConfigPath
        };
        private readonly RComboBox cmbConfigDir = new();
        private readonly PictureButton btnConfigDir = new(AppImage.Open);

        private readonly MyListItem mliRepo = new()
        {
            Text = AppString.Other.SetRequestRepo
        };
        private readonly RComboBox cmbRepo = new();

        private readonly MyListItem mliBackup = new()
        {
            Text = AppString.Other.AutoBackup
        };
        private readonly MyCheckBox chkBackup = new();
        private readonly PictureButton btnBackupDir = new(AppImage.Open);

        private readonly MyListItem mliUpdate = new()
        {
            Text = AppString.Other.SetUpdateFrequency
        };
        private readonly RComboBox cmbUpdate = new();
        private readonly PictureButton btnUpdate = new(AppImage.CheckUpdate);

        private readonly MyListItem mliTopMost = new()
        {
            Text = AppString.Other.TopMost
        };
        private readonly MyCheckBox chkTopMost = new();

        private readonly MyListItem mliProtect = new()
        {
            Text = AppString.Other.ProtectOpenItem
        };
        private readonly MyCheckBox chkProtect = new();

        private readonly MyListItem mliEngine = new()
        {
            Text = AppString.Other.WebSearchEngine
        };
        private readonly RComboBox cmbEngine = new();

        private readonly MyListItem mliShowFilePath = new()
        {
            Text = AppString.Other.ShowFilePath
        };
        private readonly MyCheckBox chkShowFilePath = new();

        private readonly MyListItem mliOpenMoreRegedit = new()
        {
            Text = AppString.Other.OpenMoreRegedit
        };
        private readonly MyCheckBox chkOpenMoreRegedit = new();

        private readonly MyListItem mliOpenMoreExplorer = new()
        {
            Text = AppString.Other.OpenMoreExplorer
        };
        private readonly MyCheckBox chkOpenMoreExplorer = new();

        private readonly MyListItem mliHideDisabledItems = new()
        {
            Text = AppString.Other.HideDisabledItems
        };
        private readonly MyCheckBox chkHideDisabledItems = new();

        private readonly MyListItem mliHideSysStoreItems = new()
        {
            Text = AppString.Other.HideSysStoreItems,
            Visible = WinOsVersion.Current >= WinOsVersion.Win7
        };
        private readonly MyCheckBox chkHideSysStoreItems = new();

        public override void ClearItems()
        {
            Controls.Clear();
        }

        public void LoadItems()
        {
            AddItems(new[] { mliConfigDir, mliUpdate, mliRepo, mliEngine, mliBackup, mliTopMost, mliProtect, mliShowFilePath,
                mliHideDisabledItems, mliHideSysStoreItems, mliOpenMoreRegedit, mliOpenMoreExplorer });
            foreach (MyListItem item in Controls) item.HasImage = false;
            cmbConfigDir.SelectedIndex = AppConfig.SaveToAppDir ? 1 : 0;
            cmbRepo.SelectedIndex = AppConfig.RequestUseGithub ? 0 : 1;
            cmbUpdate.SelectedIndex = GetUpdateSelectIndex();
            cmbEngine.SelectedIndex = GetEngineSelectIndex();
            chkBackup.Checked = AppConfig.AutoBackup;
            chkTopMost.Checked = FindForm().TopMost;
            chkProtect.Checked = AppConfig.ProtectOpenItem;
            chkShowFilePath.Checked = AppConfig.ShowFilePath;
            chkOpenMoreRegedit.Checked = AppConfig.OpenMoreRegedit;
            chkOpenMoreExplorer.Checked = AppConfig.OpenMoreExplorer;
            chkHideDisabledItems.Checked = AppConfig.HideDisabledItems;
            chkHideSysStoreItems.Checked = AppConfig.HideSysStoreItems;
        }

        private void ChangeConfigDir()
        {
            var newPath = (cmbConfigDir.SelectedIndex == 0) ? AppConfig.AppDataConfigDir : AppConfig.AppConfigDir;
            if (newPath == AppConfig.ConfigDir) return;
            if (AppMessageBox.Show(AppString.Message.RestartApp, MessageBoxButtons.OKCancel) != DialogResult.OK)
            {
                cmbConfigDir.SelectedIndex = AppConfig.SaveToAppDir ? 1 : 0;
            }
            else
            {
                DirectoryEx.CopyTo(AppConfig.ConfigDir, newPath);
                Directory.Delete(AppConfig.ConfigDir, true);
                SingleInstance.Restart();
            }
        }

        private void ChangeEngineUrl()
        {
            if (cmbEngine.SelectedIndex < cmbEngine.Items.Count - 1)
            {
                AppConfig.EngineUrl = AppConfig.EngineUrlsDic[cmbEngine.Text];
            }
            else
            {
                using var dlg = new InputDialog();
                dlg.Text = AppConfig.EngineUrl;
                dlg.Title = AppString.Other.SetCustomEngine;
                if (dlg.ShowDialog() == DialogResult.OK) AppConfig.EngineUrl = dlg.Text;
                cmbEngine.SelectedIndex = GetEngineSelectIndex();
            }
        }

        private void ChangeUpdateFrequency()
        {
            var day = 30;
            switch (cmbUpdate.SelectedIndex)
            {
                case 0:
                    day = 7; break;
                case 2:
                    day = 90; break;
                case 3:
                    day = -1; break;
            }
            AppConfig.UpdateFrequency = day;
        }

        private int GetUpdateSelectIndex()
        {
            var index = 1;
            switch (AppConfig.UpdateFrequency)
            {
                case 7:
                    index = 0; break;
                case 90:
                    index = 2; break;
                case -1:
                    index = 3; break;
            }
            return index;
        }

        private int GetEngineSelectIndex()
        {
            var urls = AppConfig.EngineUrlsDic.Values.ToArray();
            for (var i = 0; i < urls.Length; i++)
            {
                if (AppConfig.EngineUrl.Equals(urls[i])) return i;
            }
            return cmbEngine.Items.Count - 1;
        }
    }
}