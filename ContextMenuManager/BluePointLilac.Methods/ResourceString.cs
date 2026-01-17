using System;
using System.Runtime.InteropServices;
using System.Text;

namespace BluePointLilac.Methods
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

        /// <summary>获取格式为"@[filename],-[strID]"或"@{[packageName]?ms-resource://[resPath]}"的直接字符串，如果失败则返回fallback值</summary>
        /// <param name="resStr">要转换的字符串</param>
        /// <param name="fallback">当资源字符串解析失败时返回的备用值</param>
        /// <returns>成功时返回解析的字符串，失败时返回fallback值</returns>
        private static string GetDirectStringWithFallback(string resStr, string fallback)
        {
            var result = GetDirectString(resStr);
            return string.IsNullOrEmpty(result) ? fallback : result;
        }

        // OK和Cancel按钮文本，使用AppString作为Fallback将在InputDialog中处理
        public static string OK = GetDirectString("@shell32.dll,-9752");
        public static string Cancel = GetDirectString("@shell32.dll,-9751");

        /// <summary>设置OK和Cancel的fallback值，当系统资源不可用时使用</summary>
        /// <param name="okText">OK按钮的fallback文本</param>
        /// <param name="cancelText">Cancel按钮的fallback文本</param>
        public static void SetButtonTextFallbacks(string okText, string cancelText)
        {
            if (string.IsNullOrEmpty(OK)) OK = okText;
            if (string.IsNullOrEmpty(Cancel)) Cancel = cancelText;
        }
    }
}