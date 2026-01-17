using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    internal class NewItemForm : ResizeLimitedForm
    {
        public NewItemForm()
        {
            AcceptButton = btnOK;
            CancelButton = btnCancel;
            Text = AppString.Other.NewItem;
            Font = SystemFonts.MenuFont;
            MaximizeBox = MinimizeBox = false;
            ShowIcon = ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            SizeGripStyle = SizeGripStyle.Hide;
            VerticalResizable = false;
            InitializeComponents();
        }

        public string ItemText { get => txtText.Text; set => txtText.Text = value; }
        public string ItemFilePath { get => txtFilePath.Text; set => txtFilePath.Text = value; }
        public string Arguments { get => txtArguments.Text; set => txtArguments.Text = value; }
        public string ItemCommand
        {
            get
            {
                var filePath = ItemFilePath;
                var arguments = Arguments;
                if (arguments.IsNullOrWhiteSpace()) return filePath;
                if (filePath.IsNullOrWhiteSpace()) return arguments;
                if (filePath.Contains(" ")) filePath = $"\"{filePath}\"";
                if (!arguments.Contains("\"")) arguments = $"\"{arguments}\"";
                return $"{filePath} {arguments}";
            }
        }

        protected readonly Label lblText = new()
        {
            Text = AppString.Dialog.ItemText,
            AutoSize = true
        };
        protected readonly Label lblCommand = new()
        {
            Text = AppString.Dialog.ItemCommand,
            AutoSize = true
        };
        protected readonly Label lblArguments = new()
        {
            Text = AppString.Dialog.CommandArguments,
            AutoSize = true
        };
        protected readonly TextBox txtText = new();
        protected readonly TextBox txtFilePath = new();
        protected readonly TextBox txtArguments = new();
        protected readonly Button btnBrowse = new()
        {
            Text = AppString.Dialog.Browse,
            AutoSize = true
        };
        protected readonly Button btnOK = new()
        {
            Text = ResourceString.OK,
            AutoSize = true
        };
        protected readonly Button btnCancel = new()
        {
            DialogResult = DialogResult.Cancel,
            Text = ResourceString.Cancel,
            AutoSize = true
        };

        protected virtual void InitializeComponents()
        {
            Controls.AddRange(new Control[] { lblText, lblCommand, lblArguments,
                txtText, txtFilePath, txtArguments, btnBrowse, btnOK, btnCancel });
            var a = 20.DpiZoom();
            btnBrowse.Anchor = btnOK.Anchor = btnCancel.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            txtText.Top = lblText.Top = lblText.Left = lblCommand.Left = lblArguments.Left = a;
            btnBrowse.Top = txtFilePath.Top = lblCommand.Top = txtText.Bottom + a;
            lblArguments.Top = txtArguments.Top = txtFilePath.Bottom + a;
            btnOK.Top = btnCancel.Top = txtArguments.Bottom + a;
            btnCancel.Left = btnBrowse.Left = ClientSize.Width - btnCancel.Width - a;
            btnOK.Left = btnCancel.Left - btnOK.Width - a;
            var b = Math.Max(Math.Max(lblText.Width, lblCommand.Width), lblArguments.Width) + btnBrowse.Width + 4 * a;
            ClientSize = new Size(320.DpiZoom() + b, btnOK.Bottom + a);
            MinimumSize = Size;
            Resize += (sender, e) =>
            {
                txtText.Width = txtFilePath.Width = txtArguments.Width = ClientSize.Width - b;
                txtText.Left = txtFilePath.Left = txtArguments.Left = btnBrowse.Left - txtFilePath.Width - a;
            };
            OnResize(null);
        }
    }
}