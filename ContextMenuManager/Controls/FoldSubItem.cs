using ContextMenuManager.Controls.Interfaces;
using ContextMenuManager.Methods;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using static ContextMenuManager.Methods.ObjectPath;

namespace ContextMenuManager.Controls
{
    internal class FoldSubItem : MyListItem
    {
        public FoldSubItem(MyList list) : base(list)
        {

        }

        public FoldGroupItem FoldGroupItem { get; set; }

        public override void Indent()
        {
            var w = 40;
            if (HasImage) iconHost.Margin = new Thickness(iconHost.Margin.Left + w, iconHost.Margin.Top, iconHost.Margin.Right, iconHost.Margin.Bottom);
            else txtTitle.Margin = new Thickness(txtTitle.Margin.Left + w, txtTitle.Margin.Top, txtTitle.Margin.Right, txtTitle.Margin.Bottom);
        }
    }

    internal class FoldGroupItem : MyListItem, IBtnShowMenuItem
    {
        private bool isFold;
        public bool IsFold
        {
            get => isFold;
            set
            {
                if (isFold == value) return;
                isFold = value;
                FoldMe(value);
            }
        }

        public string GroupPath { get; set; }
        public PathType PathType { get; set; }

        public MenuButton BtnShowMenu { get; set; }
        public ContextMenu ContextMenu
        {
            get => Control.ContextMenu;
            set => Control.ContextMenu = value;
        }

        private readonly GlyphButton btnFold;
        private readonly GlyphButton btnOpenPath;
        private readonly RToolStripMenuItem tsiFoldAll;
        private readonly RToolStripMenuItem tsiUnfoldAll;

        public FoldGroupItem(MyList list, string groupPath, PathType pathType) : base(list)
        {
            if (list != null)
            {
                btnFold = new GlyphButton(AppGlyphs.Up);
                BtnShowMenu = new MenuButton(this);
                btnOpenPath = new GlyphButton(AppGlyphs.Open);

                tsiFoldAll = new(AppString.Menu.FoldAll);
                tsiUnfoldAll = new(AppString.Menu.UnfoldAll);
            }

            if (pathType is PathType.File or PathType.Directory)
            {
                groupPath = Environment.ExpandEnvironmentVariables(groupPath);
            }

            PathType = pathType;
            GroupPath = groupPath;

            if (list != null)
            {
                string tip = null;
                Action openPath = null;
                switch (pathType)
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

                txtTitle.FontWeight = FontWeights.Bold;

                AddCtrs([btnFold, btnOpenPath]);
                foreach (var item in new Control[] { tsiFoldAll, tsiUnfoldAll })
                {
                    Control.ContextMenu.Items.Add(item);
                }

                Control.MouseLeftButtonDown += (s, e) => Fold();

                btnFold.Click += (sender, e) => Fold();

                tsiFoldAll.Click += (sender, e) => FoldAll(true);
                tsiUnfoldAll.Click += (sender, e) => FoldAll(false);
                btnOpenPath.Click += (sender, e) => openPath.Invoke();
                ToolTipBox.SetToolTip(btnOpenPath, tip);
            }
        }

        public void SetVisibleWithSubItemCount()
        {
            foreach (var ctr in List.Controls)
            {
                if (ctr.Item is FoldSubItem item && item.FoldGroupItem == this)
                {
                    Visible = true;
                    return;
                }
            }
            Visible = false;
        }

        private void Fold()
        {
            IsFold = !IsFold;
        }

        private void FoldMe(bool isFold)
        {
            btnFold.Icon = isFold ? AppGlyphs.Down : AppGlyphs.Up;
            foreach (var ctr in List.Controls)
            {
                if (ctr.Item is FoldSubItem item && item.FoldGroupItem == this) item.Visible = !isFold;
            }
        }

        private void FoldAll(bool isFold)
        {
            foreach (var ctr in List.Controls)
            {
                if (ctr.Item is FoldGroupItem groupItem) groupItem.IsFold = isFold;
            }
        }
    }
}
