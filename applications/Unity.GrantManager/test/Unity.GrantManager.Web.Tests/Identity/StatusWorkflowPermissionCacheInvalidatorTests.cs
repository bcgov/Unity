using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.GrantManager.Permissions;
using Unity.GrantManager.Web.Identity;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Caching;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement;
using Volo.Abp.TenantManagement;
using Xunit;

namespace Unity.GrantManager.Identity;

public class StatusWorkflowPermissionCacheInvalidatorTests
{
    [Fact]
    public async Task Should_Invalidate_Workflow_Defaults_And_Retired_Grants_For_The_Host_And_Each_Tenant()
    {
        var firstTenant = new TestTenant(Guid.NewGuid(), "First");
        var secondTenant = new TestTenant(Guid.NewGuid(), "Second");
        var fixture = new Fixture(firstTenant, secondTenant);
        var callerTenantId = Guid.NewGuid();
        using var callerScope = fixture.CurrentTenant.Change(callerTenantId, "Caller");

        await fixture.Invalidator.InvalidateAsync();

        fixture.Removals.Select(request => request.TenantId)
            .ShouldBe(new Guid?[] { null, firstTenant.Id, secondTenant.Id });
        var expected = ExpectedCacheKeys().Order().ToArray();
        foreach (var request in fixture.Removals)
        {
            request.Keys.Order().ShouldBe(expected);
        }
        fixture.CurrentTenant.Id.ShouldBe(callerTenantId);
        fixture.CurrentTenant.Name.ShouldBe("Caller");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Should_Run_Startup_Cleanup_Only_When_Redis_Is_Enabled(bool redisEnabled)
    {
        var fixture = new Fixture(new TestTenant(Guid.NewGuid(), "Tenant"));
        using var provider = CreateStartupServices(fixture, redisEnabled);
        using var worker = CreateWorker(provider);

        await worker.StartAsync(CancellationToken.None);
        await worker.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));

