using Volo.Abp.Autofac;
using Volo.Abp.Http.Client.IdentityModel;
using Volo.Abp.Modularity;

namespace Unity.Payments;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(PaymentsHttpApiClientModule),
    typeof(AbpHttpClientIdentityModelModule)
    )]
public class PaymentsConsoleApiClientModule : AbpModule
{

}
