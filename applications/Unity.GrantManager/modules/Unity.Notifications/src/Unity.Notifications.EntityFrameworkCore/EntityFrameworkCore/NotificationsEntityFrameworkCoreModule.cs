using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Modularity;

namespace Unity.Notifications.EntityFrameworkCore;

[DependsOn(
    typeof(NotificationsDomainModule),
    typeof(AbpEntityFrameworkCoreModule)
)]
public class NotificationsEntityFrameworkCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAbpDbContext<NotificationsDbContext>(options =>
        {
            // Add custom repositories here.
        });
    }
}
