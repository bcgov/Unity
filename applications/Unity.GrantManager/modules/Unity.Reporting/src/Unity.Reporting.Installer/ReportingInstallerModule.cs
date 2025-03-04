using Volo.Abp.Modularity;
using Volo.Abp.VirtualFileSystem;

namespace Unity.Reporting;

[DependsOn(
    typeof(AbpVirtualFileSystemModule)
    )]
public class ReportingInstallerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<ReportingInstallerModule>();
        });
    }
}
