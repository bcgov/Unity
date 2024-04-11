using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Http.Client;
using Volo.Abp.Modularity;
using Volo.Abp.VirtualFileSystem;

namespace Unity.Payments;

[DependsOn(
    typeof(PaymentsApplicationContractsModule),
    typeof(AbpHttpClientModule))]
public class PaymentsHttpApiClientModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHttpClientProxies(
            typeof(PaymentsApplicationContractsModule).Assembly,
            PaymentsRemoteServiceConsts.RemoteServiceName
        );

        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<PaymentsHttpApiClientModule>();
        });

    }
}
