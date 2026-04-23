using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;

namespace Project1.EntityFrameworkCore.Data;

public class CnsFilesDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    public ILogger<CnsFilesDataSeedContributor> Logger { get; set; }

    private readonly Project1DbContext _dbContext;

    public CnsFilesDataSeedContributor(Project1DbContext dbContext)
    {
        _dbContext = dbContext;
        Logger = NullLogger<CnsFilesDataSeedContributor>.Instance;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        try
        {
            var root = FindRepoRoot();
            if (root == null)
            {
                Logger.LogWarning("Repository root not found. Skipping CNS files import.");
                return;
            }

            var mappingFolders = new[] {
                Path.Combine(root, "src", "MapingTables"),
                Path.Combine(root, "src", "MappingTables")
            };

            var propertiesFolders = new[] {
                Path.Combine(root, "src", "Properties"),
                Path.Combine(root, "Properties")
            };

            await _dbContext.Database.OpenConnectionAsync();
            using var tx = await _dbContext.Database.BeginTransactionAsync();

            // create tables if not exists
            await _dbContext.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS CnsMappings (
                CnsCode TEXT PRIMARY KEY,
                UnicodeChar TEXT
            );");

            await _dbContext.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS CnsProperties (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CnsCode TEXT,
                PropertyName TEXT,
                PropertyValue TEXT
            );");

            // import mapping files
            foreach (var folder in mappingFolders)
            {
                if (!Directory.Exists(folder)) continue;

                foreach (var file in Directory.EnumerateFiles(folder, "*.txt", SearchOption.AllDirectories))
                {
                    try
                    {
                        await ImportMappingFile(file);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Failed to import mapping file {file}", file);
                    }
                }
            }

            // import properties files
            foreach (var folder in propertiesFolders)
            {
                if (!Directory.Exists(folder)) continue;

                foreach (var file in Directory.EnumerateFiles(folder, "*.txt", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        var propName = Path.GetFileNameWithoutExtension(file);
                        await ImportPropertiesFile(file, propName);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Failed to import properties file {file}", file);
                    }
                }
            }

            await tx.CommitAsync();
            Logger.LogInformation("CNS files imported to database.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to import CNS files");
        }
    }

    private string? FindRepoRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current != null)
        {
            if (current.GetFiles("*.sln").Any()) return current.FullName;
            current = current.Parent;
        }

        return null;
    }

    private static readonly Regex HexRegex = new("^[0-9A-Fa-f]{4,6}$");

    private static string NormalizeCns(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return string.Empty;
        var t = token.Trim();
        t = t.Trim('"', '\'', '\r', '\n');
        return t.ToUpperInvariant();
    }

    private static string? ParseUnicodeFromToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var t = token.Trim();
        if (t.StartsWith("U+", StringComparison.OrdinalIgnoreCase)) t = t.Substring(2);
        if (t.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) t = t.Substring(2);

        if (t.Any(c => char.GetUnicodeCategory(c) == UnicodeCategory.OtherLetter || (c >= 0x4E00 && c <= 0x9FFF)))
        {
            return t[0].ToString();
        }

        if (HexRegex.IsMatch(t))
        {
            try
            {
                var code = int.Parse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                return char.ConvertFromUtf32(code);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private static string[] SmartSplit(string line)
    {
        if (line.Contains('\t')) return line.Split('\t', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
        if (line.Contains(',')) return line.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
        if (line.Contains(' ')) return line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
        if (line.Contains('=')) return line.Split('=', 2).Select(s => s.Trim()).ToArray();
        return new[] { line.Trim() };
    }

    private async Task ImportMappingFile(string filePath)
    {
        using var sr = new StreamReader(filePath);
        string? line;
        while ((line = await sr.ReadLineAsync()) != null)
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

            // upsert
            await _dbContext.Database.ExecuteSqlRawAsync(
                "INSERT OR REPLACE INTO CnsMappings (CnsCode, UnicodeChar) VALUES ({0}, {1});",
                cns, unicodeChar);
        }
    }

    private async Task ImportPropertiesFile(string filePath, string propertyName)
    {
        using var sr = new StreamReader(filePath);
        string? line;
        while ((line = await sr.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = SmartSplit(line);
            if (parts.Length < 2) continue;

            var cns = NormalizeCns(parts[0]);
            var value = parts[1].Trim();

            if (string.IsNullOrEmpty(cns)) continue;

            await _dbContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO CnsProperties (CnsCode, PropertyName, PropertyValue) VALUES ({0}, {1}, {2});",
                cns, propertyName, value);
        }
    }
}
