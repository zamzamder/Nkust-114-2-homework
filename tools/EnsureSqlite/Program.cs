using System;
using Microsoft.EntityFrameworkCore;
using Project1.EntityFrameworkCore;

if (args.Length == 0)
{
    Console.WriteLine("Usage: EnsureSqlite <path-to-db>");
    return 1;
}

var dbPath = args[0];
var conn = $"Data Source={dbPath}";

var optionsBuilder = new DbContextOptionsBuilder<Project1DbContext>();
optionsBuilder.UseSqlite(conn);

try
{
    using var context = new Project1DbContext(optionsBuilder.Options);
    Console.WriteLine($"Ensuring database created at: {dbPath}");
    context.Database.EnsureCreated();
    Console.WriteLine("Database ensure created completed.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.ToString());
    return 2;
}
