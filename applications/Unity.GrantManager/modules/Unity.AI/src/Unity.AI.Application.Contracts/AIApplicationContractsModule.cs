using Volo.Abp.Application;
using Volo.Abp.Modularity;
using Volo.Abp.Authorization;
using Volo.Abp.SettingManagement;

namespace Unity.AI;

[DependsOn(
    typeof(AIDomainSharedModule),
    typeof(AbpDddApplicationContractsModule),
    typeof(AbpAuthorizationModule),
    typeof(AbpSettingManagementApplicationContractsModule)
    )]
public class AIApplicationContractsModule : AbpModule
{

}
