using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;
using Volo.Abp.AspNetCore.TestBase;

namespace Unity.GrantManager;

public abstract class GrantManagerWebTestBase : AbpWebApplicationFactoryIntegratedTest<Program>
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    protected override void ConfigureServices(IServiceCollection services)
    {
        //
        // 🔹 Remove ALL RabbitMQ hosted services (consumers, registrators, etc.)
        //
        var hostedServices = services
            .Where(d => typeof(Microsoft.Extensions.Hosting.IHostedService)
                .IsAssignableFrom(d.ServiceType))
            .ToList();

        foreach (var descriptor in hostedServices)
        {
            // Only strip RabbitMQ-related hosted services
            if (descriptor.ImplementationType?.Namespace?.Contains("RabbitMQ") == true)
            {
                services.Remove(descriptor);
            }
        }

#if WINDOWS
        // 🔹 Remove EventLog logger to avoid ObjectDisposedException in tests
        services.RemoveAll<Microsoft.Extensions.Logging.EventLog.EventLogLoggerProvider>();
#endif

        //
        // 🔹 Replace real channel provider with fake
        //
        services.Replace(
            ServiceDescriptor.Singleton<IChannelProvider, FakeChannelProvider>()
        );

        base.ConfigureServices(services);
    }

    protected virtual async Task<T?> GetResponseAsObjectAsync<T>(
        string url,
        HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
    {
        var strResponse = await GetResponseAsStringAsync(url, expectedStatusCode);
        return JsonSerializer.Deserialize<T>(strResponse, JsonOptions);
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
