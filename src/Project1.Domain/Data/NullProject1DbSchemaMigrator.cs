using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Project1.Data;

/* This is used if database provider does't define
 * IProject1DbSchemaMigrator implementation.
 */
public class NullProject1DbSchemaMigrator : IProject1DbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
