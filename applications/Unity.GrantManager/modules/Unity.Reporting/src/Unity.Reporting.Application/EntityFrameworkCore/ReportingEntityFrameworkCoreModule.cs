using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Unity.Reporting.EntityFrameworkCore;

public class ReportingEntityFrameworkCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAbpDbContext<ReportingDbContext>(options =>
        {
            /* Add custom repositories here */
        });
    }
}
