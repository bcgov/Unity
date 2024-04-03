using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Unity.GrantManager.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.EntityFrameworkCore;

public class EntityFrameworkCoreGrantManagerDbSchemaMigrator
    : IGrantManagerDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreGrantManagerDbSchemaMigrator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync(Tenant? tenant)
    {
        /* We intentionally resolving the GrantManagerDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        if (tenant != null)
        {
            var connectionString = tenant.ConnectionStrings[0];
            if (connectionString != null)
            {
                var tenantDb = _serviceProvider
                .GetRequiredService<GrantTenantDbContext>()
                .Database;

                tenantDb.SetConnectionString(connectionString.Value);
                await tenantDb.MigrateAsync();

                /* The payments module is also migrated.
                   Currently the payments module also reference the tenant connection string.
                   Changes to that, may require an inspection in the connections string here and resolve the correct one.
                */
            }
        }
        else
        {
            await _serviceProvider
                .GetRequiredService<GrantManagerDbContext>()
                .Database
                .MigrateAsync();
        }
    }
}
