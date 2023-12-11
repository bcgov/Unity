using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.Data;

/* This is used if database provider does't define
 * IGrantManagerDbSchemaMigrator implementation.
 */
public class NullGrantManagerDbSchemaMigrator : IGrantManagerDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync(Tenant? tenant)
    {
        return Task.CompletedTask;
    }
}
