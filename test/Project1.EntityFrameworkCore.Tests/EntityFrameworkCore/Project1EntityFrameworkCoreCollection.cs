using Xunit;

namespace Project1.EntityFrameworkCore;

[CollectionDefinition(Project1TestConsts.CollectionDefinitionName)]
public class Project1EntityFrameworkCoreCollection : ICollectionFixture<Project1EntityFrameworkCoreFixture>
{

}
