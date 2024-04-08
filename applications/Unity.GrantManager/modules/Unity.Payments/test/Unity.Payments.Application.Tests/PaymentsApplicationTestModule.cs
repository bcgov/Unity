using Volo.Abp.Modularity;

namespace Unity.Payments;

[DependsOn(
    typeof(PaymentsApplicationModule),
    typeof(PaymentsDomainTestModule)
    )]
public class PaymentsApplicationTestModule : AbpModule
{

}
