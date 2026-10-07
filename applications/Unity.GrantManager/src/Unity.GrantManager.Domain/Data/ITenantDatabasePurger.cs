using System.Threading.Tasks;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.Data;

public interface ITenantDatabasePurger
{
    Task PurgeAsync(Tenant tenant, string licencePlate);
}
