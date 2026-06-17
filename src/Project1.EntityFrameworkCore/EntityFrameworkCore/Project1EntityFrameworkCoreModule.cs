using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Volo.Abp.Uow;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.SqlServer;
using Volo.Abp.EntityFrameworkCore.Sqlite;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.TenantManagement.EntityFrameworkCore;

namespace Project1.EntityFrameworkCore;

[DependsOn(
    typeof(Project1DomainModule),
    typeof(AbpIdentityEntityFrameworkCoreModule),
    typeof(AbpOpenIddictEntityFrameworkCoreModule),
    typeof(AbpPermissionManagementEntityFrameworkCoreModule),
    typeof(AbpSettingManagementEntityFrameworkCoreModule),
    typeof(AbpEntityFrameworkCoreSqlServerModule),
    typeof(AbpEntityFrameworkCoreSqliteModule),
    typeof(AbpBackgroundJobsEntityFrameworkCoreModule),
    typeof(AbpAuditLoggingEntityFrameworkCoreModule),
    typeof(AbpTenantManagementEntityFrameworkCoreModule),
    typeof(AbpFeatureManagementEntityFrameworkCoreModule)
    )]
public class Project1EntityFrameworkCoreModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        Project1EfCoreEntityExtensionMappings.Configure();
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        context.Services.AddAbpDbContext<Project1DbContext>(options =>
        {
            // Remove "includeAllEntities: true" to create default repositories only for aggregate roots
            options.AddDefaultRepositories(includeAllEntities: true);
        });

        // Choose provider based on the configured connection string.
        // If the connection string looks like SQLite (contains "Data Source" or references a .db file), use Sqlite;
        // otherwise default to SqlServer. This makes provider selection resilient to environment mismatches.
        Configure<AbpDbContextOptions>(options =>
        {
            var conn = configuration.GetConnectionString("Default") ?? string.Empty;
            if (conn.IndexOf("Data Source=", StringComparison.OrdinalIgnoreCase) >= 0 || conn.IndexOf('.' + "db", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                options.UseSqlite();
            }
            else
            {
                options.UseSqlServer();
            }
        });

    }
}
