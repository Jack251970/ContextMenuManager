using System;
using System.Diagnostics;

namespace ContextMenuManager.Methods
{
    /// <summary>Helper for managing the Windows Task Scheduler logon-restore task.</summary>
    internal static class LogonTaskHelper
    {
        public const string TaskName = "ContextMenuManager_LogonRestore";
        public const string LogonRestoreArg = "/logon-restore";

        /// <summary>Returns true if the logon-restore scheduled task currently exists.</summary>
        public static bool IsTaskEnabled()
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = $"/query /tn \"{TaskName}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });
                process?.WaitForExit();
                return process?.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Creates the logon-restore scheduled task that runs this executable on user logon.</summary>
        public static bool EnableTask()
        {
            try
            {
                var appPath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(appPath)) return false;

                // Build the task command – the path is quoted in case it contains spaces.
                var taskCommand = $"\\\"{appPath}\\\" {LogonRestoreArg}";
                var arguments = $"/create /tn \"{TaskName}\" /tr \"{taskCommand}\" /sc ONLOGON /rl HIGHEST /f";

                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });
                process?.WaitForExit();
                return process?.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Deletes the logon-restore scheduled task.</summary>
        public static bool DisableTask()
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = $"/delete /tn \"{TaskName}\" /f",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });
                process?.WaitForExit();
                return process?.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Converts a comma-separated string of Scenes enum indices to scene-text strings.</summary>
        public static System.Collections.Generic.List<string> ParseSceneTexts(string scenesStr)
        {
            var result = new System.Collections.Generic.List<string>();
            if (string.IsNullOrWhiteSpace(scenesStr)) return result;

            foreach (var part in scenesStr.Split(','))
            {
                if (int.TryParse(part.Trim(), out var index)
                    && index >= 0
                    && index < BackupHelper.BackupScenesText.Length)
                {
                    result.Add(BackupHelper.BackupScenesText[index]);
                }
            }
            return result;
        }

        /// <summary>Converts a list of scene-text strings to a comma-separated string of Scenes enum indices.</summary>
        public static string BuildScenesString(System.Collections.Generic.IEnumerable<string> sceneTexts)
        {
            var indices = new System.Collections.Generic.List<string>();
            for (var i = 0; i < BackupHelper.BackupScenesText.Length; i++)
            {
                foreach (var text in sceneTexts)
                {
                    if (text == BackupHelper.BackupScenesText[i])
                    {
                        indices.Add(i.ToString());
                        break;
                    }
                }
            }
            return string.Join(",", indices);
        }
    }
}
