using System;
using System.Collections.Generic;
using System.Text;

namespace GXLightBrowser
{
    internal sealed class PasswordCsvImportResult
    {
        public List<PasswordVaultEntry> Entries { get; private set; }
        public int SkippedRows { get; set; }

        public PasswordCsvImportResult()
        {
            Entries = new List<PasswordVaultEntry>();
        }
    }

    internal static class PasswordCsvImporter
    {
        public static PasswordCsvImportResult Parse(string csv)
        {
            PasswordCsvImportResult result = new PasswordCsvImportResult();
            List<string[]> rows = ParseRows(csv ?? string.Empty);
            if (rows.Count == 0)
            {
                return result;
            }

            int start = 0;
            Dictionary<string, int> map = HeaderMap(rows[0]);
            if (map.ContainsKey("url") && map.ContainsKey("password"))
            {
                start = 1;
            }
            else
            {
                map["name"] = 0;
                map["url"] = 1;
                map["username"] = 2;
                map["password"] = 3;
                map["note"] = 4;
            }

            for (int i = start; i < rows.Count; i++)
            {
                string[] row = rows[i];
                string url = Value(row, map, "url");
                string password = Value(row, map, "password");
                if (string.IsNullOrWhiteSpace(url) || string.IsNullOrEmpty(password))
                {
                    result.SkippedRows++;
                    continue;
                }

                PasswordVaultEntry entry = new PasswordVaultEntry();
                entry.Url = url.Trim();
                entry.Username = Value(row, map, "username").Trim();
                entry.Name = Value(row, map, "name").Trim();
                if (string.IsNullOrWhiteSpace(entry.Name))
                {
                    entry.Name = HostName(entry.Url);
                }
                entry.Note = Value(row, map, "note");
                entry.ImportedUtc = DateTime.UtcNow;
                entry.SetPassword(password);
                result.Entries.Add(entry);
            }

            return result;
        }

        private static Dictionary<string, int> HeaderMap(string[] header)
        {
            Dictionary<string, int> map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < header.Length; i++)
            {
                string key = NormalizeHeader(header[i]);
                if ((key == "name" || key == "url" || key == "username" || key == "password" || key == "note") &&
                    !map.ContainsKey(key))
                {
                    map[key] = i;
                }
            }
            return map;
        }

        private static string NormalizeHeader(string value)
        {
            string key = (value ?? string.Empty).Trim().TrimStart('\uFEFF').ToLowerInvariant();
            key = key.Replace(" ", "").Replace("-", "").Replace("_", "");
            switch (key)
            {
                case "name":
                case "title":
                case "nombre":
                    return "name";
                case "url":
                case "originurl":
                case "actionurl":
                case "loginuri":
                case "website":
                case "sitio":
                    return "url";
                case "username":
                case "usernamevalue":
                case "user":
                case "login":
                case "email":
                case "correo":
                case "usuario":
                    return "username";
                case "password":
                case "passwordvalue":
                case "pass":
                case "contrasena":
                case "contraseña":
                    return "password";
                case "note":
                case "notes":
                case "comentario":
                case "comentarios":
                    return "note";
                default:
                    return key;
            }
        }

        private static string Value(string[] row, Dictionary<string, int> map, string key)
        {
            int index;
            if (!map.TryGetValue(key, out index) || index < 0 || index >= row.Length)
            {
                return string.Empty;
            }
            return row[index] ?? string.Empty;
        }

        private static string HostName(string url)
        {
            Uri uri;
            return Uri.TryCreate(url, UriKind.Absolute, out uri) && !string.IsNullOrWhiteSpace(uri.Host)
                ? uri.Host
                : "Imported";
        }

        private static List<string[]> ParseRows(string csv)
        {
            List<string[]> rows = new List<string[]>();
            List<string> row = new List<string>();
            StringBuilder current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < csv.Length; i++)
            {
                char ch = csv[i];
                if (ch == '"')
                {
                    if (inQuotes && i + 1 < csv.Length && csv[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                    continue;
                }

                if (ch == ',' && !inQuotes)
                {
                    row.Add(current.ToString());
                    current.Length = 0;
                    continue;
                }

                if ((ch == '\r' || ch == '\n') && !inQuotes)
                {
                    if (ch == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n')
                    {
                        i++;
                    }
                    row.Add(current.ToString());
                    AddRow(rows, row);
                    row = new List<string>();
                    current.Length = 0;
                    continue;
                }

                current.Append(ch);
            }

            row.Add(current.ToString());
            AddRow(rows, row);
            return rows;
        }

        private static void AddRow(List<string[]> rows, List<string> row)
        {
            bool hasValue = false;
            for (int i = 0; i < row.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(row[i]))
                {
                    hasValue = true;
                    break;
                }
            }
            if (hasValue)
            {
                rows.Add(row.ToArray());
            }
        }
    }
}
