using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace BluePointLilac.Methods
{
    /// <summary>外部程序</summary>
    public static class ExternalProgram
    {
        /// <summary>在Regedit中跳转指定路径并定位指定键名</summary>
        /// <param name="regPath">注册表项路径</param>
        /// <param name="valueName">注册表键名</param>
        /// <param name="moreOpen">窗口是否多开</param>
        public static void JumpRegEdit(string regPath, string valueName = null, bool moreOpen = false)
        {
            //还有一种方法，修改HKCU\Software\Microsoft\Windows\CurrentVersion\Applets\Regedit
            //中的LastKey键值（记录上次关闭注册表编辑器时的注册表路径）为要跳转的注册表项路径regPath，
            //再使用Process.Start("regedit.exe", "-m")打开注册表编辑器
            //优点：代码少、不会有Bug。缺点：不能定位具体键，没有逐步展开效果
            if (regPath == null) return;
            Process process;
            var hMain = FindWindow("RegEdit_RegEdit", null);
            if (hMain != IntPtr.Zero && !moreOpen)
            {
                GetWindowThreadProcessId(hMain, out var id);
                process = Process.GetProcessById(id);
            }
            else
            {
                //注册表编辑器窗口多开
                process = Process.Start("regedit.exe", "-m");
                process.WaitForInputIdle();
                hMain = process.MainWindowHandle;
            }

            ShowWindowAsync(hMain, SW_SHOWNORMAL);
            SetForegroundWindow(hMain);
            var hTree = FindWindowEx(hMain, IntPtr.Zero, "SysTreeView32", null);
            var hList = FindWindowEx(hMain, IntPtr.Zero, "SysListView32", null);

            SetForegroundWindow(hTree);
            SetFocus(hTree);
            process.WaitForInputIdle();
            SendMessage(hTree, WM_KEYDOWN, VK_HOME, null);
            Thread.Sleep(100);
            process.WaitForInputIdle();
            SendMessage(hTree, WM_KEYDOWN, VK_RIGHT, null);
            foreach (char chr in Encoding.Default.GetBytes(regPath))
            {
                process.WaitForInputIdle();
                if (chr == '\\')
                {
                    Thread.Sleep(100);
                    SendMessage(hTree, WM_KEYDOWN, VK_RIGHT, null);
                }
                else
                {
                    SendMessage(hTree, WM_CHAR, Convert.ToInt16(chr), null);
                }
            }

            if (string.IsNullOrEmpty(valueName)) return;
            using (var key = RegistryEx.GetRegistryKey(regPath))
            {
                if (key?.GetValue(valueName) == null) return;
            }
            Thread.Sleep(100);
            SetForegroundWindow(hList);
            SetFocus(hList);
            process.WaitForInputIdle();
            SendMessage(hList, WM_KEYDOWN, VK_HOME, null);
            foreach (char chr in Encoding.Default.GetBytes(valueName))
            {
                process.WaitForInputIdle();
                SendMessage(hList, WM_CHAR, Convert.ToInt16(chr), null);
            }
            process.Dispose();
        }

        /// <summary>在Explorer中选中指定文件或文件夹</summary>
        /// <param name="filePath">文件或文件夹路径</param>
        /// <param name="moreOpen">窗口是否多开</param>
        public static void JumpExplorer(string filePath, bool moreOpen = false)
        {
            if (filePath == null) return;
            if (!moreOpen)
            {
                var pidlList = ILCreateFromPathW(filePath);
                if (pidlList == IntPtr.Zero) return;
                SHOpenFolderAndSelectItems(pidlList, 0, IntPtr.Zero, 0);
                ILFree(pidlList);
            }
            else
            {
                using var process = new Process();
                process.StartInfo.FileName = "explorer.exe";
                process.StartInfo.Arguments = $"/select, {filePath}";
                process.Start();
            }
        }

        /// <summary>在Explorer中打开指定目录</summary>
        /// <param name="dirPath">目录路径</param>
        public static void OpenDirectory(string dirPath)
        {
            if (!Directory.Exists(dirPath)) return;
            using var explorer = new Process();
            explorer.StartInfo = new ProcessStartInfo
            {
                FileName = dirPath,
                UseShellExecute = true
            };
            explorer.Start();
        }

        /// <summary>打开文件或文件夹的属性对话框</summary>
        /// <param name="filePath">文件或文件夹路径</param>
        public static bool ShowPropertiesDialog(string filePath)
        {
            var info = new SHELLEXECUTEINFO
            {
                lpVerb = "Properties",
                //显示详细信息选项卡, 此处有语言差异
                //lpParameters = ResourceString.GetDirectString("@shell32.dll,-31433"),//"详细信息",
                lpFile = filePath,
                nShow = SW_SHOW,
                fMask = SEE_MASK_INVOKEIDLIST,
                cbSize = Marshal.SizeOf(typeof(SHELLEXECUTEINFO))
            };
            return ShellExecuteEx(ref info);
        }

        /// <summary>打开指定未关联打开方式的扩展名的打开方式对话框</summary>
        /// <param name="extension">文件扩展名</param>
        public static void ShowOpenWithDialog(string extension)
        {
            //Win10 调用 SHOpenWithDialog API 或调用 OpenWith.exe -override "%1"
            //或调用 rundll32.exe shell32.dll,OpenAs_RunDLL %1 能显示打开方式对话框，但都不能设置默认应用
            //以下方法只针对未关联打开方式的扩展名显示系统打开方式对话框，对于已关联打开方式的扩展名会报错
            var tempPath = $"{Path.GetTempPath()}{Guid.NewGuid()}{extension}";
            File.WriteAllText(tempPath, "");
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = tempPath,
                    Verb = "openas"
                };
                process.Start();
            }
            File.Delete(tempPath);
        }

        /// <summary>重启Explorer</summary>
        public static void RestartExplorer()
        {
            try
            {
                // 获取所有 explorer.exe 进程
                var explorerProcesses = Process.GetProcessesByName("explorer");
                
                // 终止所有 explorer.exe 进程
                foreach (var process in explorerProcesses)
                {
                    using (process)
                    {
                        try
                        {
                            process.Kill();
                            // 等待进程完全退出，最多等待5秒
                            process.WaitForExit(5000);
                        }
                        catch (Win32Exception)
                        {
                            // 进程已退出或无法访问，继续处理下一个
                        }
                        catch (InvalidOperationException)
                        {
                            // 进程已退出，继续处理下一个
                        }
                    }
                }
                
                // 等待一小段时间确保所有进程已完全退出
                Thread.Sleep(500);
                
                // 启动新的 explorer.exe 进程
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    UseShellExecute = true
                })?.Dispose();
            }
            catch (Exception ex) when (
                ex is Win32Exception || 
                ex is InvalidOperationException || 
                ex is UnauthorizedAccessException)
            {
                // 如果上述方法失败，回退到使用 taskkill
                // 可能的原因：权限不足、进程保护等
                try
                {
                    using (var kill = Process.Start(new ProcessStartInfo
                    {
                        FileName = "taskkill.exe",
                        Arguments = "-f -im explorer.exe",
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    }))
                    {
                        kill?.WaitForExit();
                    }
                    Thread.Sleep(500);
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        UseShellExecute = true
                    })?.Dispose();
                }
                catch (Exception ex1) when (
                    ex is Win32Exception || 
                    ex is InvalidOperationException || 
                    ex is UnauthorizedAccessException)
                {
                    // 两种方法都失败，静默失败避免程序崩溃
                    // 用户会看到 explorer 没有重启，可以手动处理
                    // 在调试模式下可以通过调试器查看异常信息
                }
            }
        }

        /// <summary>调用默认浏览器打开指定网址</summary>
        /// <param name="url">网址</param>
        public static void OpenWebUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return;

            try
            {
                // 使用Uri类验证并规范化URL
                var uri = new Uri(url);
                using var process = new Process();
                // 显式设置使用Shell执行（关键修复）
                process.StartInfo.UseShellExecute = true;
                // 直接将URL作为文件名，由系统默认浏览器处理
                process.StartInfo.FileName = uri.AbsoluteUri;
                process.Start();
            }
            catch (UriFormatException)
            {
                // 处理无效URL格式的情况
                MessageBoxEx.Show("无效的网址格式：" + url, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Win32Exception ex)
            {
                // 处理打开失败的情况
                MessageBoxEx.Show("无法打开网址：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>导出指定注册表项的.reg文件</summary>
        /// <param name="regPath">注册表项路径</param>
        /// <param name="filePath">.reg文件保存路径</param>
        public static void ExportRegistry(string regPath, string filePath)
        {
            using var process = new Process();
            process.StartInfo.FileName = "regedit.exe";
            process.StartInfo.Arguments = $"/e \"{filePath}\" \"{regPath}\"";
            process.Start();
            process.WaitForExit();
        }

        /// <summary>打开记事本显示指定文本</summary>
        /// <param name="text">要显示的文本</param>
        public static void OpenNotepadWithText(string text)
        {
            using var process = Process.Start("notepad.exe");
            process.WaitForInputIdle();
            var handle = FindWindowEx(process.MainWindowHandle, IntPtr.Zero, "Edit", null);
            SendMessage(handle, WM_SETTEXT, 0, text);
        }

        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOW = 5;
        private const uint SEE_MASK_INVOKEIDLIST = 12;
        private const int WM_SETTEXT = 0xC;
        private const int WM_KEYDOWN = 0x100;
        private const int WM_CHAR = 0x102;
        private const int VK_HOME = 0x24;
        private const int VK_RIGHT = 0x27;

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int cmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetFocus(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowThreadProcessId(IntPtr hwnd, out int ID);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChild, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int uMsg, int wParam, string lParam);

        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern void ILFree(IntPtr pidlList);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern IntPtr ILCreateFromPathW(string pszPath);

        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern IntPtr SHOpenFolderAndSelectItems(IntPtr pidlList, uint cild, IntPtr children, uint dwFlags);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHELLEXECUTEINFO
        {
            public int cbSize;
            public uint fMask;
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpVerb;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpFile;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpParameters;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }
    }
}