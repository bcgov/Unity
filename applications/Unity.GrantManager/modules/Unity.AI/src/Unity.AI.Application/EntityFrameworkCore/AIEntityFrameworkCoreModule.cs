using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Unity.AI.EntityFrameworkCore;

public class AIEntityFrameworkCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAbpDbContext<AIDbContext>(options =>
        {
            /* Add custom repositories here */
        });
    }
}
