﻿using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System.Windows.Forms;

namespace ContextMenuManager.Controls.Interfaces
{
    interface ITsiRegPathItem
    {
        string RegPath { get; }
        string ValueName { get; }
        ContextMenuStrip ContextMenuStrip { get; set; }
        RegLocationMenuItem TsiRegLocation { get; set; }
    }

    sealed class RegLocationMenuItem : RToolStripMenuItem
    {
        public RegLocationMenuItem(ITsiRegPathItem item) : base(AppString.Menu.RegistryLocation)
        {
            Click += (sender, e) => ExternalProgram.JumpRegEdit(item.RegPath, item.ValueName, AppConfig.OpenMoreRegedit);
            item.ContextMenuStrip.Opening += (sender, e) =>
            {
                using(var key = RegistryEx.GetRegistryKey(item.RegPath))
                    Visible = key != null;
            };
        }
    }
}