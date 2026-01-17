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
            using var frm = new InputBox();
            frm.Text = Title;
            frm.InputedText = Text;
            frm.Size = Size;
            if (Control.FromHandle(hwndOwner) is Form owner) frm.TopMost = true;
            var flag = frm.ShowDialog() == DialogResult.OK;
            Text = flag ? frm.InputedText : null;
            return flag;
        }

        private sealed class InputBox : RForm
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

                // 监听主题变化
                DarkModeHelper.ThemeChanged += OnThemeChanged;
            }

            public string InputedText
            {
                get => txtInput.Text;
                set => txtInput.Text = value;
            }

            private readonly TextBox txtInput = new()
            {
                Font = SystemFonts.MenuFont,
                ScrollBars = ScrollBars.Vertical,
                Multiline = true
            };
            private readonly Button btnOK = new()
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                DialogResult = DialogResult.OK,
                Text = ResourceString.OK,
                AutoSize = true
            };
            private readonly Button btnCancel = new()
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                DialogResult = DialogResult.Cancel,
                Text = ResourceString.Cancel,
                AutoSize = true
            };

            private void InitializeComponents()
            {
                SuspendLayout();
                var a = 20.DpiZoom();
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

            private new void InitTheme()
            {
                BackColor = DarkModeHelper.FormBack;
                ForeColor = DarkModeHelper.FormFore;

                txtInput.BackColor = DarkModeHelper.FormBack;
                txtInput.ForeColor = DarkModeHelper.FormFore;

                btnOK.BackColor = DarkModeHelper.ButtonMain;
                btnOK.ForeColor = DarkModeHelper.FormFore;
                btnCancel.BackColor = DarkModeHelper.ButtonMain;
                btnCancel.ForeColor = DarkModeHelper.FormFore;
            }

            // 主题变化事件处理
            private void OnThemeChanged(object sender, EventArgs e)
            {
                InitTheme();
                Invalidate();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    DarkModeHelper.ThemeChanged -= OnThemeChanged;
                }
                base.Dispose(disposing);
            }
        }
    }
}