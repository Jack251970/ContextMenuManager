using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;

namespace ContextMenuManager.Controls.Interfaces
{
    internal interface ITsiRegExportItem
    {
        string Text { get; set; }
        string RegPath { get; }
        MenuFlyout Flyout { get; set; }
        RegExportMenuItem TsiRegExport { get; set; }
    }

    internal sealed class RegExportMenuItem : RToolStripMenuItem
    {
        public RegExportMenuItem(ITsiRegExportItem item) : base(AppString.Menu.ExportRegistry)
        {
            item.Flyout.Opened += (sender, e) =>
            {
                using var key = RegistryEx.GetRegistryKey(item.RegPath);
                Visibility = key != null ? Visibility.Visible : Visibility.Collapsed;
            };
            Click += (sender, e) =>
            {
                var dlg = new SaveFileDialog();
                var date = DateTime.Today.ToString("yyyy-MM-dd");
                var time = DateTime.Now.ToString("HH-mm-ss");
                var filePath = $@"{AppConfig.RegBackupDir}\{date}\{item.Text} - {time}.reg";
                var dirPath = Path.GetDirectoryName(filePath);
                var fileName = Path.GetFileName(filePath);
                Directory.CreateDirectory(dirPath);
                dlg.FileName = fileName;
                dlg.InitialDirectory = dirPath;
                dlg.Filter = $"{AppString.Dialog.RegistryFile}|*.reg";
                if (dlg.ShowDialog() == true)
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