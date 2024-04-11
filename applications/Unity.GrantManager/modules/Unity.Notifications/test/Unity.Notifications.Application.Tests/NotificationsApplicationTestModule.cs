using Volo.Abp.Modularity;

namespace Unity.Notifications;

[DependsOn(
    typeof(NotificationsApplicationModule),
    typeof(NotificationsDomainTestModule)
    )]
public class NotificationsApplicationTestModule : AbpModule
{

}
