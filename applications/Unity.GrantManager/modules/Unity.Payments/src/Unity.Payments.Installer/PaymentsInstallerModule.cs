using Volo.Abp.Modularity;
using Volo.Abp.VirtualFileSystem;

namespace Unity.Payments;

[DependsOn(
    typeof(AbpVirtualFileSystemModule)
    )]
public class PaymentsInstallerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<PaymentsInstallerModule>();
        });
    }
}
