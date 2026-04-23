using System.Threading.Tasks;

namespace Project1.Data;

public interface IProject1DbSchemaMigrator
{
    Task MigrateAsync();
}
