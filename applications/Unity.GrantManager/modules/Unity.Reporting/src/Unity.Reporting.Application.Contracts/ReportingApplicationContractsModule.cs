using Volo.Abp.Application;
using Volo.Abp.Modularity;
using Volo.Abp.Authorization;

namespace Unity.Reporting;

[DependsOn(
    typeof(ReportingDomainSharedModule),
    typeof(AbpDddApplicationContractsModule),
    typeof(AbpAuthorizationModule)
    )]
public class ReportingApplicationContractsModule : AbpModule
{

}