        fixture.Removals.Count.ShouldBe(redisEnabled ? 2 : 0);
    }

    [Fact]
    public async Task Should_Restore_The_Calling_Tenant_When_A_Cleanup_Attempt_Fails()
    {
        var firstTenant = new TestTenant(Guid.NewGuid(), "First");
        var secondTenant = new TestTenant(Guid.NewGuid(), "Second");
        var fixture = new Fixture(firstTenant, secondTenant) { FailingTenantId = firstTenant.Id };
        var callerTenantId = Guid.NewGuid();
        using var callerScope = fixture.CurrentTenant.Change(callerTenantId, "Caller");

        await Should.ThrowAsync<InvalidOperationException>(() => fixture.Invalidator.InvalidateAsync());

        fixture.Removals.Select(request => request.TenantId).ShouldBe(new Guid?[] { null, firstTenant.Id });
        fixture.CurrentTenant.Id.ShouldBe(callerTenantId);
        fixture.CurrentTenant.Name.ShouldBe("Caller");
    }

    [Fact]
    public async Task Should_Not_Block_Startup_While_Cache_Removal_Is_Pending()
    {
        var pendingRemoval = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var fixture = new Fixture { PendingRemoval = pendingRemoval };
        using var provider = CreateStartupServices(fixture, redisEnabled: true);
        using var worker = CreateWorker(provider);

        await worker.StartAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        await fixture.FirstRemoval.Task.WaitAsync(TimeSpan.FromSeconds(5));

        worker.ExecuteTask!.IsCompleted.ShouldBeFalse();
        pendingRemoval.SetResult();
        await worker.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Should_Log_A_Warning_And_Retry_A_Failed_Cache_Cleanup()
    {
        var fixture = new Fixture { FailuresRemaining = 1 };
        var logger = new RecordingLogger();
        using var provider = CreateStartupServices(fixture, redisEnabled: true);
        using var worker = CreateWorker(provider, logger);

        await worker.StartAsync(CancellationToken.None);
        await worker.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));

        fixture.Removals.Count.ShouldBe(2);
        fixture.FailuresRemaining.ShouldBe(0);
        logger.LogLevels.ShouldContain(LogLevel.Warning);
    }

    [Fact]
    public async Task Should_Cancel_A_Pending_Retry_When_The_Host_Stops()
    {
        var fixture = new Fixture { FailuresRemaining = int.MaxValue };
        using var provider = CreateStartupServices(fixture, redisEnabled: true);
        using var worker = CreateWorker(provider, retryDelay: TimeSpan.FromHours(1));
        await worker.StartAsync(CancellationToken.None);
        await fixture.FirstRemoval.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await worker.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));

        worker.ExecuteTask!.IsCompletedSuccessfully.ShouldBeTrue();
        fixture.Removals.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Repeat_Host_Cleanup_On_Subsequent_Starts_When_There_Are_No_Tenants()
    {
        var fixture = new Fixture();

        await fixture.Invalidator.InvalidateAsync();
        await fixture.Invalidator.InvalidateAsync();

        fixture.Removals.Count.ShouldBe(2);
        fixture.Removals.ShouldAllBe(request => request.TenantId == null);
        fixture.Removals[0].Keys.Order().ShouldBe(ExpectedCacheKeys().Order());
        fixture.Removals[1].Keys.ShouldBe(fixture.Removals[0].Keys);
    }

    private static ServiceProvider CreateStartupServices(Fixture fixture, bool redisEnabled)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Redis:IsEnabled"] = redisEnabled.ToString() }).Build());
        services.AddSingleton(fixture.Invalidator);
        return services.BuildServiceProvider();
    }

    private static StatusWorkflowPermissionCacheCleanupService CreateWorker(ServiceProvider provider,
        ILogger<StatusWorkflowPermissionCacheCleanupService>? logger = null, TimeSpan? retryDelay = null) =>
        new(provider.GetRequiredService<IServiceScopeFactory>(), provider.GetRequiredService<IConfiguration>(),
            logger ?? NullLogger<StatusWorkflowPermissionCacheCleanupService>.Instance, retryDelay ?? TimeSpan.Zero);

    private static IEnumerable<string> ExpectedCacheKeys()
    {
        // Clear every workflow default that seeding can restore, plus the two retired Assessor grants.
        // Custom roles, direct user assignments, and unrelated permission keys stay cached.
        (string Role, string Permission)[] affectedPermissions =
        [
            (UnityRoles.ProgramManager, GrantApplicationPermissions.StatusWorkflow.Default),
            (UnityRoles.ProgramManager, GrantApplicationPermissions.Assignments.AssignInitial),
            (UnityRoles.ProgramManager, GrantApplicationPermissions.Reviews.StartInitial),
            (UnityRoles.ProgramManager, GrantApplicationPermissions.Reviews.CompleteInitial),
            (UnityRoles.ProgramManager, GrantApplicationPermissions.Assessments.Start),
            (UnityRoles.ProgramManager, GrantApplicationPermissions.Assessments.Complete),
            (UnityRoles.TeamLead, GrantApplicationPermissions.StatusWorkflow.Default),
            (UnityRoles.TeamLead, GrantApplicationPermissions.Assignments.AssignInitial),
            (UnityRoles.TeamLead, GrantApplicationPermissions.Reviews.StartInitial),
            (UnityRoles.TeamLead, GrantApplicationPermissions.Reviews.CompleteInitial),
            (UnityRoles.TeamLead, GrantApplicationPermissions.Approvals.DeferAfterApproval),
            (UnityRoles.TeamLead, GrantApplicationPermissions.Approvals.BulkApplicationApproval),
            (UnityRoles.TeamLead, GrantApplicationPermissions.Assessments.Start),
            (UnityRoles.TeamLead, GrantApplicationPermissions.Assessments.Complete),
            (UnityRoles.Reviewer, GrantApplicationPermissions.StatusWorkflow.Default),
            (UnityRoles.Reviewer, GrantApplicationPermissions.Assignments.AssignInitial),
            (UnityRoles.Reviewer, GrantApplicationPermissions.Reviews.StartInitial),
            (UnityRoles.Reviewer, GrantApplicationPermissions.Reviews.CompleteInitial),
            (UnityRoles.ExternalAssessor, GrantApplicationPermissions.StatusWorkflow.Default),
            (UnityRoles.ExternalAssessor, GrantApplicationPermissions.Assignments.AssignInitial),
            (UnityRoles.ExternalAssessor, GrantApplicationPermissions.Assessments.Start),
            (UnityRoles.ExternalAssessor, GrantApplicationPermissions.Assessments.Complete),
            (UnityRoles.Assessor, GrantApplicationPermissions.StatusWorkflow.Default),
            (UnityRoles.Assessor, GrantApplicationPermissions.Assignments.AssignInitial),
            (UnityRoles.Assessor, GrantApplicationPermissions.Assessments.Start),
            (UnityRoles.Assessor, GrantApplicationPermissions.Assessments.Complete),
            (UnityRoles.Assessor, GrantApplicationPermissions.Reviews.StartInitial),
            (UnityRoles.Assessor, GrantApplicationPermissions.Reviews.CompleteInitial),
            (UnityRoles.Approver, GrantApplicationPermissions.StatusWorkflow.Default),
            (UnityRoles.Approver, GrantApplicationPermissions.Assignments.AssignInitial),
            (UnityRoles.Approver, GrantApplicationPermissions.Approvals.Complete),
            (UnityRoles.Approver, GrantApplicationPermissions.Approvals.DeferAfterApproval)
        ];
        return affectedPermissions.Select(change => PermissionGrantCacheItem.CalculateCacheKey(
            change.Permission, RolePermissionValueProvider.ProviderName, change.Role));
    }

    private sealed class RecordingLogger : ILogger<StatusWorkflowPermissionCacheCleanupService>
    {
        public List<LogLevel> LogLevels { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => LogLevels.Add(logLevel);
    }

    private sealed class Fixture
    {
        public ICurrentTenant CurrentTenant { get; } = new CurrentTenant(AsyncLocalCurrentTenantAccessor.Instance);
        public List<(Guid? TenantId, string[] Keys)> Removals { get; } = new();
        public Guid? FailingTenantId { get; init; }
        public int FailuresRemaining { get; set; }
        public TaskCompletionSource? PendingRemoval { get; init; }
        public TaskCompletionSource FirstRemoval { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public StatusWorkflowPermissionCacheInvalidator Invalidator { get; }

        public Fixture(params Tenant[] tenants)
        {
            var repository = Substitute.For<ITenantRepository>();
            repository.GetListAsync(includeDetails: false, cancellationToken: Arg.Any<CancellationToken>()).Returns(_ =>
            {
                CurrentTenant.Id.ShouldBeNull();
                return Task.FromResult(tenants.ToList());
            });
            var cache = Substitute.For<IDistributedCache<PermissionGrantCacheItem>>();
            cache.RemoveManyAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<bool?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    call.ArgAt<bool?>(1).ShouldBe(false);
                    call.ArgAt<bool>(2).ShouldBeFalse();
                    Removals.Add((CurrentTenant.Id, call.ArgAt<IEnumerable<string>>(0).ToArray()));
                    FirstRemoval.TrySetResult();
                    if (FailuresRemaining > 0)
                    {
                        FailuresRemaining--;
                        return Task.FromException(new InvalidOperationException("Cache unavailable"));
                    }
                    if (FailingTenantId.HasValue && CurrentTenant.Id == FailingTenantId)
                    {
                        return Task.FromException(new InvalidOperationException("Cache unavailable"));
                    }
                    return PendingRemoval?.Task.WaitAsync(call.ArgAt<CancellationToken>(3)) ?? Task.CompletedTask;
                });
            Invalidator = new StatusWorkflowPermissionCacheInvalidator(cache, repository, CurrentTenant,
                NullLogger<StatusWorkflowPermissionCacheInvalidator>.Instance);
        }
    }

    private sealed class TestTenant : Tenant
    {
        public TestTenant(Guid id, string name) : base(id, name, name.ToUpperInvariant())
        {
        }
    }
}
