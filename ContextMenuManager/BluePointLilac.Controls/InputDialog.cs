using BluePointLilac.Methods;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public sealed class InputDialog : CommonDialog
    {
        /// <summary>输入对话框标题</summary>
        public string Title { get; set; } = Application.ProductName;
        /// <summary>输入对话框文本框文本</summary>
        public string Text { get; set; }
        public Size Size { get; set; }

        public override void Reset() { }

        protected override bool RunDialog(IntPtr hwndOwner)
        {
            using(InputBox frm = new InputBox())
            {
                frm.Text = Title;
                frm.InputedText = Text;
                frm.Size = Size;
                if (Control.FromHandle(hwndOwner) is Form owner) frm.TopMost = true;
                bool flag = frm.ShowDialog() == DialogResult.OK;
                Text = flag ? frm.InputedText : null;
                return flag;
            }
        }

        sealed class InputBox : RForm
        {
            public InputBox()
            {
                AcceptButton = btnOK;
                CancelButton = btnCancel;
                Font = SystemFonts.MessageBoxFont;
                SizeGripStyle = SizeGripStyle.Hide;
                StartPosition = FormStartPosition.CenterParent;
                MaximizeBox = MinimizeBox = ShowIcon = ShowInTaskbar = false;
                Controls.AddRange(new Control[] { txtInput, btnOK, btnCancel });
                txtInput.Font = new Font(txtInput.Font.FontFamily, txtInput.Font.Size + 2F);
                txtInput.CanResizeFont();
                InitializeComponents();
                InitTheme();
            }

            public string InputedText
            {
                get => txtInput.Text;
                set => txtInput.Text = value;
            }

            readonly TextBox txtInput = new TextBox
            {
                Font = SystemFonts.MenuFont,
                ScrollBars = ScrollBars.Vertical,
                Multiline = true
            };
            readonly Button btnOK = new Button
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                DialogResult = DialogResult.OK,
                Text = ResourceString.OK,
                AutoSize = true
            };
            readonly Button btnCancel = new Button
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                DialogResult = DialogResult.Cancel,
                Text = ResourceString.Cancel,
                AutoSize = true
            };

            private void InitializeComponents()
            {
                SuspendLayout();
                int a = 20.DpiZoom();
                txtInput.Location = new Point(a, a);
                txtInput.Size = new Size(340, 24).DpiZoom();
                ClientSize = new Size(txtInput.Width + a * 2, txtInput.Height + btnOK.Height + a * 3);
                btnCancel.Top = btnOK.Top = txtInput.Bottom + a;
                btnCancel.Left = txtInput.Right - btnCancel.Width;
                btnOK.Left = btnCancel.Left - btnOK.Width - a;
                ResumeLayout();
                MinimumSize = Size;
                Resize += (sender, e) =>
                {
                    txtInput.Width = ClientSize.Width - 2 * a;
                    txtInput.Height = btnCancel.Top - 2 * a;
                };
            }
        }
    }
}