using System.Threading.Tasks;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.Data;

public interface IGrantManagerDbSchemaMigrator
{
    Task MigrateAsync(Tenant? tenant);
}
