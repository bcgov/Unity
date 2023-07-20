using Unity.GrantManager.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace Unity.GrantManager.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(GrantManagerEntityFrameworkCoreModule),
    typeof(GrantManagerApplicationContractsModule)
    )]
public class GrantManagerDbMigratorModule : AbpModule
{
}
