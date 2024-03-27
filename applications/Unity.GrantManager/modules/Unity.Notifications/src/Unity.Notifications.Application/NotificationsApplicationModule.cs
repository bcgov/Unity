using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.Application;
using Unity.Notifications.Integration.Ches;
using Unity.Notifications.Integrations.Ches;

namespace Unity.Notifications;

[DependsOn(
    typeof(NotificationsDomainModule),
    typeof(NotificationsApplicationContractsModule),
    typeof(AbpDddApplicationModule),
    typeof(AbpAutoMapperModule)
    )]
public class NotificationsApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAutoMapperObjectMapper<NotificationsApplicationModule>();
        context.Services.AddScoped<IChesClientService, ChesClientService>();
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<NotificationsApplicationModule>(validate: true);
        });
    }
}
