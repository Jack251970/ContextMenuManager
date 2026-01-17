using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    internal sealed class ExplorerRestarter : MyListItem
    {
        public ExplorerRestarter()
        {
            Visible = false;
            DoubleBuffered = false;
            Dock = DockStyle.Bottom;
            Image = AppImage.Explorer;
            Text = AppString.Other.RestartExplorer;
            ToolTipBox.SetToolTip(BtnRestart, AppString.Tip.RestartExplorer);
            AddCtr(BtnRestart);
            this.CanMoveForm();
            ShowHandler += () => Visible = true;
            HideHandler += () => Visible = false;
            BtnRestart.MouseDown += (sender, e) =>
            {
                ExternalProgram.RestartExplorer();
                Visible = false;
            };
        }

        public new bool Visible
        {
            get => base.Visible;
            set
            {
                var flag = base.Visible != value && Parent != null;
                base.Visible = value;
                if (flag) Parent.Height += value ? Height : -Height;
            }
        }

        private readonly PictureButton BtnRestart = new(AppImage.RestartExplorer);

        private static Action ShowHandler;
        private static Action HideHandler;

        public static new void Show()
        {
            ShowHandler?.Invoke();
        }

        public static new void Hide()
        {
            HideHandler?.Invoke();
        }
    }
}