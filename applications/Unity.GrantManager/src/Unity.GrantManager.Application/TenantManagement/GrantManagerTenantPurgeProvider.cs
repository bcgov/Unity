using System;
using System.Threading.Tasks;
using Unity.GrantManager.Data;
using Unity.TenantManagement;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.TenantManagement;

[ExposeServices(typeof(ITenantPurgeProvider))]
public class GrantManagerTenantPurgeProvider(
    ITenantRepository tenantRepository,
    IDataFilter dataFilter,
    ITenantDatabasePurger tenantDatabasePurger)
    : ITenantPurgeProvider, ITransientDependency
{
    public async Task PurgeAsync(Guid tenantId, string licencePlate)
    {
        Tenant? tenant;
        using (dataFilter.Disable<ISoftDelete>())
        {
            tenant = await tenantRepository.FindAsync(tenantId, includeDetails: true);
        }

        if (tenant == null)
        {
            throw new UserFriendlyException("Tenant not found.");
        }

        await tenantDatabasePurger.PurgeAsync(tenant, licencePlate);
    }
}
