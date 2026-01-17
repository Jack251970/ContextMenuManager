using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Controls.Interfaces;
using ContextMenuManager.Methods;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using static ContextMenuManager.Methods.ObjectPath;

namespace ContextMenuManager.Controls
{
    class FoldSubItem : MyListItem
    {
        public FoldGroupItem FoldGroupItem { get; set; }

        public void Indent()
        {
            int w = 40.DpiZoom();
            Controls["Image"].Left += w;
            Controls["Text"].Left += w;
        }
    }

    class FoldGroupItem : MyListItem, IBtnShowMenuItem
    {
        private bool isFold;
        public bool IsFold
        {
            get => isFold;
            set
            {
                if(isFold == value) return;
                isFold = value;
                FoldMe(value);
            }
        }

        public string GroupPath { get; set; }
        public PathType PathType { get; set; }

        public MenuButton BtnShowMenu { get; set; }
        readonly PictureButton btnFold;
        readonly PictureButton btnOpenPath;
        readonly RToolStripMenuItem tsiFoldAll = new RToolStripMenuItem(AppString.Menu.FoldAll);
        readonly RToolStripMenuItem tsiUnfoldAll = new RToolStripMenuItem(AppString.Menu.UnfoldAll);

        public FoldGroupItem(string groupPath, PathType pathType)
        {
            btnFold = new PictureButton(AppImage.Up);
            BtnShowMenu = new MenuButton(this);
            btnOpenPath = new PictureButton(AppImage.Open);

            if(pathType == PathType.File || pathType == PathType.Directory)
            {
                groupPath = Environment.ExpandEnvironmentVariables(groupPath);
            }
            string tip = null;
            Action openPath = null;
            switch(pathType)
            {
                case PathType.File:
                    tip = AppString.Menu.FileLocation;
                    Text = Path.GetFileNameWithoutExtension(groupPath);
                    Image = ResourceIcon.GetExtensionIcon(groupPath).ToBitmap();
                    openPath = () => ExternalProgram.JumpExplorer(groupPath, AppConfig.OpenMoreExplorer);
                    break;
                case PathType.Directory:
                    tip = AppString.Menu.FileLocation;
                    Text = Path.GetFileNameWithoutExtension(groupPath);
                    Image = ResourceIcon.GetFolderIcon(groupPath).ToBitmap();
                    openPath = () => ExternalProgram.OpenDirectory(groupPath);
                    break;
                case PathType.Registry:
                    tip = AppString.Menu.RegistryLocation;
                    openPath = () => ExternalProgram.JumpRegEdit(groupPath, null, AppConfig.OpenMoreRegedit);
                    break;
            }
            PathType = pathType;
            GroupPath = groupPath;
            Font = new Font(Font, FontStyle.Bold);
            AddCtrs(new[] { btnFold, btnOpenPath });
            ContextMenuStrip.Items.AddRange(new[] { tsiFoldAll, tsiUnfoldAll });
            MouseDown += (sender, e) =>
            {
                if(e.Button == MouseButtons.Left) Fold();
            };
            btnFold.MouseDown += (sender, e) =>
            {
                Fold();
                btnFold.Image = btnFold.BaseImage;
            };
            tsiFoldAll.Click += (sender, e) => FoldAll(true);
            tsiUnfoldAll.Click += (sender, e) => FoldAll(false);
            btnOpenPath.MouseDown += (sender, e) => openPath.Invoke();
            ToolTipBox.SetToolTip(btnOpenPath, tip);
        }

        public void SetVisibleWithSubItemCount()
        {
            foreach(Control ctr in Parent.Controls)
            {
                if(ctr is FoldSubItem item && item.FoldGroupItem == this)
                {
                    Visible = true;
                    return;
                }
            }
            Visible = false;
        }

        private void Fold()
        {
            Parent.SuspendLayout();
            IsFold = !IsFold;
            Parent.ResumeLayout();
        }

        private void FoldMe(bool isFold)
        {
            btnFold.BaseImage = isFold ? AppImage.Down : AppImage.Up;
            foreach(Control ctr in Parent?.Controls)
            {
                if(ctr is FoldSubItem item && item.FoldGroupItem == this) ctr.Visible = !isFold;
            }
        }

        private void FoldAll(bool isFold)
        {
            Parent.SuspendLayout();
            foreach(Control ctr in Parent.Controls)
            {
                if(ctr is FoldGroupItem groupItem) groupItem.IsFold = isFold;
            }
            Parent.ResumeLayout();
        }
    }
}