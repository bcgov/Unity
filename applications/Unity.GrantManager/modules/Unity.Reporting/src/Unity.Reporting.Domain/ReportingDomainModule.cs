using Volo.Abp.Domain;
using Volo.Abp.Modularity;

namespace Unity.Reporting;

[DependsOn(
    typeof(AbpDddDomainModule),
    typeof(ReportingDomainSharedModule)
)]
public class ReportingDomainModule : AbpModule
{

}
