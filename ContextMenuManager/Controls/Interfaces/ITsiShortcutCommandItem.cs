using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ContextMenuManager.Controls.Interfaces
{
    internal interface ITsiShortcutCommandItem
    {
        ShellLink ShellLink { get; }
        ShortcutCommandMenuItem TsiChangeCommand { get; set; }
        ContextMenuStrip ContextMenuStrip { get; set; }
    }

    internal sealed class ShortcutCommandMenuItem : RToolStripMenuItem
    {
        public ShortcutCommandMenuItem(ITsiShortcutCommandItem item) : base(AppString.Menu.ChangeCommand)
        {
            item.ContextMenuStrip.Opening += (sender, e) =>
            {
                Visible = !string.IsNullOrEmpty(item.ShellLink?.TargetPath);
            };
        }

        public bool ChangeCommand(ShellLink shellLink)
        {
            using var dlg = new CommandDialog();
            dlg.Command = shellLink.TargetPath;
            dlg.Arguments = shellLink.Arguments;
            if (dlg.ShowDialog() != DialogResult.OK) return false;
            shellLink.TargetPath = dlg.Command;
            shellLink.Arguments = dlg.Arguments;
            shellLink.Save();
            return true;
        }

        private sealed class CommandDialog : CommonDialog
        {
            public string Command { get; set; }
            public string Arguments { get; set; }

            public override void Reset() { }

            protected override bool RunDialog(IntPtr hwndOwner)
            {
                using var frm = new CommandForm();
                frm.Command = Command;
                frm.Arguments = Arguments;
                frm.TopMost = true;
                var flag = frm.ShowDialog() == DialogResult.OK;
                if (flag)
                {
                    Command = frm.Command;
                    Arguments = frm.Arguments;
                }
                return flag;
            }

            private sealed class CommandForm : ResizeLimitedForm
            {
                public CommandForm()
                {
                    AcceptButton = btnOK;
                    CancelButton = btnCancel;
                    VerticalResizable = false;
                    Font = SystemFonts.MessageBoxFont;
                    Text = AppString.Menu.ChangeCommand;
                    SizeGripStyle = SizeGripStyle.Hide;
                    StartPosition = FormStartPosition.CenterParent;
                    MaximizeBox = MinimizeBox = ShowIcon = ShowInTaskbar = false;
                    InitializeComponents();
                }

                public string Command
                {
                    get => txtCommand.Text;
                    set => txtCommand.Text = value;
                }

                public string Arguments
                {
                    get => txtArguments.Text;
                    set => txtArguments.Text = value;
                }

                private readonly Label lblCommand = new()
                {
                    Text = AppString.Dialog.ItemCommand,
                    AutoSize = true
                };
                private readonly Label lblArguments = new()
                {
                    Text = AppString.Dialog.CommandArguments,
                    AutoSize = true
                };
                private readonly TextBox txtCommand = new();
                private readonly TextBox txtArguments = new();
                private readonly Button btnOK = new()
                {
                    DialogResult = DialogResult.OK,
                    Text = ResourceString.OK,
                    AutoSize = true
                };
                private readonly Button btnCancel = new()
                {
                    DialogResult = DialogResult.Cancel,
                    Text = ResourceString.Cancel,
                    AutoSize = true
                };

                private void InitializeComponents()
                {
                    Controls.AddRange(new Control[] { lblCommand, lblArguments, txtCommand, txtArguments, btnOK, btnCancel });
                    var a = 20.DpiZoom();
                    lblArguments.Left = lblCommand.Left = lblCommand.Top = txtCommand.Top = a;
                    lblArguments.Top = txtArguments.Top = txtCommand.Bottom + a;
                    btnOK.Top = btnCancel.Top = txtArguments.Bottom + a;
                    var b = Math.Max(lblCommand.Width, lblArguments.Width) + 3 * a;
                    ClientSize = new Size(250.DpiZoom() + b, btnOK.Bottom + a);
                    btnOK.Anchor = btnCancel.Anchor = AnchorStyles.Right | AnchorStyles.Top;
                    btnCancel.Left = ClientSize.Width - btnCancel.Width - a;
                    btnOK.Left = btnCancel.Left - btnOK.Width - a;
                    Resize += (sender, e) =>
                    {
                        txtArguments.Width = txtCommand.Width = ClientSize.Width - b;
                        txtArguments.Left = txtCommand.Left = btnCancel.Right - txtCommand.Width;
                    };
                    OnResize(null);
                    MinimumSize = Size;
                }
            }
        }
    }
}