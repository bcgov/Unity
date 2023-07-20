using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Unity.GrantManager.Data;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.EntityFrameworkCore;

public class EntityFrameworkCoreGrantManagerDbSchemaMigrator
    : IGrantManagerDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreGrantManagerDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolving the GrantManagerDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<GrantManagerDbContext>()
            .Database
            .MigrateAsync();
    }
}
