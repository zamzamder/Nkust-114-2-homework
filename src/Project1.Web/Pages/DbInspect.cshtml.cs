using System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;

namespace Project1.Web.Pages
{
    public class DbInspectModel : PageModel
    {
        private readonly IWebHostEnvironment _env;

        public DbInspectModel(IWebHostEnvironment env)
        {
            _env = env;
        }
        [BindProperty]
        public string DbPath { get; set; } = "src/Project1.Web/project1_dev.db";

        public List<TableInfo> Tables { get; set; } = new();
        [BindProperty]
        public string SelectedTable { get; set; } = string.Empty;

        [BindProperty]
        public int PreviewRows { get; set; } = 10;

        public List<ColumnInfo> TableColumns { get; set; } = new();

        public List<System.Collections.Generic.Dictionary<string, object?>> PreviewData { get; set; } = new();

        [BindProperty]
        public string AdHocSql { get; set; } = string.Empty;

        public class ColumnInfo
        {
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public bool NotNull { get; set; }
            public string DefaultValue { get; set; } = string.Empty;
            public bool IsPk { get; set; }
        }

        public List<string> AvailableFiles { get; set; } = new();

        public string ErrorMessage { get; set; } = string.Empty;

        public void OnGet()
        {
            // populate available .db files under content root, excluding bin/obj folders
            try
            {
                var root = _env.ContentRootPath ?? Environment.CurrentDirectory;
                var files = Directory.GetFiles(root, "*.db", SearchOption.AllDirectories)
                    .Where(f => !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                                && !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
                    .OrderBy(f => f)
                    .ToList();

                AvailableFiles = files.Select(f => Path.GetRelativePath(root, f).Replace('\\', '/')).ToList();
            }
            catch
            {
                // ignore errors when enumerating files
                AvailableFiles = new List<string>();
            }
        }

        public IActionResult OnPost()
        {
            Tables.Clear();
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(DbPath))
            {
                ErrorMessage = "Please provide a database file path.";
                return Page();
            }

            // Try resolving the provided path using several sensible candidates so UI values work
            // regardless of whether they are relative to the solution, project, or already absolute.
            var tried = new List<string>();

            bool exists = false;
            string resolvedPath = DbPath!;

            if (Path.IsPathRooted(DbPath))
            {
                tried.Add(DbPath);
                if (System.IO.File.Exists(DbPath))
                {
                    exists = true;
                    resolvedPath = DbPath;
                }
            }

            if (!exists)
            {
                var contentRoot = _env.ContentRootPath ?? Directory.GetCurrentDirectory();

                // Candidate A: combine content root with the UI-provided relative path
                var relative = DbPath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
                var candidateA = Path.GetFullPath(Path.Combine(contentRoot, relative));
                tried.Add(candidateA);
                if (System.IO.File.Exists(candidateA)) { exists = true; resolvedPath = candidateA; }
            }

            if (!exists)
            {
                // Candidate B: the file name inside the content root (in case UI sent a long path starting with project folder)
                var candidateB = Path.GetFullPath(Path.Combine(_env.ContentRootPath ?? Directory.GetCurrentDirectory(), Path.GetFileName(DbPath)));
                tried.Add(candidateB);
                if (System.IO.File.Exists(candidateB)) { exists = true; resolvedPath = candidateB; }
            }

            if (!exists)
            {
                // Candidate C: relative to solution root (one level up from content root)
                try
                {
                    var solutionRoot = Path.GetFullPath(Path.Combine(_env.ContentRootPath ?? Directory.GetCurrentDirectory(), ".."));
                    var candidateC = Path.GetFullPath(Path.Combine(solutionRoot, DbPath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar)));
                    tried.Add(candidateC);
                    if (System.IO.File.Exists(candidateC)) { exists = true; resolvedPath = candidateC; }
                }
                catch { }
            }

            if (!exists)
            {
                ErrorMessage = $"Database file not found. Tried:\n{string.Join("\n", tried)}";
                return Page();
            }

            DbPath = resolvedPath;

