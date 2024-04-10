using Volo.Abp.Domain;
using Volo.Abp.Modularity;

namespace Unity.Notifications;

[DependsOn(
    typeof(AbpDddDomainModule),
    typeof(NotificationsDomainSharedModule)
)]
public class NotificationsDomainModule : AbpModule
{

}
