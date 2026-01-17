using System;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BluePointLilac.Controls
{
    public sealed class UAWebClient : IDisposable
    {
        private static readonly HttpClient client;

        static UAWebClient()
        {
            client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(6);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.212 Safari/537.36 Edg/90.0.818.66");
        }

        public void Dispose() { }

        /// <summary>获取网页文本</summary>
        public async Task<string> GetWebStringAsync(string url)
        {
            try
            {
                var str = await client.GetStringAsync(url);
                str = str?.Replace("\n", Environment.NewLine);//换行符转换
                return str;
            }
            catch { return null; }
        }

        /// <summary>将网络文本写入本地文件</summary>
        /// <param name="filePath">本地文件路径</param>
        /// <param name="fileUrl">网络文件Raw路径</param>
        public async Task<bool> WebStringToFileAsync(string filePath, string fileUrl)
        {
            var contents = await GetWebStringAsync(fileUrl);
            var flag = contents != null;
            if (flag) await File.WriteAllTextAsync(filePath, contents, Encoding.Unicode);
            return flag;
        }

        /// <summary>获取网页Json文本并加工为Xml</summary>
        public async Task<XmlDocument> GetWebJsonToXmlAsync(string url)
        {
            try
            {
                var bytes = await client.GetByteArrayAsync(url);
                using XmlReader xReader = JsonReaderWriterFactory.CreateJsonReader(bytes, XmlDictionaryReaderQuotas.Max);
                var doc = new XmlDocument();
                doc.Load(xReader);
                return doc;
            }
            catch { return null; }
        }

        public async Task<byte[]> GetWebDataAsync(string url)
        {
            try
            {
                return await client.GetByteArrayAsync(url);
            }
            catch { return null; }
        }
    }
}
