using Volo.Abp.Modularity;
using Volo.Abp.VirtualFileSystem;

namespace Unity.Notifications;

[DependsOn(
    typeof(AbpVirtualFileSystemModule)
    )]
public class NotificationsInstallerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<NotificationsInstallerModule>();
        });
    }
}
