using Unity.AI;
using Unity.GrantManager.EntityFrameworkCore;
using Unity.Reporting;
using Unity.TenantManagement;
using Volo.Abp.Autofac;
using Volo.Abp.Http.Client;
using Volo.Abp.Modularity;

namespace Unity.GrantManager.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpHttpClientModule),
    typeof(GrantManagerEntityFrameworkCoreModule),
    typeof(GrantManagerApplicationContractsModule),
    typeof(ReportingApplicationModule), // Needed to seed Reporting data
    typeof(AIApplicationModule),         // Needed to seed AI prompt data
    typeof(UnityTenantManagementApplicationModule) // Registers EncryptedTenantConnectionStringResolver — without it,
                                                     // tenant connection strings are resolved undecrypted (stock ABP
                                                     // resolver), breaking every standard-repository DB call for tenants.
    )]
public class GrantManagerDbMigratorModule : AbpModule
{
}
