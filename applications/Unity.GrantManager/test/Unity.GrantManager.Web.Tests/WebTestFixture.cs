using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;
using Xunit;

namespace Unity.GrantManager;

public class WebTestFixture : WebApplicationFactory<Program>
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

if (OperatingSystem.IsWindows())
            {
                services.RemoveAll<Microsoft.Extensions.Logging.EventLog.EventLogLoggerProvider>();
            }

            services.Replace(
                ServiceDescriptor.Singleton<IChannelProvider, FakeChannelProvider>()
            );
        });
    }
}

[CollectionDefinition(WebTestCollection.Name)]
public class WebTestCollection : ICollectionFixture<WebTestFixture>
{
    public const string Name = "WebTests";
}