            try
            {
                var connString = new SqliteConnectionStringBuilder { DataSource = DbPath }.ToString();
                using var conn = new SqliteConnection(connString);
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT name, type FROM sqlite_schema WHERE type IN ('table','view') AND name NOT LIKE 'sqlite_%' ORDER BY name;";
                using var reader = cmd.ExecuteReader();
                var found = new List<(string Name, string Type)>();
                while (reader.Read())
                {
                    found.Add((reader.GetString(0), reader.GetString(1)));
                }

                if (found.Count == 0)
                {
                    ErrorMessage = "No user tables or views found in the database.";
                    return Page();
                }

                foreach (var t in found)
                {
                    var info = new TableInfo { Name = t.Name, Type = t.Type };
                    using var c = conn.CreateCommand();
                    c.CommandText = $"SELECT COUNT(1) FROM \"{t.Name}\";";
                    try
                    {
                        var cnt = c.ExecuteScalar();
                        info.Count = cnt?.ToString() ?? "0";
                    }
                    catch (Exception ex)
                    {
                        info.Error = ex.Message;
                    }
                    Tables.Add(info);
                }
                // Pre-select first table if any
                if (Tables.Count > 0 && string.IsNullOrEmpty(SelectedTable))
                {
                    SelectedTable = Tables[0].Name;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            return Page();
        }

        public IActionResult OnPostPreview()
        {
            // re-run table list to ensure AvailableFiles etc are populated
            OnGet();

            if (string.IsNullOrEmpty(DbPath) || string.IsNullOrEmpty(SelectedTable))
            {
                ErrorMessage = "Select a table to preview.";
                return Page();
            }

            try
            {
                var connString = new SqliteConnectionStringBuilder { DataSource = DbPath }.ToString();
                using var conn = new SqliteConnection(connString);
                conn.Open();

                // get columns
                TableColumns.Clear();
                using (var c = conn.CreateCommand())
                {
                    c.CommandText = $"PRAGMA table_info(\"{SelectedTable}\");";
                    using var r = c.ExecuteReader();
                    while (r.Read())
                    {
                        TableColumns.Add(new ColumnInfo
                        {
                            Name = r.GetString(r.GetOrdinal("name")),
                            Type = r.GetString(r.GetOrdinal("type")),
                            NotNull = r.GetInt32(r.GetOrdinal("notnull")) != 0,
                            DefaultValue = r.IsDBNull(r.GetOrdinal("dflt_value")) ? string.Empty : r.GetValue(r.GetOrdinal("dflt_value")).ToString() ?? string.Empty,
                            IsPk = r.GetInt32(r.GetOrdinal("pk")) != 0
                        });
                    }
                }

                // preview rows
                PreviewData.Clear();
                using (var c = conn.CreateCommand())
                {
                    c.CommandText = $"SELECT * FROM \"{SelectedTable}\" LIMIT {PreviewRows};";
                    using var r = c.ExecuteReader();
                    while (r.Read())
                    {
                        var dict = new System.Collections.Generic.Dictionary<string, object?>();
                        for (int i = 0; i < r.FieldCount; i++)
                        {
                            var name = r.GetName(i);
                            var val = r.IsDBNull(i) ? null : r.GetValue(i);
                            dict[name] = val;
                        }
                        PreviewData.Add(dict);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            return Page();
        }

        public IActionResult OnPostSchema()
        {
            // Schema only
            if (string.IsNullOrEmpty(SelectedTable))
            {
                ErrorMessage = "Select a table.";
                return Page();
            }
            // reuse preview to populate schema
            return OnPostPreview();
        }

        public IActionResult OnPostExport()
        {
            if (string.IsNullOrEmpty(SelectedTable))
            {
                ErrorMessage = "Select a table to export.";
                return Page();
            }

            if (!User?.IsInRole("Admin") ?? false)
            {
                // still allow export for authenticated users; not restricting here intentionally
            }

            try
            {
                var connString = new SqliteConnectionStringBuilder { DataSource = DbPath }.ToString();
                using var conn = new SqliteConnection(connString);
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT * FROM \"{SelectedTable}\";";
                using var reader = cmd.ExecuteReader();

                // build CSV in memory
                using var ms = new MemoryStream();
                using var sw = new StreamWriter(ms);

                // header
                var cols = new string[reader.FieldCount];
                for (int i = 0; i < reader.FieldCount; i++) cols[i] = reader.GetName(i);
                sw.WriteLine(string.Join(',', cols.Select(h => EscapeCsv(h))));

                while (reader.Read())
                {
                    var vals = new string[reader.FieldCount];
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var v = reader.IsDBNull(i) ? string.Empty : reader.GetValue(i)?.ToString() ?? string.Empty;
                        vals[i] = EscapeCsv(v);
                    }
                    sw.WriteLine(string.Join(',', vals));
                }
                sw.Flush();
                ms.Position = 0;
                var fileName = SelectedTable + ".csv";
                return File(ms.ToArray(), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return Page();
            }
        }

        public IActionResult OnPostQuery()
        {
            if (!(User?.IsInRole("Admin") ?? false))
            {
                ErrorMessage = "Ad-hoc SQL is restricted to administrators.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(AdHocSql))
            {
                ErrorMessage = "Enter a SQL query.";
                return Page();
            }

            var sql = AdHocSql.Trim();
            if (!sql.StartsWith("select", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "Only SELECT queries are allowed.";
                return Page();
            }

            if (sql.IndexOf(";") >= 0)
            {
                ErrorMessage = "Multiple statements are not allowed.";
                return Page();
            }

            try
            {
                var connString = new SqliteConnectionStringBuilder { DataSource = DbPath }.ToString();
                using var conn = new SqliteConnection(connString);
                conn.Open();

                PreviewData.Clear();
                TableColumns.Clear();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql + " LIMIT 1000"; // guard
                using var reader = cmd.ExecuteReader();

                // columns
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    TableColumns.Add(new ColumnInfo { Name = reader.GetName(i), Type = "" });
                }

                while (reader.Read())
                {
                    var dict = new System.Collections.Generic.Dictionary<string, object?>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var name = reader.GetName(i);
                        var val = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        dict[name] = val;
                    }
                    PreviewData.Add(dict);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            return Page();
        }

        private static string EscapeCsv(string s)
        {
            if (s.Contains('"') || s.Contains(',') || s.Contains('\n') || s.Contains('\r'))
            {
                return '"' + s.Replace("\"", "\"\"") + '"';
            }
            return s;
        }

        public class TableInfo
        {
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string Count { get; set; } = string.Empty;
            public string Error { get; set; } = string.Empty;
        }
    }
}
