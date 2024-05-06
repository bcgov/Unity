using Volo.Abp.Application;
using Volo.Abp.Modularity;
using Volo.Abp.Authorization;
using Volo.Abp.VirtualFileSystem;

namespace Unity.Payments;

[DependsOn(
    typeof(AbpAuthorizationModule),
    typeof(AbpVirtualFileSystemModule),
    typeof(AbpDddApplicationContractsModule)    
    )]
public class PaymentsApplicationContractsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<PaymentsApplicationContractsModule>();
        });
    }
}
