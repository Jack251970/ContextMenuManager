using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ContextMenuManager.Methods
{
    // http://blogs.microsoft.co.il/arik/2010/05/28/wpf-single-instance-application/
    // modified to allow single instace restart
    public interface ISingleInstanceApp
    {
        void OnSecondAppStarted();
    }

    /// <summary>
    /// This class checks to make sure that only one instance of 
    /// this application is running at a time.
    /// </summary>
    /// <remarks>
    /// Note: this class should be used with some caution, because it does no
    /// security checking. For example, if one instance of an app that uses this class
    /// is running as Administrator, any other instance, even if it is not
    /// running as Administrator, can activate it with command line arguments.
    /// For most apps, this will not be much of an issue.
    /// </remarks>
    public static class SingleInstance<TApplication> where TApplication : Application, ISingleInstanceApp
    {
        #region Private Fields

        /// <summary>
        /// String delimiter used in channel names.
        /// </summary>
        private const string Delimiter = ":";

        /// <summary>
        /// Suffix to the channel name.
        /// </summary>
        private const string ChannelNameSuffix = "SingeInstanceIPCChannel";
        private const string InstanceMutexName = "ContextMenuManager_Unique_Application_Mutex";

        /// <summary>
        /// Application mutex.
        /// </summary>
        internal static Mutex SingleInstanceMutex { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Checks if the instance of the application attempting to start is the first instance. 
        /// If not, activates the first instance.
        /// </summary>
        /// <returns>True if this is the first instance of the application.</returns>
        public static bool InitializeAsFirstInstance()
        {
            // Build unique application Id and the IPC channel name.
            var applicationIdentifier = InstanceMutexName + Environment.UserName;

            var channelName = string.Concat(applicationIdentifier, Delimiter, ChannelNameSuffix);

            // Create mutex based on unique application Id to check if this is the first instance of the application. 
            SingleInstanceMutex = new Mutex(true, applicationIdentifier, out var firstInstance);
            if (firstInstance)
            {
                _ = CreateRemoteServiceAsync(channelName);
                return true;
            }
            else
            {
                _ = SignalFirstInstanceAsync(channelName);
                return false;
            }
        }

        /// <summary>
        /// Cleans up single-instance code, clearing shared resources, mutexes, etc.
        /// </summary>
        public static void Cleanup()
        {
            SingleInstanceMutex?.ReleaseMutex();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates a remote server pipe for communication. 
        /// Once receives signal from client, will activate first instance.
        /// </summary>
        /// <param name="channelName">Application's IPC channel name.</param>
        private static async Task CreateRemoteServiceAsync(string channelName)
        {
            using var pipeServer = new NamedPipeServerStream(channelName, PipeDirection.In);
            while (true)
            {
                // Wait for connection to the pipe
                await pipeServer.WaitForConnectionAsync();

                // Do an asynchronous call to ActivateFirstInstance function
                Application.Current?.Dispatcher.Invoke(ActivateFirstInstance);

                // Disconect client
                pipeServer.Disconnect();
            }
        }

        /// <summary>
        /// Creates a client pipe and sends a signal to server to launch first instance
        /// </summary>
        /// <param name="channelName">Application's IPC channel name.</param>
        /// <param name="args">
        /// Command line arguments for the second instance, passed to the first instance to take appropriate action.
        /// </param>
        private static async Task SignalFirstInstanceAsync(string channelName)
        {
            // Create a client pipe connected to server
            using var pipeClient = new NamedPipeClientStream(".", channelName, PipeDirection.Out);

            // Connect to the available pipe
            await pipeClient.ConnectAsync(0);
        }

        /// <summary>
        /// Activates the first instance of the application with arguments from a second instance.
        /// </summary>
        /// <param name="args">List of arguments to supply the first instance of the application.</param>
        private static void ActivateFirstInstance()
        {
            // Set main window state and process command line args
            if (Application.Current == null)
            {
                return;
            }

            ((TApplication)Application.Current).OnSecondAppStarted();
        }

        #endregion
    }

    public static class SingleInstance
    {
        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int cmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>判断单实例程序是否正在运行</summary>
        /// <remarks>若正在运行激活窗口</remarks>
        public static bool IsRunning()
        {
            using var current = Process.GetCurrentProcess();
            foreach (var process in Process.GetProcessesByName(current.ProcessName))
            {
                using (process)
                {
                    if (process.Id == current.Id) continue;
                    if (process.MainModule.FileName == current.MainModule.FileName)
                    {
                        const int SW_RESTORE = 9;
                        ShowWindowAsync(process.MainWindowHandle, SW_RESTORE);
                        SetForegroundWindow(process.MainWindowHandle);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>重启单实例程序</summary>
        /// <param name="args">重启程序时传入参数</param>
        /// <param name="updatePath">用于更新程序的新版本文件路径，为null则为普通重启</param>
        public static void Restart(string[] args = null, string updatePath = null)
        {
            var appPath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(appPath)) return;

            // 1. 处理参数：使用空格分隔，并确保路径带有双引号
            var arguments = (args != null && args.Length > 0) ? string.Join(" ", args) : "";

            var contents = new List<string>
            {
                "On Error Resume Next",
                "WScript.Sleep 1500", // 给 WPF 进程留出足够的物理退出时间
                "Dim wsh, fso",
                "Set wsh = CreateObject(\"WScript.Shell\")",
                "Set fso = CreateObject(\"Scripting.FileSystemObject\")"
            };

            // 使用 """ 来在 VBS 字符串内部表示一个双引号
            if (!string.IsNullOrEmpty(updatePath) && File.Exists(updatePath))
            {
                contents.AddRange(
                [
                    $"If fso.FileExists(\"{appPath}\") Then fso.DeleteFile \"{appPath}\", True",
                    "WScript.Sleep 500",
                    $"fso.MoveFile \"{updatePath}\", \"{appPath}\"",
                    "WScript.Sleep 500"
                ]);
            }

            // 2. 关键点：用 Chr(34) 或多重引号包裹路径，防止空格截断
            // 最终命令形式应该是：wsh.Run """C:\Path With Space\App.exe"" args"
            var runCommand = $"wsh.Run \"\"\"{appPath}\"\" {arguments}\", 1, False";
            contents.Add(runCommand);

            contents.Add("Set wsh = Nothing");
            contents.Add("Set fso = Nothing");
            // 脚本自删（放在最后执行）
            contents.Add("fso.DeleteFile WScript.ScriptFullName");

            var vbsPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".vbs");
            // 使用 Default 编码（通常是 ANSI），VBS 对 Unicode 支持有时较怪异
            File.WriteAllLines(vbsPath, contents, Encoding.Default);

            Process.Start(new ProcessStartInfo
            {
                FileName = "wscript.exe",
                Arguments = $"\"{vbsPath}\"",
                UseShellExecute = true,
                CreateNoWindow = true
            });

            Application.Current.Shutdown();
        }
    }
}
