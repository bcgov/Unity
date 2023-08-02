using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp;

namespace Unity.GrantManager;

public class GrantManagerWebTestStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddApplication<GrantManagerWebTestModule>();
    }

    public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
    {
        app.InitializeApplication();
    }
}
