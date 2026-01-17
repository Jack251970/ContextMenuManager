using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System;
using System.IO;
using System.Windows.Forms;

namespace ContextMenuManager.Controls.Interfaces
{
    internal interface ITsiRegExportItem
    {
        string Text { get; set; }
        string RegPath { get; }
        ContextMenuStrip ContextMenuStrip { get; set; }
        RegExportMenuItem TsiRegExport { get; set; }
    }

    internal sealed class RegExportMenuItem : RToolStripMenuItem
    {
        public RegExportMenuItem(ITsiRegExportItem item) : base(AppString.Menu.ExportRegistry)
        {
            item.ContextMenuStrip.Opening += (sender, e) =>
            {
                using var key = RegistryEx.GetRegistryKey(item.RegPath);
                Visible = key != null;
            };
            Click += (sender, e) =>
            {
                using var dlg = new SaveFileDialog();
                var date = DateTime.Today.ToString("yyyy-MM-dd");
                var time = DateTime.Now.ToString("HH-mm-ss");
                var filePath = $@"{AppConfig.RegBackupDir}\{date}\{item.Text} - {time}.reg";
                var dirPath = Path.GetDirectoryName(filePath);
                var fileName = Path.GetFileName(filePath);
                Directory.CreateDirectory(dirPath);
                dlg.FileName = fileName;
                dlg.InitialDirectory = dirPath;
                dlg.Filter = $"{AppString.Dialog.RegistryFile}|*.reg";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    ExternalProgram.ExportRegistry(item.RegPath, dlg.FileName);
                }
                if (Directory.GetFileSystemEntries(dirPath).Length == 0)
                {
                    Directory.Delete(dirPath);
                }
            };
        }
    }
}