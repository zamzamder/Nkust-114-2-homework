using Volo.Abp.Modularity;

namespace Project1;

public abstract class Project1ApplicationTestBase<TStartupModule> : Project1TestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
