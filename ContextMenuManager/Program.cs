using ContextMenuManager.Controls;
using ContextMenuManager.Methods;
using System;
using System.Collections.Generic;
using System.IO;

namespace ContextMenuManager
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // Check for logon restore command-line argument (silent background restore)
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && args[1].Equals(LogonTaskHelper.LogonRestoreArg, StringComparison.OrdinalIgnoreCase))
            {
                PerformLogonRestore();
                return;
            }

            // 启动WPF应用
            if (SingleInstance<App>.InitializeAsFirstInstance())
            {
                // 初始化字符串、更新检查和XML字典
                AppString.LoadStrings();
                XmlDicHelper.ReloadDics();

                using var application = new App();
                application.InitializeComponent();
                application.Run();
            }
        }

        /// <summary>Performs a silent backup restore for the logon scheduled task (no UI).</summary>
        private static void PerformLogonRestore()
        {
            try
            {
                AppString.LoadStrings();

                var filePath = AppConfig.LogonRestoreFilePath;
                var scenesStr = AppConfig.LogonRestoreScenes;
                var restoreMode = (RestoreMode)AppConfig.LogonRestoreMode;

                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                    return;

                var sceneTexts = LogonTaskHelper.ParseSceneTexts(scenesStr);
                if (sceneTexts.Count == 0)
                    return;

                BackupList.LoadBackupDataMetaData(filePath);
                if (BackupList.metaData == null
                    || BackupList.metaData.Version <= BackupHelper.DeprecatedBackupVersion)
                    return;

                var helper = new BackupHelper();
                var silentReporter = new LoadingDialogInterface();
                helper.RestoreItems(filePath, sceneTexts, restoreMode, silentReporter);
            }
            catch
            {
                // Silent – do not surface errors during a logon task
            }
        }
    }
}
