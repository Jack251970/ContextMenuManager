using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    internal sealed class DictionariesBox : TabControl
    {
        public DictionariesBox()
        {
            SuspendLayout();
            Dock = DockStyle.Fill;
            Controls.AddRange(pages);
            ForeColor = DarkModeHelper.FormFore; // 修改这里
            BackColor = DarkModeHelper.FormBack; // 修改这里
            Font = SystemFonts.MenuFont;
            Font = new Font(Font.FontFamily, Font.Size + 1F);
            cms.Items.AddRange(items);
            for (var i = 0; i < 6; i++)
            {
                boxs[i] = new ReadOnlyRichTextBox { Parent = pages[i] };
                if (i > 0) boxs[i].ContextMenuStrip = cms;
            }
            items[0].Click += (sender, e) => ExternalProgram.OpenNotepadWithText(GetInitialText());
            items[2].Click += (sender, e) => SaveFile();
            boxs[0].Controls.Add(btnOpenDir);
            btnOpenDir.Top = boxs[0].Height - btnOpenDir.Height;
            ToolTipBox.SetToolTip(btnOpenDir, AppString.Menu.FileLocation);
            btnOpenDir.MouseDown += (sender, e) => ExternalProgram.OpenDirectory(AppConfig.DicsDir);
            SelectedIndexChanged += (sender, e) => LoadText();
            VisibleChanged += (sender, e) => this.SetEnabled(Visible);
            ResumeLayout();
        }

        private readonly TabPage[] pages =
        {
            new(AppString.Other.DictionaryDescription),
            new(AppString.SideBar.AppLanguage),
            new(AppString.Other.GuidInfosDictionary),
            new(AppString.SideBar.DetailedEdit),
            new(AppString.SideBar.EnhanceMenu),
            new(AppString.Other.UwpMode)
        };
        private readonly ReadOnlyRichTextBox[] boxs = new ReadOnlyRichTextBox[6];
        private readonly PictureButton btnOpenDir = new(AppImage.Open)
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
            Left = 0
        };
        private readonly ContextMenuStrip cms = new();
        private readonly ToolStripItem[] items =
        {
            new RToolStripMenuItem(AppString.Menu.Edit),
            new RToolStripSeparator(),
            new RToolStripMenuItem(AppString.Menu.Save)
        };

        private void SaveFile()
        {
            using var dlg = new SaveFileDialog();
            var dirPath = AppConfig.UserDicsDir;
            switch (SelectedIndex)
            {
                case 1:
                    dirPath = AppConfig.LangsDir;
                    dlg.FileName = AppConfig.ZH_CNINI;
                    break;
                case 2:
                    dlg.FileName = AppConfig.GUIDINFOSDICINI;
                    break;
                case 3:
                    dlg.FileName = AppConfig.DETAILEDEDITDICXML;
                    break;
                case 4:
                    dlg.FileName = AppConfig.ENHANCEMENUSICXML;
                    break;
                case 5:
                    dlg.FileName = AppConfig.UWPMODEITEMSDICXML;
                    break;
            }
            dlg.Filter = $"{dlg.FileName}|*{Path.GetExtension(dlg.FileName)}";
            Directory.CreateDirectory(dirPath);
            dlg.InitialDirectory = dirPath;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(dlg.FileName, GetInitialText(), Encoding.Unicode);
            }
        }

        private string GetInitialText()
        {
            return SelectedIndex switch
            {
                0 => AppString.Other.Dictionaries,
                1 => Properties.Resources.AppLanguageDic,
                2 => Properties.Resources.GuidInfosDic,
                3 => Properties.Resources.DetailedEditDic,
                4 => Properties.Resources.EnhanceMenusDic,
                5 => Properties.Resources.UwpModeItemsDic,
                _ => string.Empty,
            };
        }

        public void LoadText()
        {
            var index = SelectedIndex;
            if (boxs[index].Text.Length > 0) return;
            Action<string> action = null;
            switch (index)
            {
                case 0:
                case 1:
                case 2:
                    action = boxs[index].LoadIni; break;
                case 3:
                case 4:
                case 5:
                    action = boxs[index].LoadXml; break;
            }
            BeginInvoke(action, new[] { GetInitialText() });
        }
    }
}