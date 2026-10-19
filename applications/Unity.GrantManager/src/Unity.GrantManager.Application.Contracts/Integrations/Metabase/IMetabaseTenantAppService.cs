using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Integrations.Metabase;

/// <summary>
/// Metabase status and Setup for one tenant, used by the Reporting tab of the tenant
/// Configuration modal. Setup also serves as Resync: it is idempotent, and on a tenant that is
/// already set up it updates the database connection details, renames the stored database, group
/// and collection to the tenant's current name, and adds any missing members.
/// </summary>
public interface IMetabaseTenantAppService : IApplicationService
{
    Task<MetabaseTenantStatusDto> GetAsync(Guid tenantId);

    /// <summary>
    /// Queues the Metabase post-creation step for the tenant and marks it Waiting.
    /// </summary>
    Task SetupAsync(Guid tenantId);

    /// <summary>
    /// Queues Resync (as <see cref="SetupAsync"/>) when the tenant is already set up in Metabase
    /// and Setup can run, so a renamed tenant's database, group and collection are renamed to
    /// match. Does nothing otherwise. Returns whether it queued.
    /// </summary>
    Task<bool> ResyncIfSetUpAsync(Guid tenantId);

    /// <summary>
    /// Asks Metabase to re-read the tenant database's tables, views and columns, so newly
    /// generated reporting views show up. Metabase runs it in the background.
    /// </summary>
    Task SyncSchemaAsync(Guid tenantId);

    /// <summary>
    /// Asks Metabase to re-read the distinct field values used by filters. Metabase runs it in
    /// the background.
    /// </summary>
    Task RescanValuesAsync(Guid tenantId);
}
