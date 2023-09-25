using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Unity.GrantManager;

public class GrantManagerWebTestStartup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddApplication<GrantManagerWebTestModule>();
    }

    public static void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
    {
        app.InitializeApplication();
    }
}
