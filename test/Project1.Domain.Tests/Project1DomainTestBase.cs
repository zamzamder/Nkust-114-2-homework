using Volo.Abp.Modularity;

namespace Project1;

/* Inherit from this class for your domain layer tests. */
public abstract class Project1DomainTestBase<TStartupModule> : Project1TestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
