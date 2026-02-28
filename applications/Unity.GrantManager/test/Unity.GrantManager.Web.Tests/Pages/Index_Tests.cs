using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Unity.GrantManager.Pages;

[Collection(WebTestCollection.Name)]
public class Index_Tests
{
    private readonly HttpClient _client;

    public Index_Tests(WebTestFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task Welcome_Page()
    {
        var response = await _client.GetAsync("/");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldNotBeNullOrEmpty();
    }
}
