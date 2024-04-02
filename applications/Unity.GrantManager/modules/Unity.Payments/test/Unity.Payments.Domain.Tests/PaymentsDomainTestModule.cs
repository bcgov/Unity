using Unity.Payments.EntityFrameworkCore;
using Volo.Abp.Modularity;

namespace Unity.Payments;

/* Domain tests are configured to use the EF Core provider.
 * You can switch to MongoDB, however your domain tests should be
 * database independent anyway.
 */
[DependsOn(
    typeof(PaymentsEntityFrameworkCoreTestModule)
    )]
public class PaymentsDomainTestModule : AbpModule
{

}
