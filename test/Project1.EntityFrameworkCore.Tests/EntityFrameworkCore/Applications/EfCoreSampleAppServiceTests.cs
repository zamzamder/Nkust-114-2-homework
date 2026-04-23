using Project1.Samples;
using Xunit;

namespace Project1.EntityFrameworkCore.Applications;

[Collection(Project1TestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<Project1EntityFrameworkCoreTestModule>
{

}
