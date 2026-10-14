using System.Threading;
using System.Threading.Tasks;

namespace Unity.GrantManager.Integrations.Metabase;

/// <summary>
/// Thin wrapper over the Metabase admin REST API, covering the same steps as the
/// manual_deploy_new_metabase_tenant.ps1 runbook: create a database connection for the tenant's
/// readonly Postgres role, create a permissions group and add members to it, grant the group
/// access to the database, and create/grant a collection.
///
/// The <c>FindOrCreate*</c> methods and <see cref="AddGroupMemberAsync"/> are idempotent - they
/// look for an existing database/group/collection/membership by its natural key (name, or
/// group+user) before creating one, so the caller can safely retry after a partial failure
/// (e.g. re-enqueuing <c>MetabaseTenantRegistrationStep</c>) without duplicating resources.
/// </summary>
public interface IMetabaseApiClient
{
    /// <summary>
    /// The Metabase site URL (the METABASE_API_BASE endpoint), for links into Metabase.
    /// </summary>
    Task<string> GetSiteUrlAsync();

    /// <summary>
    /// Calls <c>GET /api/user/current</c> with the configured API key. Returns whether the key
    /// belongs to a Metabase administrator; throws when Metabase is unreachable or rejects the key.
    /// </summary>
    Task<bool> CheckConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the database connection by name, or creates it. When it already exists, its
    /// connection details are updated (<see cref="UpdateDatabaseDetailsAsync"/>) so a changed
    /// password or host is picked up on a rerun.
    /// </summary>
    Task<int> FindOrCreateDatabaseAsync(string name, string host, int port, string dbName, string username, string password, bool ssl, CancellationToken cancellationToken = default);
    Task UpdateDatabaseDetailsAsync(int databaseId, string host, int port, string dbName, string username, string password, bool ssl, CancellationToken cancellationToken = default);
    Task SyncDatabaseSchemaAsync(int databaseId, CancellationToken cancellationToken = default);
    Task RescanDatabaseValuesAsync(int databaseId, CancellationToken cancellationToken = default);
    Task<int> FindOrCreateGroupAsync(string name, CancellationToken cancellationToken = default);
    Task<int?> FindUserIdByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddGroupMemberAsync(int groupId, int userId, CancellationToken cancellationToken = default);
    Task GrantGroupDatabaseAccessAsync(int groupId, int databaseId, CancellationToken cancellationToken = default);
    Task<int> FindOrCreateCollectionAsync(string name, CancellationToken cancellationToken = default);
    Task GrantGroupCollectionAccessAsync(int groupId, int collectionId, CancellationToken cancellationToken = default);
}
