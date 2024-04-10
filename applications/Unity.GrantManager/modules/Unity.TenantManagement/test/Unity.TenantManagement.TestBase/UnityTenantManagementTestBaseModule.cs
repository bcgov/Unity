using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;
using Volo.Abp.TenantManagement;

namespace Unity.TenantManagement;

[DependsOn(
    typeof(AbpTenantManagementDomainModule),
    typeof(AbpAutofacModule),
    typeof(AbpTestBaseModule)
    )]
public class UnityTenantManagementTestBaseModule : AbpModule
{
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        SeedTestData(context);
    }

    private static void SeedTestData(ApplicationInitializationContext context)
    {
        using var scope = context.ServiceProvider.CreateScope();
        scope.ServiceProvider
            .GetRequiredService<UnityTenantManagementTestDataBuilder>()
            .Build();
    }
}

