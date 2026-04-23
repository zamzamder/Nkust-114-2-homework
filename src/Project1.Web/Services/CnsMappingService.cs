using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Project1.Web.Services
{
    // A lightweight, resilient CNS11643 mapping loader and lookup service.
    // It scans probable mapping folders and attempts to parse common formats.
    public class CnsMappingService : ICnsMappingService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<CnsMappingService> _logger;
        private readonly ConcurrentDictionary<string, string> _cnsToUnicode = new();
        private readonly ConcurrentDictionary<string, HashSet<string>> _unicodeToCns = new();
        private volatile bool _loaded;
        private readonly object _loadLock = new();

        public CnsMappingService(IWebHostEnvironment env, ILogger<CnsMappingService> logger)
        {
            _env = env;
            _logger = logger;
        }

        public void EnsureLoaded()
        {
            if (_loaded) return;
            lock (_loadLock)
            {
                if (_loaded) return;
                LoadMappings();
                _loaded = true;
            }
        }

        private void LoadMappings()
        {
            try
            {
                var candidates = GetCandidateFolders();

                foreach (var folder in candidates)
                {
                    if (!Directory.Exists(folder)) continue;

                    foreach (var file in Directory.EnumerateFiles(folder, "*.txt", SearchOption.TopDirectoryOnly))
                    {
                        try
                        {
                            ParseFile(file);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse mapping file {file}", file);
                        }
                    }
                }

                _logger.LogInformation("CNS mapping loaded. entries: {count}", _cnsToUnicode.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load CNS mappings");
            }
        }

        private IEnumerable<string> GetCandidateFolders()
        {
            var contentRoot = _env.ContentRootPath ?? AppContext.BaseDirectory;
            var baseDir = AppContext.BaseDirectory ?? contentRoot;

            // common possible locations relative to content root or base dir
            var candidates = new List<string>
            {
                Path.Combine(contentRoot, "MapingTables"),
                Path.Combine(contentRoot, "MappingTables"),
                Path.Combine(contentRoot, "Properties"),
                Path.Combine(contentRoot, "..", "MapingTables"),
                Path.Combine(contentRoot, "..", "MappingTables"),
                Path.Combine(contentRoot, "..", "Properties"),
                Path.Combine(baseDir, "MapingTables"),
                Path.Combine(baseDir, "MappingTables"),
                Path.Combine(baseDir, "Properties")
            };

            return candidates.Distinct();
        }

        private static readonly Regex HexRegex = new("^[0-9A-Fa-f]{4,6}$");

        private void ParseFile(string filePath)
        {
            using var sr = new StreamReader(filePath);
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
                    // maybe the file is reversed (Unicode then CNS)
                    var maybeCns = NormalizeCns(right);
                    var maybeUnicode = ParseUnicodeFromToken(left);
                    if (!string.IsNullOrEmpty(maybeCns) && !string.IsNullOrEmpty(maybeUnicode))
                    {
                        cns = maybeCns;
                        unicodeChar = maybeUnicode;
                    }
                    else
                    {
                        continue;
                    }
                }

                _cnsToUnicode[cns] = unicodeChar;

                _unicodeToCns.AddOrUpdate(unicodeChar,
                    _ => new HashSet<string> { cns },
                    (_, set) => { set.Add(cns); return set; });
            }
        }

        private static string[] SmartSplit(string line)
        {
            // try tab, comma, whitespace
            if (line.Contains('\t')) return line.Split('\t', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
            if (line.Contains(',')) return line.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
            if (line.Contains(' ')) return line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
            if (line.Contains('=')) return line.Split('=', 2).Select(s => s.Trim()).ToArray();
            return new[] { line.Trim() };
        }

        private static string NormalizeCns(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return string.Empty;
            var t = token.Trim();
            // remove surrounding quotes
            t = t.Trim('"', '\'', '\r', '\n');
            // common CNS formats: plane-number + code or like "1-4E00" or "A1A1"
            // return upper-case normalized
            return t.ToUpperInvariant();
        }

        private static string? ParseUnicodeFromToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;
            var t = token.Trim();
            // remove U+ or 0x prefixes
            if (t.StartsWith("U+", StringComparison.OrdinalIgnoreCase)) t = t.Substring(2);
            if (t.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) t = t.Substring(2);

            // if token already contains a Chinese character
            if (t.Any(c => char.GetUnicodeCategory(c) == UnicodeCategory.OtherLetter || (c >= 0x4E00 && c <= 0x9FFF)))
            {
                // return first character
                return t[0].ToString();
            }

            // if token is hex code
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

        public bool TryGetUnicode(string cnsCode, out string unicodeChar)
        {
            EnsureLoaded();
            unicodeChar = string.Empty;
            if (string.IsNullOrWhiteSpace(cnsCode)) return false;
            var key = NormalizeCns(cnsCode);
            return _cnsToUnicode.TryGetValue(key, out unicodeChar);
        }

        public IEnumerable<string> GetCnsCodes(string unicodeChar)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(unicodeChar)) return Array.Empty<string>();
            if (_unicodeToCns.TryGetValue(unicodeChar, out var set)) return set;
            return Array.Empty<string>();
        }
    }
}
