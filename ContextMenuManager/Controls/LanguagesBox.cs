using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.BluePointLilac.Controls;
using ContextMenuManager.Methods;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    internal sealed class LanguagesBox : FlowLayoutPanel
    {
        public LanguagesBox()
        {
            SuspendLayout();
            Dock = DockStyle.Fill;
            Font = SystemFonts.MenuFont;
            Font = new Font(Font.FontFamily, Font.Size + 1F);
            Controls.AddRange(new Control[] { cmbLanguages, btnOpenDir, btnDownLoad, btnTranslate, lblThank, pnlTranslators });
            VisibleChanged += (sender, e) => this.SetEnabled(Visible);
            cmbLanguages.SelectionChangeCommitted += (sender, e) => ChangeLanguage();
            btnOpenDir.MouseDown += (sender, e) => ExternalProgram.OpenDirectory(AppConfig.LangsDir);
            lblThank.MouseEnter += (sender, e) => lblThank.ForeColor = DarkModeHelper.MainColor; // 修改这里
            lblThank.MouseLeave += (sender, e) => lblThank.ForeColor = Color.DimGray;//Fixed
            btnDownLoad.MouseDown += (sender, e) =>
            {
                Cursor = Cursors.WaitCursor;
                ShowLanguageDialog();
                Cursor = Cursors.Default;
            };
            btnTranslate.MouseDown += (sender, e) =>
            {
                using var dlg = new TranslateDialog();
                dlg.ShowDialog();
            };
            ToolTipBox.SetToolTip(btnOpenDir, AppString.Menu.FileLocation);
            ToolTipBox.SetToolTip(btnDownLoad, AppString.Dialog.DownloadLanguages);
            ToolTipBox.SetToolTip(btnTranslate, AppString.Dialog.TranslateTool);
            lblHeader.Font = new Font(Font, FontStyle.Bold);
            cmbLanguages.AutosizeDropDownWidth();
            OnResize(null);
            ResumeLayout();
        }

        private readonly RComboBox cmbLanguages = new()
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 170.DpiZoom(),
        };
        private readonly PictureButton btnOpenDir = new(AppImage.Open);
        private readonly PictureButton btnDownLoad = new(AppImage.DownLoad);
        private readonly PictureButton btnTranslate = new(AppImage.Translate);
        private readonly ToolTip toolTip = new()
        { InitialDelay = 1 };
        private readonly Panel pnlTranslators = new()
        {
            BorderStyle = BorderStyle.FixedSingle,
            AutoScroll = true
        };
        private readonly Label lblHeader = new()
        {
            Text = AppString.Other.Translators + "\r\n" + new string('-', 96),
            ForeColor = DarkModeHelper.MainColor, // 修改这里
            Dock = DockStyle.Top,
            AutoSize = true
        };
        private readonly Label lblThank = new()
        {
            Font = new Font("Lucida Handwriting", 11F),
            Text = "Thank you for your translation!",
            ForeColor = Color.DimGray,//Fixed
            AutoSize = true,
        };
        private readonly List<string> languages = new();

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            var a = 20.DpiZoom();
            pnlTranslators.Width = ClientSize.Width - 2 * a;
            pnlTranslators.Height = ClientSize.Height - pnlTranslators.Top - a;
            cmbLanguages.Margin = pnlTranslators.Margin = lblThank.Margin = btnOpenDir.Margin
                = btnDownLoad.Margin = btnTranslate.Margin = new Padding(a, a, 0, 0);
        }

        public void LoadLanguages()
        {
            cmbLanguages.Items.Clear();
            cmbLanguages.Items.Add("(default) 简体中文");
            languages.Clear();
            languages.Add("default");
            pnlTranslators.SuspendLayout();
            pnlTranslators.Controls.Remove(lblHeader);
            foreach (Control ctr in pnlTranslators.Controls) ctr.Dispose();
            pnlTranslators.Controls.Clear();
            pnlTranslators.Controls.Add(lblHeader);
            if (Directory.Exists(AppConfig.LangsDir))
            {
                var dic = new Dictionary<Label, Control[]>();
                foreach (var fileName in Directory.GetFiles(AppConfig.LangsDir, "*.ini"))
                {
                    var writer = new IniWriter(fileName);
                    var language = writer.GetValue("General", "Language");
                    var translator = writer.GetValue("General", "Translator");
                    var translatorUrl = writer.GetValue("General", "TranslatorUrl");

                    var langName = Path.GetFileNameWithoutExtension(fileName);
                    if (string.IsNullOrEmpty(language)) language = langName;
                    var translators = translator.Split(new[] { "\\r\\n", "\\n" }, StringSplitOptions.RemoveEmptyEntries);
                    var urls = translatorUrl.Split(new[] { "\\r\\n", "\\n" }, StringSplitOptions.RemoveEmptyEntries);

                    var lblLanguage = new Label
                    {
                        ForeColor = DarkModeHelper.FormFore, // 修改这里
                        Text = language,
                        AutoSize = true,
                        Font = Font
                    };
                    var ctrTranslators = new Label[translators.Length];
                    for (var i = 0; i < translators.Length; i++)
                    {
                        ctrTranslators[i] = new Label
                        {
                            AutoSize = true,
                            Font = Font,
                            Text = translators[i],
                            ForeColor = Color.DimGray,//Fixed
                        };
                        if (urls.Length > i)
                        {
                            var url = urls[i].Trim();
                            if (url != "null")
                            {
                                toolTip.SetToolTip(ctrTranslators[i], url);
                                ctrTranslators[i].ForeColor = DarkModeHelper.FormFore; // 修改这里
                                ctrTranslators[i].Font = new Font(ctrTranslators[i].Font, FontStyle.Underline);
                                ctrTranslators[i].Click += (sender, e) => ExternalProgram.OpenWebUrl(url);
                            }
                        }
                    }
                    dic.Add(lblLanguage, ctrTranslators);
                    cmbLanguages.Items.Add(language);
                    languages.Add(langName);
                }
                var left = 0;
                dic.Keys.ToList().ForEach(lbl => left = Math.Max(left, lbl.Width));
                left += 250.DpiZoom();
                var top = lblHeader.Bottom + 10.DpiZoom();
                foreach (var item in dic)
                {
                    item.Key.Top = top;
                    pnlTranslators.Controls.Add(item.Key);
                    foreach (var ctr in item.Value)
                    {
                        ctr.Location = new Point(left, top);
                        pnlTranslators.Controls.Add(ctr);
                        top += ctr.Height + 10.DpiZoom();
                    }
                }
            }
            pnlTranslators.ResumeLayout();
            cmbLanguages.SelectedIndex = GetSelectIndex();
        }

        private void ChangeLanguage()
        {
            var index = GetSelectIndex();
            if (cmbLanguages.SelectedIndex == index) return;
            var language = languages[cmbLanguages.SelectedIndex];
            var msg = "";
            if (cmbLanguages.SelectedIndex != 0)
            {
                var langPath = $@"{AppConfig.LangsDir}\{language}.ini";
                msg = new IniWriter(langPath).GetValue("Message", "RestartApp");
            }
            if (msg == "") msg = AppString.Message.RestartApp;
            if (AppMessageBox.Show(msg, MessageBoxButtons.OKCancel) != DialogResult.OK)
            {
                cmbLanguages.SelectedIndex = index;
            }
            else
            {
                if (language == CultureInfo.CurrentUICulture.Name) language = "";
                AppConfig.Language = language;
                SingleInstance.Restart();
            }
        }

        private int GetSelectIndex()
        {
            var index = languages.FindIndex(language => language.Equals(AppConfig.Language, StringComparison.OrdinalIgnoreCase));
            if (index == -1) index = 0;
            return index;
        }

        public async void ShowLanguageDialog()
        {
            using var client = new UAWebClient();
            var apiUrl = AppConfig.RequestUseGithub ? AppConfig.GithubLangsApi : AppConfig.GiteeLangsApi;
            var doc = await client.GetWebJsonToXmlAsync(apiUrl);
            if (doc == null)
            {
                AppMessageBox.Show(AppString.Message.WebDataReadFailed);
                return;
            }
            var list = doc.FirstChild.ChildNodes;
            var langs = new string[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                var nameXN = list.Item(i).SelectSingleNode("name");
                langs[i] = Path.GetFileNameWithoutExtension(nameXN.InnerText);
            }
            if (langs.Length == 0)
            {
                AppMessageBox.Show(AppString.Message.WebDataReadFailed);
                return;
            }
            using var dlg = new SelectDialog();
            dlg.Items = langs;
            dlg.Title = AppString.Dialog.DownloadLanguages;
            var lang = CultureInfo.CurrentUICulture.Name;
            if (dlg.Items.Contains(lang)) dlg.Selected = lang;
            else dlg.SelectedIndex = 0;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                var fileName = $"{dlg.Selected}.ini";
                var filePath = $@"{AppConfig.LangsDir}\{fileName}";
                var dirUrl = AppConfig.RequestUseGithub ? AppConfig.GithubLangsRawDir : AppConfig.GiteeLangsRawDir;
                var fileUrl = $"{dirUrl}/{fileName}";
                var flag = await client.WebStringToFileAsync(filePath, fileUrl);
                if (!flag)
                {
                    if (AppMessageBox.Show(AppString.Message.WebDataReadFailed + "\r\n ● " + fileName + "\r\n"
                        + AppString.Message.OpenWebUrl, MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        ExternalProgram.OpenWebUrl(fileUrl);
                    }
                }
                else
                {
                    LoadLanguages();
                    var language = new IniWriter(filePath).GetValue("General", "Language");
                    if (language == "") language = dlg.Selected;
                    cmbLanguages.Text = language;
                    ChangeLanguage();
                }
            }
        }

        private sealed class TranslateDialog : CommonDialog
        {
            public override void Reset() { }

            protected override bool RunDialog(IntPtr hwndOwner)
            {
                using var frm = new TranslateForm();
                frm.TopMost = true;
                return frm.ShowDialog() == DialogResult.OK;
            }

            private sealed class TranslateForm : RForm
            {
                public TranslateForm()
                {
                    SuspendLayout();
                    CancelButton = btnCancel;
                    Font = SystemFonts.MessageBoxFont;
                    SizeGripStyle = SizeGripStyle.Hide;
                    Text = AppString.Dialog.TranslateTool;
                    ShowInTaskbar = MinimizeBox = false;
                    Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                    StartPosition = FormStartPosition.CenterParent;
                    InitializeComponents();
                    ResumeLayout();
                    InitTheme();
                }

                private readonly Label lblSections = new()
                {
                    AutoSize = true,
                    Text = "Section"
                };
                private readonly Label lblKeys = new()
                {
                    AutoSize = true,
                    Text = "Key"
                };
                private readonly Label lblDefault = new()
                {
                    Text = AppString.Dialog.DefaultText,
                    AutoSize = true
                };
                private readonly Label lblOld = new()
                {
                    Text = AppString.Dialog.OldTranslation,
                    AutoSize = true
                };
                private readonly Label lblNew = new()
                {
                    Text = AppString.Dialog.NewTranslation,
                    AutoSize = true
                };
                private readonly TextBox txtDefault = new()
                {
                    Multiline = true,
                    ReadOnly = true
                };
                private readonly TextBox txtOld = new()
                {
                    Multiline = true,
                    ReadOnly = true
                };
                private readonly TextBox txtNew = new()
                {
                    Multiline = true
                };
                private readonly RComboBox cmbSections = new()
                {
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                private readonly RComboBox cmbKeys = new()
                {
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                private readonly Button btnBrowse = new()
                {
                    Text = AppString.Dialog.Browse,
                    AutoSize = true
                };
                private readonly Button btnSave = new()
                {
                    Text = AppString.Menu.Save,
                    AutoSize = true
                };
                private readonly Button btnCancel = new()
                {
                    DialogResult = DialogResult.Cancel,
                    Text = ResourceString.Cancel,
                    AutoSize = true
                };

                static TranslateForm()
                {
                    foreach (var section in AppString.DefLangReader.Sections)
                    {
                        var dic = new Dictionary<string, string>();
                        foreach (var key in AppString.DefLangReader.GetSectionKeys(section))
                        {
                            dic.Add(key, string.Empty);
                        }
                        EditingDic.Add(section, dic);
                    }
                }

                private static readonly Dictionary<string, Dictionary<string, string>> EditingDic
                    = new();

                private static readonly IniWriter ReferentialWirter = new();

                private string Section => cmbSections.Text;
                private string Key => cmbKeys.Text;

                private void InitializeComponents()
                {
                    Controls.AddRange(new Control[] { lblSections, cmbSections, lblKeys,
                    cmbKeys, lblDefault, txtDefault, lblOld, txtOld, lblNew,
                    txtNew, btnBrowse, btnSave, btnCancel });

                    txtDefault.SetAutoShowScroll(ScrollBars.Vertical);
                    txtOld.SetAutoShowScroll(ScrollBars.Vertical);
                    txtNew.SetAutoShowScroll(ScrollBars.Vertical);
                    txtDefault.CanSelectAllWhenReadOnly();
                    txtOld.CanSelectAllWhenReadOnly();
                    cmbSections.AutosizeDropDownWidth();
                    cmbKeys.AutosizeDropDownWidth();

                    var a = 20.DpiZoom();

                    lblSections.Top = lblSections.Left = cmbSections.Top = lblKeys.Left
                        = lblDefault.Left = lblOld.Left = lblNew.Left = btnBrowse.Left = a;

                    lblKeys.Top = cmbKeys.Top = cmbSections.Bottom + a;
                    lblDefault.Top = txtDefault.Top = cmbKeys.Bottom + a;
                    txtDefault.Height = txtOld.Height = txtNew.Height = 4 * a;
                    cmbSections.Width = cmbKeys.Width = txtDefault.Width = txtOld.Width = txtNew.Width = 20 * a;

                    var h = cmbSections.Height + cmbKeys.Height + btnBrowse.Height;
                    int[] ws = { lblSections.Width, lblKeys.Width, lblDefault.Width, lblOld.Width, lblNew.Width };
                    var w = ws.Max();

                    cmbSections.Left = cmbKeys.Left = txtDefault.Left = txtOld.Left = txtNew.Left = w + 2 * a;

                    Resize += (sender, e) =>
                    {
                        txtDefault.Height = txtOld.Height = txtNew.Height
                            = (ClientSize.Height - h - 7 * a) / 3;

                        lblOld.Top = txtOld.Top = txtDefault.Bottom + a;
                        lblNew.Top = txtNew.Top = txtOld.Bottom + a;
                        btnBrowse.Top = btnSave.Top = btnCancel.Top = txtNew.Bottom + a;

                        cmbSections.Width = cmbKeys.Width = txtDefault.Width = txtOld.Width = txtNew.Width
                            = ClientSize.Width - (w + 3 * a);

                        btnCancel.Left = ClientSize.Width - btnCancel.Width - a;
                        btnSave.Left = btnCancel.Left - btnSave.Width - a;
                        btnBrowse.Left = btnSave.Left - btnBrowse.Width - a;
                    };
                    ClientSize = new Size(w + 23 * a, h + 3 * 4 * a + 7 * a);
                    MinimumSize = Size;

                    cmbSections.Items.AddRange(AppString.DefLangReader.Sections);
                    cmbSections.SelectedIndexChanged += (sender, e) =>
                    {
                        cmbKeys.Items.Clear();
                        cmbKeys.Items.AddRange(AppString.DefLangReader.GetSectionKeys(Section));
                        cmbKeys.SelectedIndex = 0;
                    };
                    cmbKeys.SelectedIndexChanged += (sender, e) =>
                    {
                        txtDefault.Text = AppString.DefLangReader.GetValue(Section, Key).Replace("\\r", "\r").Replace("\\n", "\n");
                        txtOld.Text = ReferentialWirter.GetValue(Section, Key).Replace("\\r", "\r").Replace("\\n", "\n");
                        txtNew.Text = EditingDic[Section][Key].Replace("\\r", "\r").Replace("\\n", "\n");
                    };
                    cmbSections.SelectedIndex = 0;

                    txtOld.TextChanged += (sender, e) => { if (txtNew.Text == string.Empty) txtNew.Text = txtOld.Text; };
                    txtNew.TextChanged += (sender, e) => EditingDic[Section][Key] = txtNew.Text.Replace("\n", "\\n").Replace("\r", "\\r");
                    btnBrowse.Click += (sender, e) => SelectFile();
                    btnSave.Click += (sender, e) => Save();
                }

                private void SelectFile()
                {
                    using var dlg = new OpenFileDialog();
                    dlg.InitialDirectory = AppConfig.LangsDir;
                    dlg.Filter = $"{AppString.SideBar.AppLanguage}|*.ini";
                    if (dlg.ShowDialog() != DialogResult.OK) return;
                    ReferentialWirter.FilePath = dlg.FileName;
                    txtOld.Text = ReferentialWirter.GetValue(Section, Key).Replace("\\r", "\r").Replace("\\n", "\n");
                }

                private void Save()
                {
                    using var dlg = new SaveFileDialog();
                    var language = EditingDic["General"]["Language"];
                    var index = language.IndexOf(' ');
                    if (index > 0) language = language[..index];
                    dlg.FileName = $"{language}.ini";
                    dlg.InitialDirectory = AppConfig.LangsDir;
                    dlg.Filter = $"{AppString.SideBar.AppLanguage}|*.ini";
                    if (dlg.ShowDialog() != DialogResult.OK) return;

                    var contents = string.Empty;
                    foreach (var section in EditingDic.Keys)
                    {
                        contents += $"[{section}]" + "\r\n";
                        foreach (var key in EditingDic[section].Keys)
                        {
                            var value = EditingDic[section][key];
                            contents += $"{key} = {value}" + "\r\n";
                        }
                        contents += "\r\n";
                    }
                    File.WriteAllText(dlg.FileName, contents, Encoding.Unicode);
                }
            }
        }
    }
}