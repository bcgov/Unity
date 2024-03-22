using Volo.Abp.Modularity;

namespace Notifications;

[DependsOn(
    typeof(NotificationsApplicationModule),
    typeof(NotificationsDomainTestModule)
    )]
public class NotificationsApplicationTestModule : AbpModule
{

}
