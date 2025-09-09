using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ;
using Volo.Abp.AspNetCore.TestBase;

namespace Unity.GrantManager;

using System.Text.Json.Serialization;

public abstract class GrantManagerWebTestBase : AbpWebApplicationFactoryIntegratedTest<Program>
{
    public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    protected override void ConfigureServices(IServiceCollection services)
    {
        // Remove only RabbitMQ consumer services
        var descriptorsToRemove = services
            .Where(d => d.ServiceType.IsGenericType &&
                        d.ServiceType.GetGenericTypeDefinition() == typeof(QueueConsumerRegistratorService<,>))
            .ToList();

        foreach (var descriptor in descriptorsToRemove)
        {
            services.Remove(descriptor);
        }

        // Remove EventLog logger to avoid ObjectDisposedException in tests
        services.RemoveAll(typeof(Microsoft.Extensions.Logging.EventLog.EventLogLoggerProvider));

        base.ConfigureServices(services);
    }

    protected virtual async Task<T?> GetResponseAsObjectAsync<T>(
        string url,
        HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
    {
        var strResponse = await GetResponseAsStringAsync(url, expectedStatusCode);
        return JsonSerializer.Deserialize<T>(strResponse, GrantManagerWebTestBase.JsonOptions);
    }

    protected virtual async Task<string> GetResponseAsStringAsync(
        string url,
        HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
    {
        var response = await GetResponseAsync(url, expectedStatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    protected virtual async Task<HttpResponseMessage> GetResponseAsync(
        string url,
        HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
    {
        var response = await Client.GetAsync(url);
        response.StatusCode.ShouldBe(expectedStatusCode);
        return response;
    }
}
