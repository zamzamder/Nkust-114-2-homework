using Microsoft.Data.Sqlite;
using System;
using System.IO;

var dbPath = args.Length > 0 ? args[0] : "src/Project1.Web/project1_dev.db";
if (!File.Exists(dbPath))
{
    Console.Error.WriteLine($"Database file not found: {dbPath}");
    return 1;
}

var connString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
using var conn = new SqliteConnection(connString);
conn.Open();

// get tables
using var cmd = conn.CreateCommand();
cmd.CommandText = "SELECT name, type FROM sqlite_schema WHERE type IN ('table','view') AND name NOT LIKE 'sqlite_%' ORDER BY name;";
using var reader = cmd.ExecuteReader();
var tables = new System.Collections.Generic.List<string>();
while (reader.Read())
{
    tables.Add(reader.GetString(0));
}

if (tables.Count == 0)
{
    Console.WriteLine("No user tables or views found in the database.");
    return 0;
}

Console.WriteLine("Found tables/views:");
foreach (var t in tables)
{
    Console.WriteLine($"- {t}");
}

Console.WriteLine();
Console.WriteLine("Row counts:");
foreach (var t in tables)
{
    using var c = conn.CreateCommand();
    c.CommandText = $"SELECT COUNT(1) FROM \"{t}\";";
    try
    {
        var cnt = c.ExecuteScalar();
        Console.WriteLine($"- {t}: {cnt}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"- {t}: (error counting rows: {ex.Message})");
    }
}

return 0;
