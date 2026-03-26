using System;
using System.Drawing;
using System.IO;

namespace ContextMenuManager.Properties
{
    internal static class AppResources
    {
        private static string BasePath => AppDomain.CurrentDomain.BaseDirectory;
        private static string ResourcesPath => Path.Combine(BasePath, "Properties", "Resources");
        private static string ImagesPath => Path.Combine(ResourcesPath, "Images");
        private static string TextsPath => Path.Combine(ResourcesPath, "Texts");

        private static Image GetImage(string name)
        {
            var path = Path.Combine(ImagesPath, name + ".png");
            if (File.Exists(path)) return Image.FromFile(path);

            // Try to find it in the project structure if not found in base directory (for debug/dev)
            var devPath = Path.Combine(BasePath, "..", "..", "..", "Properties", "Resources", "Images", name + ".png");
            if (File.Exists(devPath)) return Image.FromFile(devPath);

            return null;
        }

        private static string GetText(string name, string extension = ".xml")
        {
            var path = Path.Combine(TextsPath, name + extension);
            if (File.Exists(path)) return File.ReadAllText(path);

            var devPath = Path.Combine(BasePath, "..", "..", "..", "Properties", "Resources", "Texts", name + extension);
            if (File.Exists(devPath)) return File.ReadAllText(devPath);

            return string.Empty;
        }

        public static Image Add => GetImage("Add");
        public static Image AddExisting => GetImage("AddExisting");
        public static Image AddSeparator => GetImage("AddSeparator");
        public static Image BackupItem => GetImage("BackupItem");
        public static Image ContextMenuStyle => GetImage("ContextMenuStyle");
        public static Image Custom => GetImage("Custom");
        public static Image Delete => GetImage("Delete");
        public static Image Donate => GetImage("Donate");
        public static Image Down => GetImage("Down");
        public static Image DownLoad => GetImage("DownLoad");
        public static Image Enhance => GetImage("Enhance");
        public static Image Jump => GetImage("Jump");
        public static Bitmap Logo => (Bitmap)GetImage("Logo");
        public static Image MicrosoftStore => GetImage("MicrosoftStore");
        public static Image NewFolder => GetImage("NewFolder");
        public static Image NewItem => GetImage("NewItem");
        public static Image Open => GetImage("Open");
        public static Image Select => GetImage("Select");
        public static Image Setting => GetImage("Setting");
        public static Image Sort => GetImage("Sort");
        public static Image SubItems => GetImage("SubItems");
        public static Image Translate => GetImage("Translate");
        public static Image Up => GetImage("Up");
        public static Image User => GetImage("User");
        public static Image Web => GetImage("Web");

        public static string AppLanguageDic => GetText("AppLanguageDic", ".ini");
        public static string GuidInfosDic => GetText("GuidInfosDic", ".ini");
        public static string DetailedEditDic => GetText("DetailedEditDic");
        public static string EnhanceMenusDic => GetText("EnhanceMenusDic");
        public static string UwpModeItemsDic => GetText("UwpModeItemsDic");
    }
}
