using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Http.Client;
using Volo.Abp.Modularity;
using Volo.Abp.VirtualFileSystem;

namespace Unity.Notifications;

[DependsOn(
    typeof(NotificationsApplicationContractsModule),
    typeof(AbpHttpClientModule))]
public class NotificationsHttpApiClientModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHttpClientProxies(
            typeof(NotificationsApplicationContractsModule).Assembly,
            NotificationsRemoteServiceConsts.RemoteServiceName
        );

        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<NotificationsHttpApiClientModule>();
        });

    }
}
