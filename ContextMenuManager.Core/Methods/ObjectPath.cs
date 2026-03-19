using BluePointLilac.Methods;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ContextMenuManager.Methods
{
    internal static class ObjectPath
    {
        /// <summary>路径类型</summary>
        public enum PathType { File, Directory, Registry }
        private const string RegAppPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths";
        private const string ShellExecuteCommand = "mshta vbscript:createobject(\"shell.application\").shellexecute(\"";
        private const string PowerShellCommandPrefix = "powershell -WindowStyle Hidden -Command \"Start-Process";


        private static readonly char[] IllegalChars = { '/', '*', '?', '\"', '<', '>', '|' };
        private static readonly List<string> IgnoreCommandParts = new() { "", "%1", "%v" };

        /// <summary>根据文件名获取完整的文件路径</summary>
        /// <remarks>fileName为Win+R、注册表等可直接使用的文件名</remarks>
        /// <param name="fileName">文件名</param>
        /// <returns>成功提取返回true, fullPath为现有文件路径; 否则返回false, fullPath为null</returns>
        public static bool GetFullFilePath(string fileName, out string fullPath)
        {
            fullPath = null;
            if (fileName.IsNullOrWhiteSpace()) return false;

            foreach (var name in new[] { fileName, $"{fileName}.exe" })
            {
                //右键菜单仅支持%SystemRoot%\System32和%SystemRoot%两个环境变量，不考虑其他系统环境变量和用户环境变量，和Win+R命令有区别
                foreach (var dir in new[] { "", @"%SystemRoot%\System32\", @"%SystemRoot%\" })
                {
                    if (dir != "" && (name.Contains('\\') || name.Contains(':'))) return false;
                    fullPath = Environment.ExpandEnvironmentVariables($@"{dir}{name}");
                    if (File.Exists(fullPath)) return true;
                }

                fullPath = Registry.GetValue($@"{RegAppPath}\{name}", "", null)?.ToString();
                if (File.Exists(fullPath)) return true;
            }
            fullPath = null;
            return false;
        }


        public static readonly Dictionary<string, string> FilePathDic = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>从包含现有文件路径的命令语句中提取文件路径</summary>
        /// <param name="command">命令语句</param>
        /// <returns>成功提取返回现有文件路径，否则返回值为null</returns>
        public static string ExtractFilePath(string command)
        {
            if (command.IsNullOrWhiteSpace()) return null;
            if (FilePathDic.ContainsKey(command)) return FilePathDic[command];
            else
            {
                string filePath = null;
                var partCmd = Environment.ExpandEnvironmentVariables(command).Replace(@"\\", @"\");
                if (partCmd.StartsWith(ShellExecuteCommand, StringComparison.OrdinalIgnoreCase))
                {
                    partCmd = partCmd[ShellExecuteCommand.Length..];
                    var arr = partCmd.Split(new[] { "\",\"" }, StringSplitOptions.None);
                    if (arr.Length > 0)
                    {
                        var fileName = arr[0];
                        if (GetFullFilePath(fileName, out filePath))
                        {
                            FilePathDic.Add(command, filePath);
                            return filePath;
                        }
                        if (arr.Length > 1)
                        {
                            var arguments = arr[1];
                            filePath = ExtractFilePath(arguments);
                            if (filePath != null) return filePath;
                        }
                    }
                }
                else if (partCmd.StartsWith(PowerShellCommandPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    int idx = partCmd.IndexOf("-FilePath '", StringComparison.OrdinalIgnoreCase);
                    if (idx != -1)
                    {
                        int start = idx + 11;
                        int end = partCmd.IndexOf("'", start);
                        if (end != -1)
                        {
                            var fileName = partCmd.Substring(start, end - start).Replace("''", "'");
                            if (GetFullFilePath(fileName, out filePath))
                            {
                                FilePathDic.Add(command, filePath);
                                return filePath;
                            }
                        }
                    }
                }

                var strs = Array.FindAll(partCmd.Split(IllegalChars), str
                    => IgnoreCommandParts.Any(part => !part.Equals(str.Trim()))).Reverse().ToArray();

                foreach (var str1 in strs)
                {
                    var str2 = str1;
                    var index = -1;
                    do
                    {
                        var paths = new List<string>();
                        var path1 = str2[(index + 1)..];
                        paths.Add(path1);
                        if (index > 0)
                        {
                            var path2 = str2[..index];
                            paths.Add(path2);
                        }
                        var count = paths.Count;
                        for (var i = 0; i < count; i++)
                        {
                            foreach (var c in new[] { ',', '-' })
                            {
                                if (paths[i].Contains(c)) paths.AddRange(paths[i].Split(c));
                            }
                        }
                        foreach (var path in paths)
                        {
                            if (GetFullFilePath(path, out filePath))
                            {
                                FilePathDic.Add(command, filePath);
                                return filePath;
                            }
                        }
                        str2 = path1;
                        index = str2.IndexOf(' ');
                    }
                    while (index != -1);
                }
                FilePathDic.Add(command, null);
                return null;
            }
        }

        /// <summary>移除文件或文件夹名称中的非法字符</summary>
        /// <param name="fileName">文件或文件夹名称</param>
        /// <returns>返回移除非法字符后的文件或文件夹名称</returns>
        public static string RemoveIllegalChars(string fileName)
        {
            Array.ForEach(IllegalChars, c => fileName = fileName.Replace(c.ToString(), ""));
            return fileName.Replace("\\", "").Replace(":", "");
        }

        /// <summary>判断文件或文件夹或注册表项是否存在</summary>
        /// <param name="path">文件或文件夹或注册表项路径</param>
        /// <param name="type">路径类型</param>
        /// <returns>目标路径存在返回true，否则返回false</returns>
        public static bool ObjectPathExist(string path, PathType type)
        {
            return type switch
            {
                PathType.File => File.Exists(path),
                PathType.Directory => Directory.Exists(path),
                PathType.Registry => RegistryEx.GetRegistryKey(path) != null,
                _ => false,
            };
        }

        /// <summary>获取带序号的新路径</summary>
        /// <param name="oldPath">目标路径</param>
        /// <param name="type">路径类型</param>
        /// <returns>如果目标路径不存在则返回目标路径，否则返回带序号的新路径</returns>
        public static string GetNewPathWithIndex(string oldPath, PathType type, int startIndex = -1)
        {
            string newPath;
            var dirPath = type == PathType.Registry ? RegistryEx.GetParentPath(oldPath) : Path.GetDirectoryName(oldPath);
            var name = type == PathType.Registry ? RegistryEx.GetKeyName(oldPath) : Path.GetFileNameWithoutExtension(oldPath);
            var extension = type == PathType.Registry ? "" : Path.GetExtension(oldPath);

            do
            {
                newPath = $@"{dirPath}\{name}";
                if (startIndex > -1) newPath += startIndex;
                newPath += extension;
                startIndex++;
            } while (ObjectPathExist(newPath, type));
            return newPath;
        }
    }
}