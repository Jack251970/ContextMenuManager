using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ContextMenuManager.Methods
{
    public static class ResourceString
    {
        //MSDN文档: https://docs.microsoft.com/windows/win32/api/shlwapi/nf-shlwapi-shloadindirectstring
        //提取.pri文件资源: https://docs.microsoft.com/windows/uwp/app-resources/makepri-exe-command-options
        //.pri转储.xml资源列表: MakePri.exe dump /if [priPath] /of [xmlPath]

        [DllImport("shlwapi.dll", BestFitMapping = false, CharSet = CharSet.Unicode,
            ExactSpelling = true, SetLastError = false, ThrowOnUnmappableChar = true)]
        private static extern int SHLoadIndirectString(string pszSource, StringBuilder pszOutBuf, uint cchOutBuf, IntPtr ppvReserved);

        /// <summary>获取格式为"@[filename],-[strID]"或"@{[packageName]?ms-resource://[resPath]}"的直接字符串</summary>
        /// <param name="resStr">要转换的字符串</param>
        /// <returns>resStr为Null时返回值为string.Empty; resStr首字符为@但解析失败时返回string.Empty</returns>
        /// <remarks>[fileName]:文件路径; [strID]:字符串资源索引; [packageName]:UWP带版本号包名; [resPath]:pri资源路径</remarks>
        public static string GetDirectString(string resStr)
        {
            var outBuff = new StringBuilder(1024);
            SHLoadIndirectString(resStr, outBuff, 1024, IntPtr.Zero);
            return outBuff.ToString();
        }

        /// <summary>Strips Win32 menu access-key markers so the list matches what Explorer actually renders.</summary>
        /// <param name="text">The raw menu text, typically read from the registry.</param>
        /// <returns>The input unchanged when the setting is off or no '&amp;' is present; otherwise the text with single '&amp;' removed and '&amp;&amp;' collapsed to a literal '&amp;'.</returns>
        /// <remarks>Leaving the setting off preserves the raw registry text for power users who want to see the mnemonic markers.</remarks>
        public static string StripMnemonics(string text)
        {
            if (string.IsNullOrEmpty(text) || !AppConfig.StripMenuMnemonics) return text;
            if (text.IndexOf('&') < 0) return text;

            var sb = new StringBuilder(text.Length);
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == '&')
                {
                    if (i + 1 < text.Length && text[i + 1] == '&')
                    {
                        sb.Append('&');
                        i++;
                    }
                }
                else
                {
                    sb.Append(text[i]);
                }
            }
            return sb.ToString();
        }
    }
}
