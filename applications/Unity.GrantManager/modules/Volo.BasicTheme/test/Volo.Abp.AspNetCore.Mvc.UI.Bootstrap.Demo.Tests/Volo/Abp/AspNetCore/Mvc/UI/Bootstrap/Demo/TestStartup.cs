using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.Demo;

public class TestStartup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddApplication<AbpAspNetCoreMvcUiBootstrapDemoTestModule>();
    }

    public static void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
    {
        app.InitializeApplication();
    }
}
