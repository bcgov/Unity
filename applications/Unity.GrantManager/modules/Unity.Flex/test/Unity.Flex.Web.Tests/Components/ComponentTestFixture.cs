using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Unity.GrantManager;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;

namespace Unity.Flex.Web.Tests.Components;

public class ComponentTestFixture : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var hostedServices = services
                .Where(d => typeof(IHostedService)
                    .IsAssignableFrom(d.ServiceType))
                .ToList();

            foreach (var descriptor in hostedServices)
            {
                if (descriptor.ImplementationType?.Namespace?.Contains("RabbitMQ") == true)
                {
                    services.Remove(descriptor);
                }
            }

#if WINDOWS
            services.RemoveAll<Microsoft.Extensions.Logging.EventLog.EventLogLoggerProvider>();
#endif

            services.Replace(
                ServiceDescriptor.Singleton<IChannelProvider, FakeChannelProvider>()
            );
        });
    }
}

[CollectionDefinition(ComponentTestCollection.Name)]
public class ComponentTestCollection : ICollectionFixture<ComponentTestFixture>
{
    public const string Name = "ComponentTests";
}
