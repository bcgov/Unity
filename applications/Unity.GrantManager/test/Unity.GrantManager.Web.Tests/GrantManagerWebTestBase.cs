using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.AspNetCore.TestBase;

namespace Unity.GrantManager;

public abstract class GrantManagerWebTestBase : AbpWebApplicationFactoryIntegratedTest<Program>
{
    protected virtual async Task<T?> GetResponseAsObjectAsync<T>(string url, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
    {
        var strResponse = await GetResponseAsStringAsync(url, expectedStatusCode);
        JsonSerializerOptions jsonSerializerOptions = new(JsonSerializerDefaults.Web);
        JsonSerializerOptions options = jsonSerializerOptions;
        return JsonSerializer.Deserialize<T>(strResponse, options);
    }

    protected virtual async Task<string> GetResponseAsStringAsync(string url, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
    {
        var response = await GetResponseAsync(url, expectedStatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    protected virtual async Task<HttpResponseMessage> GetResponseAsync(string url, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
    {
        var response = await Client.GetAsync(url);
        response.StatusCode.ShouldBe(expectedStatusCode);
        return response;
    }
}
