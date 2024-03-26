using Volo.Abp.Autofac;
using Volo.Abp.Http.Client.IdentityModel;
using Volo.Abp.Modularity;

namespace Unity.Notifications;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(NotificationsHttpApiClientModule),
    typeof(AbpHttpClientIdentityModelModule)
    )]
public class NotificationsConsoleApiClientModule : AbpModule
{

}
