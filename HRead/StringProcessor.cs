using System;
using System.Linq;
using System.Text;

namespace HRead
{
    public static class StringProcessor
    {
        public static string ConvertToSqlString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var lines = input.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var resultLines = lines.Select(ProcessLine);

            return string.Join("," + Environment.NewLine, resultLines);
        }

        private static string ProcessLine(string line)
        {
            var values = line.Split('\t')
                           .Select(v => v.Trim())
                           .Where(v => !string.IsNullOrEmpty(v))
                           .Select(v => $"'{v.Replace("'", "''")}'");

            return $"({string.Join(",", values)})";
        }

        public static string EncodeBase64(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            var bytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(bytes);
        }

        public static string DecodeBase64(string base64Text)
        {
            if (string.IsNullOrEmpty(base64Text))
                return string.Empty;

            try
            {
                var bytes = Convert.FromBase64String(base64Text);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return "[Invalid Base64]";
            }
        }
    }
}