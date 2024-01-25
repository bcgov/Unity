using Volo.Abp.Application;
using Volo.Abp.Authorization;
using Volo.Abp.Modularity;
using Volo.Abp.ObjectExtending;
using Volo.Abp.ObjectExtending.Modularity;
using Volo.Abp.TenantManagement;
using Volo.Abp.Threading;

namespace Unity.TenantManagement;

[DependsOn(
    typeof(AbpDddApplicationContractsModule),
    typeof(AbpTenantManagementDomainSharedModule),
    typeof(AbpAuthorizationAbstractionsModule)
    )]
public class UnityTenantManagementApplicationContractsModule : AbpModule
{
    private static readonly OneTimeRunner OneTimeRunner = new();

    public override void PostConfigureServices(ServiceConfigurationContext context)
    {
        OneTimeRunner.Run(() =>
        {
            ModuleExtensionConfigurationHelper
                .ApplyEntityConfigurationToApi(
                    TenantManagementModuleExtensionConsts.ModuleName,
                    TenantManagementModuleExtensionConsts.EntityNames.Tenant,
                    getApiTypes: new[] { typeof(TenantDto) },
                    createApiTypes: new[] { typeof(TenantCreateDto) },
                    updateApiTypes: new[] { typeof(TenantUpdateDto) }
                );
        });
    }
}
