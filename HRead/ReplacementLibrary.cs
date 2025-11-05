using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;

namespace HRead
{
    public class ReplacementLibrary
    {
        public Dictionary<string, ReplacementItem> Items = new Dictionary<string, ReplacementItem>();

        private string baseDir;

        public ReplacementLibrary(string baseDirectory)
        {
            baseDir = baseDirectory;
            LoadAll();
        }

        private void LoadAll()
        {
            string textFile = Path.Combine(baseDir, "text.txt");
            string fileDir = Path.Combine(baseDir, "file");
            string imgDir = Path.Combine(baseDir, "img");

            // --- 1. text.txt ---
            if (File.Exists(textFile))
            {
                foreach (string line in File.ReadAllLines(textFile))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(new char[] { ':' }, 2);

                    if (parts.Length == 2)
                    {
                        string key = "@" + parts[0].Trim();
                        string value = parts[1].Trim();
                        AddItem(key, "text", value);
                    }
                }
            }

            // --- 2. file\ ---
            if (Directory.Exists(fileDir))
            {
                foreach (var f in Directory.GetFiles(fileDir))
                {
                    string key = "@" + Path.GetFileNameWithoutExtension(f);
                    string value = File.ReadAllText(f);
                    AddItem(key, "file", value);
                }
            }

            // --- 3. img\ ---
            if (Directory.Exists(imgDir))
            {
                foreach (var f in Directory.GetFiles(imgDir))
                {
                    string key = "@" + Path.GetFileNameWithoutExtension(f);
                    AddItem(key, "image", f); // chỉ lưu path, khi cần thì load ảnh
                }
            }
        }

        private void AddItem(string key, string type, string value)
        {
            if (!Items.ContainsKey(key))
            {
                Items[key] = new ReplacementItem { Key = key, Type = type, Value = value };
            }
        }

        public ReplacementItem Find(string key)
        {
            Items.TryGetValue(key, out var item);
            return item;
        }
        public string ExportAsText()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var kv in Items)
            {
                var item = kv.Value;
                string displayValue;

                if (item.Type == "image" || item.Type == "file")
                {
                    displayValue = $"[FILE]";
                }
                else
                {
                    // Text và file: cắt ngắn nếu quá dài
                    displayValue = item.Value.Length > 100
                        ? item.Value.Substring(0, 100) + "..."
                        : item.Value;
                }

                sb.AppendLine($"{item.Key}: {displayValue}");
            }
            return sb.ToString();
        }
    }

    public class ReplacementItem
    {
        public string Key { get; set; }
        public string Type { get; set; } // "text", "file", "image"
        public string Value { get; set; } // text hoặc path hoặc base64 image
    }
}
