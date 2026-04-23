using Volo.Abp.Modularity;

namespace Project1;

[DependsOn(
    typeof(Project1DomainModule),
    typeof(Project1TestBaseModule)
)]
public class Project1DomainTestModule : AbpModule
{

}
