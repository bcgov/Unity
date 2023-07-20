using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Data;

/* This is used if database provider does't define
 * IGrantManagerDbSchemaMigrator implementation.
 */
public class NullGrantManagerDbSchemaMigrator : IGrantManagerDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
