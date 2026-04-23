using Project1.Samples;
using Xunit;

namespace Project1.EntityFrameworkCore.Domains;

[Collection(Project1TestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<Project1EntityFrameworkCoreTestModule>
{

}
