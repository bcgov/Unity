using System.Threading;
using System.Threading.Tasks;

namespace Unity.GrantManager.Integrations.Metabase;

/// <summary>
/// Thin wrapper over the Metabase admin REST API, covering the same steps as the
/// manual_deploy_new_metabase_tenant.ps1 runbook: create a database connection for the tenant's
/// readonly Postgres role, create a permissions group and add members to it, grant the group
/// access to the database, and create/grant a collection.
/// </summary>
public interface IMetabaseApiClient
{
    Task<int> CreateDatabaseAsync(string name, string host, int port, string dbName, string username, string password, CancellationToken cancellationToken = default);
    Task SyncDatabaseSchemaAsync(int databaseId, CancellationToken cancellationToken = default);
    Task RescanDatabaseValuesAsync(int databaseId, CancellationToken cancellationToken = default);
    Task<int> CreateGroupAsync(string name, CancellationToken cancellationToken = default);
    Task<int?> FindUserIdByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddGroupMemberAsync(int groupId, int userId, CancellationToken cancellationToken = default);
    Task GrantGroupDatabaseAccessAsync(int groupId, int databaseId, CancellationToken cancellationToken = default);
    Task<int> CreateCollectionAsync(string name, CancellationToken cancellationToken = default);
    Task GrantGroupCollectionAccessAsync(int groupId, int collectionId, CancellationToken cancellationToken = default);
}
