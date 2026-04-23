using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Project1.Pages;

public class Index_Tests : Project1WebTestBase
{
    [Fact]
    public async Task Welcome_Page()
    {
        var response = await GetResponseAsStringAsync("/");
        response.ShouldNotBeNull();
    }
}
