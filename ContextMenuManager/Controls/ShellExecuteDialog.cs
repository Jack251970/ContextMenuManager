using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    sealed class ShellExecuteDialog : CommonDialog
    {
        public string Verb { get; set; }
        public int WindowStyle { get; set; }
        public override void Reset() { }

        protected override bool RunDialog(IntPtr hwndOwner)
        {
            using (ShellExecuteForm frm = new ShellExecuteForm())
            {
                frm.TopMost = true;
                bool flag = frm.ShowDialog() == DialogResult.OK;
                if (flag)
                {
                    Verb = frm.Verb;
                    WindowStyle = frm.WindowStyle;
                }
                return flag;
            }
        }

        public static string GetCommand(string fileName, string arguments, string verb, int windowStyle, string directory = null)
        {
            arguments = arguments.Replace("\"", "\"\"");
            if (directory == null)
            {
                ObjectPath.GetFullFilePath(fileName, out string filePath);
                directory = Path.GetDirectoryName(filePath);
            }
            return "mshta vbscript:createobject(\"shell.application\").shellexecute" +
                $"(\"{fileName}\",\"{arguments}\",\"{directory}\",\"{verb}\",{windowStyle})(close)";
        }

        sealed class ShellExecuteForm : RForm
        {
            private const string ApiInfoUrl = "https://docs.microsoft.com/windows/win32/api/shellapi/nf-shellapi-shellexecutea";
            private static readonly string[] Verbs = new[] { "open", "runas", "edit", "print", "find", "explore" };
            public ShellExecuteForm()
            {
                SuspendLayout();
                HelpButton = true;
                Text = "ShellExecute";
                AcceptButton = btnOK;
                CancelButton = btnCancel;
                Font = SystemFonts.MenuFont;
                FormBorderStyle = FormBorderStyle.FixedSingle;
                StartPosition = FormStartPosition.CenterParent;
                ShowIcon = ShowInTaskbar = MaximizeBox = MinimizeBox = false;
                HelpButtonClicked += (sender, e) => ExternalProgram.OpenWebUrl(ApiInfoUrl);
                InitializeComponents();
                ResumeLayout();
                InitTheme();
            }
            public string Verb { get; set; }
            public int WindowStyle { get; set; }

            readonly RadioButton[] rdoVerbs = new RadioButton[6];
            readonly GroupBox grpVerb = new GroupBox { Text = "Verb" };
            readonly Label lblStyle = new Label
            {
                Text = "WindowStyle",
                AutoSize = true
            };
            readonly NumericUpDown nudStyle = new NumericUpDown
            {
                ForeColor = MyMainForm.FormFore,
                BackColor = MyMainForm.ButtonMain,
                TextAlign = HorizontalAlignment.Center,
                Width = 80.DpiZoom(),
                Maximum = 10,
                Minimum = 0,
                Value = 1
            };
            readonly Button btnOK = new Button
            {
                Text = ResourceString.OK,
                DialogResult = DialogResult.OK,
                AutoSize = true
            };
            readonly Button btnCancel = new Button
            {
                Text = ResourceString.Cancel,
                DialogResult = DialogResult.Cancel,
                AutoSize = true
            };

            private void InitializeComponents()
            {
                Controls.AddRange(new Control[] { grpVerb, lblStyle, nudStyle, btnOK, btnCancel });
                int a = 10.DpiZoom();
                int b = 2 * a;
                for (int i = 0; i < 6; i++)
                {
                    rdoVerbs[i] = new RadioButton
                    {
                        Text = Verbs[i],
                        AutoSize = true,
                        Parent = grpVerb,
                        Location = new Point(a, b + a)
                    };
                    if (i > 0) rdoVerbs[i].Left += rdoVerbs[i - 1].Right;
                }
                rdoVerbs[0].Checked = true;
                grpVerb.Width = rdoVerbs[5].Right + a;
                grpVerb.Height = rdoVerbs[5].Bottom + b;
                lblStyle.Left = grpVerb.Left = grpVerb.Top = b;
                btnOK.Top = btnCancel.Top = lblStyle.Top = nudStyle.Top = grpVerb.Bottom + b;
                nudStyle.Left = lblStyle.Right + b;
                btnCancel.Left = grpVerb.Right - btnCancel.Width;
                btnOK.Left = btnCancel.Left - btnOK.Width - b;
                ClientSize = new Size(btnCancel.Right + b, btnCancel.Bottom + b);
                btnOK.Click += (sender, e) =>
                {
                    for (int i = 0; i < 6; i++)
                    {
                        if (rdoVerbs[i].Checked)
                        {
                            Verb = rdoVerbs[i].Text;
                            break;
                        }
                    }
                    WindowStyle = (int)nudStyle.Value;
                };
            }
        }
    }

    sealed class ShellExecuteCheckBox : CheckBox
    {
        public ShellExecuteCheckBox()
        {
            Text = "ShellExecute";
            AutoSize = true;
            //Font = SystemFonts.DialogFont;
            //Font = new Font(Font.FontFamily, Font.Size - 1F);
        }

        public string Verb { get; set; }
        public int WindowStyle { get; set; }

        readonly ToolTip ttpInfo = new ToolTip { InitialDelay = 1 };

        protected override void OnClick(EventArgs e)
        {
            if (Checked)
            {
                Checked = false;
                ttpInfo.RemoveAll();
            }
            else
            {
                using (ShellExecuteDialog dlg = new ShellExecuteDialog())
                {
                    if (dlg.ShowDialog() != DialogResult.OK) return;
                    Verb = dlg.Verb;
                    WindowStyle = dlg.WindowStyle;
                    Checked = true;
                    ttpInfo.SetToolTip(this, $"Verb: \"{Verb}\"\nWindowStyle: {WindowStyle}");
                }
            }
        }
    }
}