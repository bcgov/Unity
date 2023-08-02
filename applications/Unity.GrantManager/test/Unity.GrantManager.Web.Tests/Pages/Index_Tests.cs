using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Unity.GrantManager.Pages;

public class Index_Tests : GrantManagerWebTestBase
{
    [Fact]
    public async Task Welcome_Page()
    {
        var response = await GetResponseAsStringAsync("/");
        response.ShouldNotBeNull();
    }
}
