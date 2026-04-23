using Volo.Abp.Modularity;

namespace Project1;

[DependsOn(
    typeof(Project1ApplicationModule),
    typeof(Project1DomainTestModule)
)]
public class Project1ApplicationTestModule : AbpModule
{

}
