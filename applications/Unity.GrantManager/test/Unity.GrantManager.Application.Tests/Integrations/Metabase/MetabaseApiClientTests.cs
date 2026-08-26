using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Unity.GrantManager.Integrations.Exceptions;
using Unity.Modules.Shared.Http;
using Xunit;

namespace Unity.GrantManager.Integrations.Metabase;

public class MetabaseApiClientTests
{
    private const string BaseUrl = "https://metabase.example";

    private static (MetabaseApiClient Client, IResilientHttpRequest Http) CreateClient()
    {
        var http = Substitute.For<IResilientHttpRequest>();
        var endpointService = Substitute.For<IEndpointManagementAppService>();
        endpointService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.METABASE_API_BASE).Returns(BaseUrl);
        var options = Options.Create(new MetabaseOptions { ApiKey = "test-api-key" });

        return (new MetabaseApiClient(http, endpointService, options), http);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static void SetupHttpSequence(IResilientHttpRequest http, params HttpResponseMessage[] responses) =>
        http.HttpAsync(
                Arg.Any<HttpMethod>(), Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<string?>(),
                Arg.Any<(string username, string password)?>(), Arg.Any<HttpCompletionOption>(),
                Arg.Any<System.Collections.Generic.IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
            .Returns(responses[0], responses[1..]);

    [Fact]
    public async Task GrantGroupDatabaseAccessAsync_NoConflict_SucceedsOnFirstAttempt()
    {
        var (client, http) = CreateClient();
        SetupHttpSequence(http,
            JsonResponse(HttpStatusCode.OK, "{\"groups\":{},\"revision\":1}"),
            JsonResponse(HttpStatusCode.OK, "{}"));

        await client.GrantGroupDatabaseAccessAsync(groupId: 5, databaseId: 11);

        await http.Received(2).HttpAsync(
            Arg.Any<HttpMethod>(), Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<string?>(),
            Arg.Any<(string username, string password)?>(), Arg.Any<HttpCompletionOption>(),
            Arg.Any<System.Collections.Generic.IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GrantGroupDatabaseAccessAsync_StaleRevisionOnFirstPut_RefetchesGraphAndRetries()
    {
        var (client, http) = CreateClient();
        SetupHttpSequence(http,
            JsonResponse(HttpStatusCode.OK, "{\"groups\":{},\"revision\":1}"),       // GET #1
            JsonResponse(HttpStatusCode.Conflict, "{\"message\":\"stale\"}"),        // PUT #1 - stale revision
            JsonResponse(HttpStatusCode.OK, "{\"groups\":{},\"revision\":2}"),       // GET #2 (retry re-fetch)
            JsonResponse(HttpStatusCode.OK, "{}"));                                   // PUT #2 - succeeds

        await Should.NotThrowAsync(() => client.GrantGroupDatabaseAccessAsync(groupId: 5, databaseId: 11));

        await http.Received(4).HttpAsync(
            Arg.Any<HttpMethod>(), Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<string?>(),
            Arg.Any<(string username, string password)?>(), Arg.Any<HttpCompletionOption>(),
            Arg.Any<System.Collections.Generic.IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GrantGroupDatabaseAccessAsync_ConflictOnEveryAttempt_ThrowsAfterMaxAttempts()
    {
        var (client, http) = CreateClient();
        // 3 attempts allowed: GET/PUT-conflict x3 (6 calls total), all conflicting.
        SetupHttpSequence(http,
            JsonResponse(HttpStatusCode.OK, "{\"groups\":{},\"revision\":1}"),
            JsonResponse(HttpStatusCode.Conflict, "{}"),
            JsonResponse(HttpStatusCode.OK, "{\"groups\":{},\"revision\":2}"),
            JsonResponse(HttpStatusCode.Conflict, "{}"),
            JsonResponse(HttpStatusCode.OK, "{\"groups\":{},\"revision\":3}"),
            JsonResponse(HttpStatusCode.Conflict, "{}"));

        await Should.ThrowAsync<IntegrationServiceException>(
            () => client.GrantGroupDatabaseAccessAsync(groupId: 5, databaseId: 11));

        await http.Received(6).HttpAsync(
            Arg.Any<HttpMethod>(), Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<string?>(),
            Arg.Any<(string username, string password)?>(), Arg.Any<HttpCompletionOption>(),
            Arg.Any<System.Collections.Generic.IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GrantGroupDatabaseAccessAsync_NonConflictFailure_ThrowsWithoutRetrying()
    {
        var (client, http) = CreateClient();
        SetupHttpSequence(http,
            JsonResponse(HttpStatusCode.OK, "{\"groups\":{},\"revision\":1}"),
            JsonResponse(HttpStatusCode.InternalServerError, "{}"));

        await Should.ThrowAsync<IntegrationServiceException>(
            () => client.GrantGroupDatabaseAccessAsync(groupId: 5, databaseId: 11));

        // Only the initial GET + PUT - a non-conflict failure isn't retried at this layer.
        await http.Received(2).HttpAsync(
            Arg.Any<HttpMethod>(), Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<string?>(),
            Arg.Any<(string username, string password)?>(), Arg.Any<HttpCompletionOption>(),
            Arg.Any<System.Collections.Generic.IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GrantGroupCollectionAccessAsync_StaleRevisionOnFirstPut_RefetchesGraphAndRetries()
    {
        var (client, http) = CreateClient();
        SetupHttpSequence(http,
            JsonResponse(HttpStatusCode.OK, "{\"groups\":{},\"revision\":1}"),
            JsonResponse(HttpStatusCode.BadRequest, "{}"),
            JsonResponse(HttpStatusCode.OK, "{\"groups\":{},\"revision\":2}"),
            JsonResponse(HttpStatusCode.OK, "{}"));

        await Should.NotThrowAsync(() => client.GrantGroupCollectionAccessAsync(groupId: 5, collectionId: 22));
    }

    // /api/database wraps its list in {"data": [...]}.
    [Fact]
    public async Task FindOrCreateDatabaseAsync_DatabaseWithSameNameAlreadyExists_ReturnsExistingIdWithoutCreating()
    {
        var (client, http) = CreateClient();
        SetupHttpSequence(http,
            JsonResponse(HttpStatusCode.OK, "{\"data\":[{\"id\":11,\"name\":\"AG-MARB\"}]}"));

        var databaseId = await client.FindOrCreateDatabaseAsync(
            "AG-MARB", "host", 5432, "db", "user", "pass", ssl: true);

        databaseId.ShouldBe(11);
        await http.Received(1).HttpAsync(
            Arg.Any<HttpMethod>(), Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<string?>(),
            Arg.Any<(string username, string password)?>(), Arg.Any<HttpCompletionOption>(),
            Arg.Any<System.Collections.Generic.IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindOrCreateDatabaseAsync_NoDatabaseWithThatName_CreatesNewDatabase()
    {
        var (client, http) = CreateClient();
        SetupHttpSequence(http,
            JsonResponse(HttpStatusCode.OK, "{\"data\":[]}"),
            JsonResponse(HttpStatusCode.OK, "{\"id\":12}"));

        var databaseId = await client.FindOrCreateDatabaseAsync(
            "AG-MARB", "host", 5432, "db", "user", "pass", ssl: true);

        databaseId.ShouldBe(12);
        await http.Received(1).HttpAsync(
            HttpMethod.Post, Arg.Is<string>(url => url != null && url.EndsWith("/api/database", StringComparison.Ordinal)),
            Arg.Any<object?>(), Arg.Any<string?>(), Arg.Any<(string username, string password)?>(),
            Arg.Any<HttpCompletionOption>(), Arg.Any<System.Collections.Generic.IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>());
    }

    // /api/permissions/group returns a raw JSON array, unlike /api/database.
    [Fact]
    public async Task FindOrCreateGroupAsync_GroupWithSameNameAlreadyExists_ReturnsExistingIdWithoutCreating()
    {
        var (client, http) = CreateClient();
        SetupHttpSequence(http,
            JsonResponse(HttpStatusCode.OK, "[{\"id\":7,\"name\":\"AG-MARB\"}]"));

        var groupId = await client.FindOrCreateGroupAsync("AG-MARB");

        groupId.ShouldBe(7);
        await http.Received(1).HttpAsync(
            Arg.Any<HttpMethod>(), Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<string?>(),
            Arg.Any<(string username, string password)?>(), Arg.Any<HttpCompletionOption>(),
            Arg.Any<System.Collections.Generic.IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>());
    }

    // /api/collection also returns a raw JSON array.
    [Fact]
    public async Task FindOrCreateCollectionAsync_CollectionWithSameNameAlreadyExists_ReturnsExistingIdWithoutCreating()
    {
        var (client, http) = CreateClient();
        SetupHttpSequence(http,
            JsonResponse(HttpStatusCode.OK, "[{\"id\":33,\"name\":\"AG-MARB\"}]"));

        var collectionId = await client.FindOrCreateCollectionAsync("AG-MARB");

        collectionId.ShouldBe(33);
        await http.Received(1).HttpAsync(
            Arg.Any<HttpMethod>(), Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<string?>(),
            Arg.Any<(string username, string password)?>(), Arg.Any<HttpCompletionOption>(),
            Arg.Any<System.Collections.Generic.IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddGroupMemberAsync_UserAlreadyAMember_DoesNotPostMembershipAgain()
    {
        var (client, http) = CreateClient();
        SetupHttpSequence(http,
            JsonResponse(HttpStatusCode.OK, "{\"5\":[{\"user_id\":101,\"membership_id\":1}]}"));

        await client.AddGroupMemberAsync(groupId: 5, userId: 101);

        await http.Received(1).HttpAsync(
            Arg.Any<HttpMethod>(), Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<string?>(),
            Arg.Any<(string username, string password)?>(), Arg.Any<HttpCompletionOption>(),
            Arg.Any<System.Collections.Generic.IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddGroupMemberAsync_UserNotYetAMember_PostsMembership()
    {
        var (client, http) = CreateClient();
        SetupHttpSequence(http,
            JsonResponse(HttpStatusCode.OK, "{\"5\":[{\"user_id\":101,\"membership_id\":1}]}"),
            JsonResponse(HttpStatusCode.OK, "{}"));

        await client.AddGroupMemberAsync(groupId: 5, userId: 202);

        await http.Received(1).HttpAsync(
            HttpMethod.Post, Arg.Is<string>(url => url != null && url.EndsWith("/api/permissions/membership", StringComparison.Ordinal)),
            Arg.Any<object?>(), Arg.Any<string?>(), Arg.Any<(string username, string password)?>(),
            Arg.Any<HttpCompletionOption>(), Arg.Any<System.Collections.Generic.IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>());
    }
}
