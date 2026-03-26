using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Packaging;
using System.Windows;
using System.Windows.Resources;

namespace ContextMenuManager.Properties
{
    internal static class AppResources
    {
        private static readonly string AssemblyName = typeof(AppResources).Assembly.GetName().Name;
        private static readonly Dictionary<string, Image> ImageCache = [];
        private static readonly Dictionary<string, string> TextCache = [];

        static AppResources()
        {
            // 确保 pack:// 协议已注册
            if (!UriParser.IsKnownScheme("pack"))
            {
                _ = PackUriHelper.UriSchemePack;
            }
        }

        private static StreamResourceInfo GetStreamInfo(string path)
        {
            try
            {
                // 尝试原始路径
                var uri = new Uri(path, UriKind.RelativeOrAbsolute);
                var info = Application.GetResourceStream(uri);
                if (info != null) return info;

                // 如果是绝对路径且包含 ;component/，尝试提取相对路径并重试
                if (uri.IsAbsoluteUri && path.Contains(";component/"))
                {
                    var relativePath = path.Substring(path.IndexOf(";component/") + 11);
                    info = Application.GetResourceStream(new Uri(relativePath, UriKind.Relative));
                    if (info != null) return info;

                    info = Application.GetResourceStream(new Uri("/" + relativePath.TrimStart('/'), UriKind.Relative));
                    if (info != null) return info;
                }

                // 尝试使用正确的程序集名称构建 pack URI
                if (!path.StartsWith("pack://"))
                {
                    var newPath = $"pack://application:,,,/{AssemblyName};component/{path.TrimStart('/')}";
                    info = Application.GetResourceStream(new Uri(newPath, UriKind.Absolute));
                    if (info != null) return info;
                }
            }
            catch { }
            return null;
        }

        private static Image GetImage(string path)
        {
            if (ImageCache.TryGetValue(path, out var image)) return image;
            var info = GetStreamInfo(path);
            if (info == null) return null;
            using var stream = info.Stream;
            // 复制到内存流，防止原始流被释放导致 Bitmap 异常，同时也提高兼容性
            var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;
            var bitmap = new Bitmap(ms);
            ImageCache[path] = bitmap;
            return bitmap;
        }

        private static string GetText(string path)
        {
            if (TextCache.TryGetValue(path, out var text)) return text;
            var info = GetStreamInfo(path);
            if (info == null) return string.Empty;
            using var stream = info.Stream;
            using var reader = new StreamReader(stream);
            text = reader.ReadToEnd();
            TextCache[path] = text;
            return text;
        }

        public static Image Add => GetImage("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Images/Add.png");
        public static Image AddExisting => GetImage("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Images/AddExisting.png");
        public static Image AddSeparator => GetImage("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Images/AddSeparator.png");
        public static Image BackupItem => GetImage("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Images/BackupItem.png");
        public static Image ContextMenuStyle => GetImage("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Images/ContextMenuStyle.png");
        public static Image Custom => GetImage("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Images/Custom.png");
        public static Image Delete => GetImage("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Images/Delete.png");
        public static Image Donate => GetImage("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Images/Donate.png");
        public static Image Down => GetImage("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Images/Down.png");
        public static Image DownLoad => GetImage("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Images/DownLoad.png");
        public static Image Enhance => GetImage("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Images/Enhance.png");
        public static Image Jump => GetImage("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Images/Jump.png");
        public static Image Logo => GetImage("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Images/Logo.png");
        public static Image MicrosoftStore => GetImage("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Images/MicrosoftStore.png");
        public static Image NewFolder => GetImage("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Images/NewFolder.png");
        public static Image NewItem => GetImage("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Images/NewItem.png");
        public static Image Open => GetImage("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Images/Open.png");
        public static Image Select => GetImage("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Images/Select.png");
        public static Image Setting => GetImage("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Images/Setting.png");
        public static Image Sort => GetImage("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Images/Sort.png");
        public static Image SubItems => GetImage("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Images/SubItems.png");
        public static Image Translate => GetImage("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Images/Translate.png");
        public static Image Up => GetImage("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Images/Up.png");
        public static Image User => GetImage("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Images/User.png");
        public static Image Web => GetImage("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Images/Web.png");

        public static string AppLanguageDic => GetText("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Texts/AppLanguageDic.ini");
        public static string GuidInfosDic => GetText("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Texts/GuidInfosDic.ini");
        public static string DetailedEditDic => GetText("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Texts/DetailedEditDic.xml");
        public static string EnhanceMenusDic => GetText("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Texts/EnhanceMenusDic.xml");
        public static string UwpModeItemsDic => GetText("pack://application:,,,/ContextMenuManager;component/Properties/Resources/Texts/UwpModeItemsDic.xml");
    }
}
