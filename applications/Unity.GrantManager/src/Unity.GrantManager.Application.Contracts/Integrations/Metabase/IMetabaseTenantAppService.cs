using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Integrations.Metabase;

/// <summary>
/// Metabase status and Setup for one tenant, used by the Reporting tab of the tenant
/// Configuration modal. Setup also serves as Resync: it is idempotent, and on a tenant that is
/// already set up it updates the database connection details and adds any missing members.
/// </summary>
public interface IMetabaseTenantAppService : IApplicationService
{
    Task<MetabaseTenantStatusDto> GetAsync(Guid tenantId);

    /// <summary>
    /// Queues the Metabase post-creation step for the tenant and marks it Waiting.
    /// </summary>
    Task SetupAsync(Guid tenantId);
}
