using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Project1.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
public class Project1DbContextFactory : IDesignTimeDbContextFactory<Project1DbContext>
{
    public Project1DbContext CreateDbContext(string[] args)
    {
        Project1EfCoreEntityExtensionMappings.Configure();

        var configuration = BuildConfiguration();

        var defaultConn = configuration.GetConnectionString("Default");

        var builder = new DbContextOptionsBuilder<Project1DbContext>();

        // If the configured connection string looks like a Sqlite file, use Sqlite provider.
        if (!string.IsNullOrWhiteSpace(defaultConn) &&
            (defaultConn.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) || defaultConn.EndsWith(".db", StringComparison.OrdinalIgnoreCase)))
        {
            builder.UseSqlite(defaultConn);
        }
        else
        {
            builder.UseSqlServer(defaultConn);
        }

        return new Project1DbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../Project1.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false);

        return builder.Build();
    }
}
