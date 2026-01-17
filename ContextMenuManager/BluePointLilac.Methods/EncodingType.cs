using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BluePointLilac.Methods
{
    /* 获取文本文件编码类型
     * 代码参考：https://www.cnblogs.com/guyun/p/4262587.html (Napoléon)*/
    public static class EncodingType
    {
        /// <summary>各种带BOM的编码BOM值</summary>
        private static readonly Dictionary<byte[], Encoding> EncodingBomBytes = new()
        {
            { new byte[] { 0xEF, 0xBB, 0xBF }, Encoding.UTF8 },                         //UTF-8         EF BB BF
            { new byte[] { 0xFF, 0xFE, 0x00, 0x00 }, Encoding.UTF32 },                  //UTF-32LE      FF FE 00 00
            { new byte[] { 0xFF, 0xFE }, Encoding.Unicode },                            //UTF-16LE      FF FE
            { new byte[] { 0xFE, 0xFF }, Encoding.BigEndianUnicode },                   //UTF-16BE      FE FF
            { new byte[] { 0x2B, 0x2F, 0x76 }, Encoding.UTF8 },                         //UTF-7         2B 2F 76
            { new byte[] { 0x00, 0x00, 0xFE, 0xFF }, new UTF32Encoding(true, true) },   //UTF-32BE      00 00 FE FF
        };

        /// <summary>获取给定的文件的编码类型</summary> 
        /// <param name=“filePath“>文件路径</param> 
        /// <returns>文件的编码类型</returns> 
        public static Encoding GetType(string filePath)
        {
            var fs = File.ReadAllBytes(filePath);
            foreach (var kv in EncodingBomBytes)
            {
                if (fs.Length < kv.Key.Length) continue;
                var i = -1;
                var flag = kv.Key.All(s => { i++; return s == fs[i]; });
                if (flag) return kv.Value;
            }
            if (IsUTF8Bytes(fs)) return Encoding.UTF8; //不带BOM的UTF-8
            return Encoding.Default;
        }

        /// <summary>判断是否是不带 BOM 的 UTF8 格式</summary> 
        /// <param name=“bytes“></param> 
        private static bool IsUTF8Bytes(byte[] bytes)
        {
            var count = 1; //计算当前正分析的字符应还有的字节数 
            for (var i = 0; i < bytes.Length; i++)
            {
                var curByte = bytes[i];//当前分析的字节. 
                if (count == 1)
                {
                    if (curByte >= 0x80)
                    {
                        //计算当前字符所占的字节数
                        //修复了EncodingType内造成en语言下debug错误的问题（算术溢出）
                        var byteCount = 7;
                        if (curByte < 0xC0)
                        {
                            byteCount = 0;
                        }
                        else if (curByte < 0xE0)
                        {
                            byteCount = 1;
                        }
                        else if (curByte < 0xF0)
                        {
                            byteCount = 2;
                        }
                        else if (curByte < 0xF8)
                        {
                            byteCount = 3;
                        }
                        else if (curByte < 0xFC)
                        {
                            byteCount = 4;
                        }
                        else if (curByte < 0xFE)
                        {
                            byteCount = 5;
                        }
                        else if (curByte < 0xFF)
                        {
                            byteCount = 6;
                        }
                        count += byteCount;
                        /*while (((curByte <<= 1) & 0x80) != 0)
                        {
                            count++;
                        }*/
                        //标记位首位若为非0 则至少以2个1开始 如:110XXXXX...........1111110X 
                        if (count is 1 or > 6) return false;
                    }
                }
                else
                {
                    //若是UTF-8 此时第一位必须为1 
                    if ((curByte & 0xC0) != 0x80) return false;
                    else count--;
                }
            }
            //if(count > 1) throw new Exception("非预期的byte格式");
            return true;
        }
    }
}