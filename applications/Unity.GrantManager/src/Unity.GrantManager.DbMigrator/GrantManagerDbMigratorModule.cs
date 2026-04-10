using Unity.AI;
using Unity.GrantManager.EntityFrameworkCore;
using Unity.Reporting;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace Unity.GrantManager.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(GrantManagerEntityFrameworkCoreModule),
    typeof(GrantManagerApplicationContractsModule),
    typeof(ReportingApplicationModule), // Needed to seed Reporting data
    typeof(AIApplicationModule)          // Needed to seed AI prompt data
    )]
public class GrantManagerDbMigratorModule : AbpModule
{
}
