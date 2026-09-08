using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Unity.GrantManager.Integrations.Exceptions;
using Unity.Modules.Shared.Http;

namespace Unity.GrantManager.Integrations.Metabase;

public class MetabaseApiClient(
    IResilientHttpRequest resilientHttpRequest,
    IEndpointManagementAppService endpointManagementAppService,
    IOptions<MetabaseOptions> options) : IMetabaseApiClient
{
    private const string ApiKeyHeader = "x-api-key";

    // Metabase's permissions/collection graph endpoints use an optimistic-concurrency "revision"
    // number - a PUT with a stale revision (because another tenant registration updated the graph
    // first) is rejected. Retry the whole read-mutate-write cycle against a freshly-fetched graph
    // rather than surfacing a transient conflict as a permanent failure.
    private const int MaxGraphUpdateAttempts = 3;

    // Right after POST /api/database creates a connection, Metabase validates/establishes it
    // asynchronously in the background - calling sync_schema or rescan_values immediately after
    // can transiently 422 ("Looks like your Password is incorrect") even though the connection is
    // actually fine, simply because Metabase's own internal check hasn't settled yet. That status
    // code isn't one ResilientHttpRequest's Polly pipeline retries (only 429/5xx), so retry it here
    // (same immediate-retry approach as UpdateGraphWithRetryAsync below - no artificial delay).
    private const int MaxDatabaseSyncAttempts = 3;

    public async Task<int> FindOrCreateDatabaseAsync(string name, string host, int port, string dbName, string username, string password, bool ssl, CancellationToken cancellationToken = default)
    {
        var existingId = await FindIdByNameAsync("/api/database", name, cancellationToken);
        if (existingId != null)
        {
            return existingId.Value;
        }

        var body = new
        {
            engine = "postgres",
            name,
            is_full_sync = true,
            details = new { host, port, dbname = dbName, user = username, password, ssl }
        };
        var result = await PostAsync("/api/database", body, cancellationToken);
        return result.Value<int>("id");
    }

    public Task SyncDatabaseSchemaAsync(int databaseId, CancellationToken cancellationToken = default) =>
        PostWithRetryAsync($"/api/database/{databaseId}/sync_schema", cancellationToken);

    public Task RescanDatabaseValuesAsync(int databaseId, CancellationToken cancellationToken = default) =>
        PostWithRetryAsync($"/api/database/{databaseId}/rescan_values", cancellationToken);

    private async Task PostWithRetryAsync(string path, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await PostVoidAsync(path, new { }, cancellationToken);
                return;
            }
            // Narrowed to the specific transient status described on MaxDatabaseSyncAttempts - a
            // 401/403/404/500/etc. here means real misconfiguration or an outage, and should fail
            // immediately rather than retrying (and delaying) a failure that won't resolve itself.
            catch (IntegrationServiceException ex) when (attempt < MaxDatabaseSyncAttempts
                && ex.StatusCode == HttpStatusCode.UnprocessableEntity)
            {
                // Loop around and retry immediately - see the comment on MaxDatabaseSyncAttempts.
            }
        }
    }

    public async Task<int> FindOrCreateGroupAsync(string name, CancellationToken cancellationToken = default)
    {
        var existingId = await FindIdByNameAsync("/api/permissions/group", name, cancellationToken);
        if (existingId != null)
        {
            return existingId.Value;
        }

        var result = await PostAsync("/api/permissions/group", new { name }, cancellationToken);
        return result.Value<int>("id");
    }

    public async Task<int?> FindUserIdByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var result = await GetAsync($"/api/user?query={Uri.EscapeDataString(email)}", cancellationToken);
        var match = result["data"]?
            .FirstOrDefault(u => string.Equals(u.Value<string>("email"), email, StringComparison.OrdinalIgnoreCase));
        return match?.Value<int>("id");
    }

    public async Task AddGroupMemberAsync(int groupId, int userId, CancellationToken cancellationToken = default)
    {
        // A rerun after a partial failure must not re-POST an existing membership - Metabase
        // treats that as a conflict rather than a no-op.
        if (await IsGroupMemberAsync(groupId, userId, cancellationToken))
        {
            return;
        }

        try
        {
            await PostVoidAsync("/api/permissions/membership", new { group_id = groupId, user_id = userId }, cancellationToken);
        }
        catch (IntegrationServiceException)
        {
            // The pre-check above is inherently racy (e.g. a concurrent/retried registration
            // adding the same membership between our check and this POST) and Metabase surfaces
            // a duplicate membership as a raw 500 (a DB unique-constraint violation), not a clean
            // 409 - so status code alone can't tell "already a member" apart from a real failure.
            // Re-checking membership after the POST fails is the reliable signal: if the user is
            // a member now, by whatever path, the desired end state is already achieved.
            if (!await IsGroupMemberAsync(groupId, userId, cancellationToken))
            {
                throw;
            }
        }
    }

    private async Task<bool> IsGroupMemberAsync(int groupId, int userId, CancellationToken cancellationToken)
    {
        var membershipsToken = await ReadJsonTokenAsync(
            await GetRawAsync("/api/permissions/membership", cancellationToken), "/api/permissions/membership");

        // /api/permissions/membership normally returns an object keyed by group id
        // ({"<groupId>": [...members...]}), but - like /api/permissions/group and /api/collection
        // (see the comment on FindIdByNameAsync) - Metabase can return a raw JSON array instead
        // (observed for a brand-new group with no memberships yet: []). GetAsync's hard cast to
        // JObject throws on that shape, so read the raw token and handle both here.
        var groupMembers = membershipsToken switch
        {
            JObject membershipsByGroup => membershipsByGroup[groupId.ToString(CultureInfo.InvariantCulture)] as JArray,
            JArray flatMemberships => flatMemberships,
            _ => null
        };

        return groupMembers?.Any(m => m.Value<int>("user_id") == userId) ?? false;
    }

    public Task GrantGroupDatabaseAccessAsync(int groupId, int databaseId, CancellationToken cancellationToken = default) =>
        UpdateGraphWithRetryAsync("/api/permissions/graph", groups =>
        {
            var groupKey = groupId.ToString();
            var groupNode = (JObject?)groups[groupKey] ?? new JObject();

            groupNode[databaseId.ToString()] = new JObject
            {
                ["view-data"] = "unrestricted",
                ["create-queries"] = "query-builder-and-native"
            };
            groups[groupKey] = groupNode;
        }, cancellationToken);

    public async Task<int> FindOrCreateCollectionAsync(string name, CancellationToken cancellationToken = default)
    {
        var existingId = await FindIdByNameAsync("/api/collection", name, cancellationToken);
        if (existingId != null)
        {
            return existingId.Value;
        }

        var result = await PostAsync("/api/collection", new { name, color = "#509EE3" }, cancellationToken);
        return result.Value<int>("id");
    }

    public Task GrantGroupCollectionAccessAsync(int groupId, int collectionId, CancellationToken cancellationToken = default) =>
        UpdateGraphWithRetryAsync("/api/collection/graph", groups =>
        {
            var groupKey = groupId.ToString();
            var groupNode = (JObject?)groups[groupKey] ?? new JObject();

            groupNode[collectionId.ToString()] = "write";
            groups[groupKey] = groupNode;
        }, cancellationToken);

    private async Task UpdateGraphWithRetryAsync(string graphPath, Action<JObject> applyMutation, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var graph = await GetAsync(graphPath, cancellationToken);
            var groups = (JObject?)graph["groups"] ?? new JObject();
            applyMutation(groups);

            var response = await PutRawAsync(graphPath,
                new { groups, revision = graph.Value<int>("revision") }, cancellationToken);

            if (response.IsSuccessStatusCode)
                return;

            var isRevisionConflict = response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.BadRequest;
            if (!isRevisionConflict || attempt >= MaxGraphUpdateAttempts)
            {
                var content = response.Content == null ? string.Empty : await response.Content.ReadAsStringAsync(cancellationToken);
                throw new IntegrationServiceException(
                    $"Metabase API call to '{graphPath}' failed with status {response.StatusCode}: {content}");
            }

            // Stale revision - another concurrent tenant registration updated the graph first.
            // Loop around to re-fetch the latest graph and reapply this mutation on top of it.
        }
    }

    private async Task<string> GetBaseUrlAsync() =>
        await endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.METABASE_API_BASE);

    private IReadOnlyDictionary<string, string> BuildHeaders() =>
        new Dictionary<string, string> { [ApiKeyHeader] = options.Value.ApiKey };

    private async Task<HttpResponseMessage> GetRawAsync(string path, CancellationToken cancellationToken)
    {
        var baseUrl = await GetBaseUrlAsync();
        return await resilientHttpRequest.HttpAsync(
            HttpMethod.Get, $"{baseUrl}{path}", extraHeaders: BuildHeaders(), cancellationToken: cancellationToken);
    }

    private async Task<JObject> GetAsync(string path, CancellationToken cancellationToken) =>
        await ReadJsonAsync(await GetRawAsync(path, cancellationToken), path);

    // Metabase's list endpoints are inconsistent about pagination - /api/database wraps results in
    // {"data": [...]}, while /api/permissions/group and /api/collection return a raw JSON array.
    // Handling both shapes here keeps the find-or-create callers simple.
    private async Task<int?> FindIdByNameAsync(string listPath, string name, CancellationToken cancellationToken)
    {
        var root = await ReadJsonTokenAsync(await GetRawAsync(listPath, cancellationToken), listPath);
        var items = root as JArray ?? root["data"] as JArray ?? new JArray();
        var match = items.FirstOrDefault(item => string.Equals(item.Value<string>("name"), name, StringComparison.Ordinal));
        return match?.Value<int>("id");
    }

    private async Task<JObject> PostAsync(string path, object body, CancellationToken cancellationToken)
    {
        var baseUrl = await GetBaseUrlAsync();
        var response = await resilientHttpRequest.HttpAsync(
            HttpMethod.Post, $"{baseUrl}{path}", body, extraHeaders: BuildHeaders(), cancellationToken: cancellationToken);
        return await ReadJsonAsync(response, path);
    }

    // For POST calls whose response body the caller doesn't need (only whether it succeeded).
    // Some of Metabase's write endpoints - like /api/permissions/membership - return a raw JSON
    // array in the response body rather than an object (the same list-endpoint inconsistency
    // documented on FindIdByNameAsync, just showing up on a POST response instead of a GET). Since
    // these callers only care about success/failure, read via ReadJsonTokenAsync (which accepts
    // either shape and still throws on a non-success status) instead of PostAsync's hard JObject
    // cast, and discard the parsed body entirely.
    private async Task PostVoidAsync(string path, object body, CancellationToken cancellationToken)
    {
        var baseUrl = await GetBaseUrlAsync();
        var response = await resilientHttpRequest.HttpAsync(
            HttpMethod.Post, $"{baseUrl}{path}", body, extraHeaders: BuildHeaders(), cancellationToken: cancellationToken);
        await ReadJsonTokenAsync(response, path);
    }

    private async Task<HttpResponseMessage> PutRawAsync(string path, object body, CancellationToken cancellationToken)
    {
        var baseUrl = await GetBaseUrlAsync();
        return await resilientHttpRequest.HttpAsync(
            HttpMethod.Put, $"{baseUrl}{path}", body, extraHeaders: BuildHeaders(), cancellationToken: cancellationToken);
    }

    private static async Task<JObject> ReadJsonAsync(HttpResponseMessage response, string path) =>
        (JObject)await ReadJsonTokenAsync(response, path);

    private static async Task<JToken> ReadJsonTokenAsync(HttpResponseMessage response, string path)
    {
        var content = response.Content == null ? string.Empty : await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new IntegrationServiceException(
                $"Metabase API call to '{path}' failed with status {response.StatusCode}: {content}")
            {
                StatusCode = response.StatusCode
            };
        }

        return string.IsNullOrWhiteSpace(content) ? new JObject() : JToken.Parse(content);
    }
}
