using Volo.Abp.Modularity;

namespace Unity.Reporting;

[DependsOn(typeof(ReportingApplicationModule))]
public class ReportingApplicationTestModule : AbpModule
{

}
