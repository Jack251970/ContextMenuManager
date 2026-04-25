using Microsoft.Win32.TaskScheduler;
using System;
using System.Diagnostics;
using System.Security.Principal;

namespace ContextMenuManager.Methods
{
    /// <summary>Helper for managing the Windows Task Scheduler logon-restore task.</summary>
    internal static class LogonTaskHelper
    {
        public const string TaskName = "ContextMenuManager_LogonRestore";
        public const string LogonRestoreArg = "/logon-restore";
        private const string LogonTaskDesc = "Restores the context menu backup automatically when the user logs on.";

        /// <summary>Returns true if the current process is running with administrator privileges.</summary>
        public static bool IsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>Returns true if the logon-restore scheduled task currently exists.</summary>
        public static bool IsTaskEnabled()
        {
            try
            {
                return TaskService.Instance.FindTask(TaskName) != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Creates (or replaces) the logon-restore scheduled task.</summary>
        public static bool EnableTask()
        {
            try
            {
                var appPath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(appPath)) return false;

                using var td = TaskService.Instance.NewTask();
                td.RegistrationInfo.Description = LogonTaskDesc;
                td.Triggers.Add(new LogonTrigger
                {
                    UserId = WindowsIdentity.GetCurrent().Name,
                    Delay = TimeSpan.FromSeconds(2)
                });
                td.Actions.Add(new ExecAction(appPath, LogonRestoreArg));

                // Only set highest run-level when already running as administrator.
                if (IsAdministrator())
                {
                    td.Principal.RunLevel = TaskRunLevel.Highest;
                }

                td.Settings.StopIfGoingOnBatteries = false;
                td.Settings.DisallowStartIfOnBatteries = false;
                td.Settings.ExecutionTimeLimit = TimeSpan.Zero;
                td.Settings.Priority = ProcessPriorityClass.Normal;

                TaskService.Instance.RootFolder.RegisterTaskDefinition(TaskName, td);
                return true;
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
                // exceptionOnNotExists = false: silently succeed if the task no longer exists.
                TaskService.Instance.RootFolder.DeleteTask(TaskName, false);
                return true;
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
