using System;
using System.Threading.Tasks;

namespace Unity.TenantManagement;

public interface ITenantPurgeProvider
{
    Task PurgeAsync(Guid tenantId, string licencePlate);
}
