using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

var dbPath = args.Length > 0 ? args[0] : "src/Project1.Web/project1_dev.db";
var root = Directory.GetCurrentDirectory();

if (!File.Exists(dbPath))
{
    // attempt to create the database file
    try
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        using(var fs = File.Create(dbPath)) {}
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Failed to create database file '{dbPath}': {ex.Message}");
        Environment.Exit(2);
    }
}

var connString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();

using var conn = new SqliteConnection(connString);
conn.Open();

using var cmd = conn.CreateCommand();
cmd.CommandText = @"CREATE TABLE IF NOT EXISTS CnsMappings (
    CnsCode TEXT PRIMARY KEY,
    UnicodeChar TEXT
);";
cmd.ExecuteNonQuery();

cmd.CommandText = @"CREATE TABLE IF NOT EXISTS CnsProperties (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CnsCode TEXT,
    PropertyName TEXT,
    PropertyValue TEXT
);";
cmd.ExecuteNonQuery();

var mappingFolders = new[] { Path.Combine(root, "src", "MapingTables"), Path.Combine(root, "src", "MappingTables") };
var propertiesFolders = new[] { Path.Combine(root, "src", "Properties"), Path.Combine(root, "Properties") };

string NormalizeCns(string token)
{
    if (string.IsNullOrWhiteSpace(token)) return string.Empty;
    var t = token.Trim();
    t = t.Trim('"','\'','\r','\n');
    return t.ToUpperInvariant();
}

string? ParseUnicodeFromToken(string token)
{
    if (string.IsNullOrWhiteSpace(token)) return null;
    var t = token.Trim();
    if (t.StartsWith("U+", StringComparison.OrdinalIgnoreCase)) t = t.Substring(2);
    if (t.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) t = t.Substring(2);

    if (t.Any(c => char.GetUnicodeCategory(c) == UnicodeCategory.OtherLetter || (c >= 0x4E00 && c <= 0x9FFF)))
    {
        return t[0].ToString();
    }

    var hexRegex = new Regex("^[0-9A-Fa-f]{4,6}$");
    if (hexRegex.IsMatch(t))
    {
        try
        {
            var code = int.Parse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return char.ConvertFromUtf32(code);
        }
        catch { return null; }
    }

    return null;
}

string[] SmartSplit(string line)
{
    if (line.Contains('\t')) return line.Split('\t', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
    if (line.Contains(',')) return line.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
    if (line.Contains(' ')) return line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
    if (line.Contains('=')) return line.Split('=', 2).Select(s => s.Trim()).ToArray();
    return new[] { line.Trim() };
}

int importedMappings = 0;
foreach (var folder in mappingFolders)
{
    if (!Directory.Exists(folder)) continue;
    foreach (var file in Directory.EnumerateFiles(folder, "*.txt", SearchOption.AllDirectories))
    {
        using var sr = new StreamReader(file);
        string? line;
        while ((line = sr.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = SmartSplit(line);
            if (parts.Length < 2) continue;

            var left = parts[0];
            var right = parts[1];

            var cns = NormalizeCns(left);
            if (string.IsNullOrEmpty(cns)) continue;

            var unicodeChar = ParseUnicodeFromToken(right);
            if (string.IsNullOrEmpty(unicodeChar))
            {
                var maybeCns = NormalizeCns(right);
                var maybeUnicode = ParseUnicodeFromToken(left);
                if (!string.IsNullOrEmpty(maybeCns) && !string.IsNullOrEmpty(maybeUnicode))
                {
                    cns = maybeCns;
                    unicodeChar = maybeUnicode;
                }
                else continue;
            }

            using var ins = conn.CreateCommand();
            ins.CommandText = "INSERT OR REPLACE INTO CnsMappings (CnsCode, UnicodeChar) VALUES ($cns, $u)";
            ins.Parameters.AddWithValue("$cns", cns);
            ins.Parameters.AddWithValue("$u", unicodeChar);
            ins.ExecuteNonQuery();
            importedMappings++;
        }
    }
}

int importedProps = 0;
foreach (var folder in propertiesFolders)
{
    if (!Directory.Exists(folder)) continue;
    foreach (var file in Directory.EnumerateFiles(folder, "*.txt", SearchOption.TopDirectoryOnly))
    {
        var propName = Path.GetFileNameWithoutExtension(file);
        using var sr = new StreamReader(file);
        string? line;
        while ((line = sr.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = SmartSplit(line);
            if (parts.Length < 2) continue;
            var cns = NormalizeCns(parts[0]);
            var value = parts[1].Trim();
            using var ins = conn.CreateCommand();
            ins.CommandText = "INSERT INTO CnsProperties (CnsCode, PropertyName, PropertyValue) VALUES ($cns, $pname, $pval)";
            ins.Parameters.AddWithValue("$cns", cns);
            ins.Parameters.AddWithValue("$pname", propName);
            ins.Parameters.AddWithValue("$pval", value);
            ins.ExecuteNonQuery();
            importedProps++;
        }
    }
}

Console.WriteLine($"Imported mappings: {importedMappings}");
Console.WriteLine($"Imported properties entries: {importedProps}");
