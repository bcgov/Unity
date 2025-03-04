using Volo.Abp.Modularity;

namespace Unity.Reporting;

[DependsOn(
    typeof(ReportingDomainModule),
    typeof(ReportingTestBaseModule)
)]
public class ReportingDomainTestModule : AbpModule
{

}
