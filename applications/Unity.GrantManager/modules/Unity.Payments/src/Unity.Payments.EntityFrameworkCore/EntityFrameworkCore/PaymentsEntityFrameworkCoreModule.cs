using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Modularity;

namespace Unity.Payments.EntityFrameworkCore;

[DependsOn(
    typeof(PaymentsDomainModule),
    typeof(AbpEntityFrameworkCoreModule)
)]
public class PaymentsEntityFrameworkCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAbpDbContext<PaymentsDbContext>(options =>
        {
           /* Add custom repositories here */
        });
    }
}
