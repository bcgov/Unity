using Volo.Abp.Autofac;
using Volo.Abp.Http.Client.IdentityModel;
using Volo.Abp.Modularity;

namespace Notifications;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(NotificationsHttpApiClientModule),
    typeof(AbpHttpClientIdentityModelModule)
    )]
public class NotificationsConsoleApiClientModule : AbpModule
{

}
