using Volo.Abp.Domain;
using Volo.Abp.Modularity;

namespace Unity.Payments;

[DependsOn(
    typeof(AbpDddDomainModule),
    typeof(PaymentsDomainSharedModule)
)]
public class PaymentsDomainModule : AbpModule
{

}
