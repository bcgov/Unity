using System;
using System.Collections.Generic;
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

    public async Task<int> CreateDatabaseAsync(string name, string host, int port, string dbName, string username, string password, bool ssl, CancellationToken cancellationToken = default)
    {
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
        PostAsync($"/api/database/{databaseId}/sync_schema", new { }, cancellationToken);

    public Task RescanDatabaseValuesAsync(int databaseId, CancellationToken cancellationToken = default) =>
        PostAsync($"/api/database/{databaseId}/rescan_values", new { }, cancellationToken);

    public async Task<int> CreateGroupAsync(string name, CancellationToken cancellationToken = default)
    {
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

    public Task AddGroupMemberAsync(int groupId, int userId, CancellationToken cancellationToken = default) =>
        PostAsync("/api/permissions/membership", new { group_id = groupId, user_id = userId }, cancellationToken);

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

    public async Task<int> CreateCollectionAsync(string name, CancellationToken cancellationToken = default)
    {
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

    private async Task<JObject> GetAsync(string path, CancellationToken cancellationToken)
    {
        var baseUrl = await GetBaseUrlAsync();
        var response = await resilientHttpRequest.HttpAsync(
            HttpMethod.Get, $"{baseUrl}{path}", extraHeaders: BuildHeaders(), cancellationToken: cancellationToken);
        return await ReadJsonAsync(response, path);
    }

    private async Task<JObject> PostAsync(string path, object body, CancellationToken cancellationToken)
    {
        var baseUrl = await GetBaseUrlAsync();
        var response = await resilientHttpRequest.HttpAsync(
            HttpMethod.Post, $"{baseUrl}{path}", body, extraHeaders: BuildHeaders(), cancellationToken: cancellationToken);
        return await ReadJsonAsync(response, path);
    }

    private async Task<HttpResponseMessage> PutRawAsync(string path, object body, CancellationToken cancellationToken)
    {
        var baseUrl = await GetBaseUrlAsync();
        return await resilientHttpRequest.HttpAsync(
            HttpMethod.Put, $"{baseUrl}{path}", body, extraHeaders: BuildHeaders(), cancellationToken: cancellationToken);
    }

    private static async Task<JObject> ReadJsonAsync(HttpResponseMessage response, string path)
    {
        var content = response.Content == null ? string.Empty : await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new IntegrationServiceException(
                $"Metabase API call to '{path}' failed with status {response.StatusCode}: {content}");
        }

        return string.IsNullOrWhiteSpace(content) ? new JObject() : JObject.Parse(content);
    }
}
