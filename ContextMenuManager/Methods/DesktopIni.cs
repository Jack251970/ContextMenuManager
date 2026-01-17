using BluePointLilac.Methods;
using System.IO;

namespace ContextMenuManager.Methods
{
    internal static class DesktopIni
    {
        private const string LocalizedFileNames = "LocalizedFileNames";

        public static string GetDesktopIniPath(string filePath)
        {
            return $@"{Path.GetDirectoryName(filePath)}\desktop.ini";
        }

        public static void DeleteLocalizedFileNames(string filePath)
        {
            var writer = new IniWriter(GetDesktopIniPath(filePath));
            var fileName = Path.GetFileName(filePath);
            writer.DeleteKey(LocalizedFileNames, fileName);
        }

        public static void SetLocalizedFileNames(string filePath, string name)
        {
            var writer = new IniWriter(GetDesktopIniPath(filePath));
            var fileName = Path.GetFileName(filePath);
            writer.SetValue(LocalizedFileNames, fileName, name);
        }

        public static string GetLocalizedFileNames(string filePath, bool translate = false)
        {
            var writer = new IniWriter(GetDesktopIniPath(filePath));
            var fileName = Path.GetFileName(filePath);
            var name = writer.GetValue(LocalizedFileNames, fileName);
            if (translate) name = ResourceString.GetDirectString(name);
            return name;
        }
    }
}