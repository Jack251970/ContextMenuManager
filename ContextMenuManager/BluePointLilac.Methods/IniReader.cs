using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BluePointLilac.Methods
{
    public sealed class IniReader
    {
        public IniReader() { }

        public IniReader(StringBuilder sb)
        {
            LoadStringBuilder(sb);
        }

        public IniReader(string filePath)
        {
            LoadFile(filePath);
        }

        private readonly Dictionary<string, Dictionary<string, string>> RootDic
            = new(StringComparer.OrdinalIgnoreCase);

        public string[] Sections => RootDic.Keys.ToArray();

        public void LoadStringBuilder(StringBuilder sb)
        {
            RootDic.Clear();
            if (sb.ToString().IsNullOrWhiteSpace()) return;
            var lines = sb.ToString().Split(new[] { "\r\n", "\n" },
                StringSplitOptions.RemoveEmptyEntries).ToList();//拆分为行
            lines.ForEach(line => line.Trim());
            ReadLines(lines);
        }

        public void LoadFile(string filePath)
        {
            RootDic.Clear();
            if (!File.Exists(filePath)) return;
            var lines = new List<string>();
            using (var reader = new StreamReader(filePath, EncodingType.GetType(filePath)))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine().Trim();
                    if (line != string.Empty) lines.Add(line);
                }
            }
            ReadLines(lines);
        }

        private void ReadLines(List<string> lines)
        {
            lines.RemoveAll(
                line => line.StartsWith(";") || line.StartsWith("#")//移除注释
                || (!line.StartsWith("[") && !line.Contains("=")));//移除非section行且非key行

            if (lines.Count == 0) return;

            var indexs = new List<int> { 0 };
            for (var i = 1; i < lines.Count; i++)
            {
                if (lines[i].StartsWith("[")) indexs.Add(i);//获取section行号
            }
            indexs.Add(lines.Count);

            for (var i = 0; i < indexs.Count - 1; i++)
            {
                var section = lines[indexs[i]];
                var m = section.IndexOf(']') - 1;
                if (m < 0) continue;
                section = section.Substring(1, m);
                if (RootDic.ContainsKey(section)) continue;
                var keyValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                RootDic.Add(section, keyValues);

                for (var j = indexs[i] + 1; j < indexs[i + 1]; j++)
                {
                    var k = lines[j].IndexOf('=');
                    var key = lines[j][..k].TrimEnd();
                    var value = lines[j][(k + 1)..].TrimStart();
                    if (keyValues.ContainsKey(key)) continue;
                    keyValues.Add(key, value);
                }
            }
        }

        public string GetValue(string section, string key)
        {
            if (RootDic.TryGetValue(section, out var sectionDic))
                if (sectionDic.TryGetValue(key, out var value))
                    return value;
            return string.Empty;
        }

        public bool TryGetValue(string section, string key, out string value)
        {
            value = GetValue(section, key);
            return value != string.Empty;
        }

        public string[] GetSectionKeys(string section)
        {
            if (!RootDic.ContainsKey(section)) return null;
            else return RootDic[section].Keys.ToArray();
        }

        public bool RemoveSection(string section)
        {
            return RootDic.Remove(section);
        }

        public bool RemoveKey(string section, string key)
        {
            if (RootDic.ContainsKey(section))
            {
                return RootDic[section].Remove(key);
            }
            return false;
        }

        public void AddValue(string section, string key, string value)
        {
            if (RootDic.ContainsKey(section))
            {
                if (RootDic[section].ContainsKey(key))
                {
                    RootDic[section][key] = value;
                }
                else
                {
                    RootDic[section].Add(key, value);
                }
            }
            else
            {
                var dic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                RootDic.Add(section, dic);
                dic.Add(key, value);
            }
        }

        public void SaveFile(string filePath)
        {
            var lines = new List<string>();
            foreach (var item in RootDic)
            {
                lines.Add("[" + item.Key + "]");
                foreach (var key in item.Value)
                {
                    lines.Add(key.Key + " = " + key.Value);
                }
                lines.Add("");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            var attributes = FileAttributes.Normal;
            var encoding = Encoding.Unicode;
            if (File.Exists(filePath))
            {
                encoding = EncodingType.GetType(filePath);
                attributes = File.GetAttributes(filePath);
                File.SetAttributes(filePath, FileAttributes.Normal);
            }
            File.WriteAllLines(filePath, lines.ToArray(), encoding);
            File.SetAttributes(filePath, attributes);
        }
    }
}