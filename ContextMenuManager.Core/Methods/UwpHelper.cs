using BluePointLilac.Methods;
using Microsoft.Win32;
using System;
using System.IO;

namespace ContextMenuManager.Methods
{
    internal static class UwpHelper
    {
        private const string PackageRegPath = @"HKEY_CLASSES_ROOT\PackagedCom\Package";
        private const string PackagesRegPath = @"HKEY_CLASSES_ROOT\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\PackageRepository\Packages";

        public static string GetPackageName(string uwpName)
        {
            if (string.IsNullOrEmpty(uwpName)) return null;
            using var packageKey = RegistryEx.GetRegistryKey(PackageRegPath);
            if (packageKey == null) return null;
            foreach (var packageName in packageKey.GetSubKeyNames())
            {
                if (packageName.StartsWith(uwpName, StringComparison.OrdinalIgnoreCase))
                {
                    return packageName;
                }
            }
            return null;
        }

        public static string GetRegPath(string uwpName, Guid guid)
        {
            var packageName = GetPackageName(uwpName);
            if (packageName == null) return null;
            else return $@"{PackageRegPath}\{packageName}\Class\{guid:B}";
        }

        public static string GetFilePath(string uwpName, Guid guid)
        {
            var regPath = GetRegPath(uwpName, guid);
            if (regPath == null) return null;
            var packageName = GetPackageName(uwpName);
            using var pKey = RegistryEx.GetRegistryKey($@"{PackagesRegPath}\{packageName}");
            if (pKey == null) return null;
            var dirPath = pKey.GetValue("Path")?.ToString();
            var dllPath = Registry.GetValue(regPath, "DllPath", null)?.ToString();
            var filePath = $@"{dirPath}\{dllPath}";
            if (File.Exists(filePath)) return filePath;
            var names = pKey.GetSubKeyNames();
            if (names.Length == 1)
            {
                filePath = "shell:AppsFolder\\" + names[0];
                return filePath;
            }
            if (Directory.Exists(dirPath)) return dirPath;
            return null;
        }
    }
}