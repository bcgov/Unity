using Medallion.Threading;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.GrantApplications.Automation;
using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.DistributedLocking;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.GrantApplications.Automation;

public class AIGenerationQueueTests(ITestOutputHelper outputHelper) : GrantManagerApplicationTestBase(outputHelper)
{
    [Fact]
    public async Task QueueAllAIStagesAsync_Should_Enqueue_All_Stage_Jobs_When_None_Exists()
    {
        var applicationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var repository = Substitute.For<IRepository<AIGenerationRequest, Guid>>();
        repository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIGenerationRequest>>(Array.Empty<AIGenerationRequest>().AsQueryable()));
        repository.InsertAsync(Arg.Any<AIGenerationRequest>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<AIGenerationRequest>()));

        var backgroundJobManager = Substitute.For<IBackgroundJobManager>();
        backgroundJobManager.EnqueueAsync<GenerateAttachmentSummaryBackgroundJobArgs>(
                Arg.Any<GenerateAttachmentSummaryBackgroundJobArgs>(),
                Arg.Any<BackgroundJobPriority>(),
                Arg.Any<TimeSpan?>())
            .Returns(Task.FromResult(string.Empty));
        backgroundJobManager.EnqueueAsync<GenerateApplicationAnalysisBackgroundJobArgs>(
                Arg.Any<GenerateApplicationAnalysisBackgroundJobArgs>(),
                Arg.Any<BackgroundJobPriority>(),
                Arg.Any<TimeSpan?>())
            .Returns(Task.FromResult(string.Empty));
        backgroundJobManager.EnqueueAsync<GenerateApplicationScoringBackgroundJobArgs>(
                Arg.Any<GenerateApplicationScoringBackgroundJobArgs>(),
                Arg.Any<BackgroundJobPriority>(),
                Arg.Any<TimeSpan?>())
            .Returns(Task.FromResult(string.Empty));

        var queue = new ApplicationAIGenerationQueue(backgroundJobManager, repository, new TestDistributedLockProvider());

        await queue.QueueAllAIStagesAsync(applicationId, tenantId, "v1");

        await backgroundJobManager.Received(1).EnqueueAsync(Arg.Any<GenerateAttachmentSummaryBackgroundJobArgs>(), Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>());
        await backgroundJobManager.Received(1).EnqueueAsync(Arg.Any<GenerateApplicationAnalysisBackgroundJobArgs>(), Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>());
        await backgroundJobManager.Received(1).EnqueueAsync(Arg.Any<GenerateApplicationScoringBackgroundJobArgs>(), Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>());
        await repository.Received(3).InsertAsync(Arg.Any<AIGenerationRequest>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueueApplicationAnalysisAsync_Should_Not_Enqueue_When_An_Active_Request_Already_Exists()
    {
        var tenantId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var promptVersion = "v1";
        var requestKey = AIGenerationRequestKeyHelper.BuildRequestKey(tenantId, applicationId, AIGenerationRequestKeyHelper.ApplicationAnalysisOperationType);
        var request = new AIGenerationRequest(
            Guid.NewGuid(),
            tenantId,
            AIGenerationRequestKeyHelper.ApplicationAnalysisOperationType,
            applicationId,
            requestKey);

        var repository = Substitute.For<IRepository<AIGenerationRequest, Guid>>();
        repository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIGenerationRequest>>(new[] { request }.AsQueryable()));

        var backgroundJobManager = Substitute.For<IBackgroundJobManager>();
        var queue = new ApplicationAIGenerationQueue(backgroundJobManager, repository, new TestDistributedLockProvider());

        await queue.QueueApplicationAnalysisAsync(applicationId, tenantId, promptVersion);

        await backgroundJobManager.DidNotReceive().EnqueueAsync(Arg.Any<GenerateApplicationAnalysisBackgroundJobArgs>());
        await repository.DidNotReceive().InsertAsync(Arg.Any<AIGenerationRequest>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueueApplicationAnalysisAsync_Should_Enqueue_New_Request_When_None_Exists()
    {
        var applicationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var promptVersion = "v1";
        var repository = Substitute.For<IRepository<AIGenerationRequest, Guid>>();
        repository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIGenerationRequest>>(Array.Empty<AIGenerationRequest>().AsQueryable()));
        repository.InsertAsync(Arg.Any<AIGenerationRequest>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<AIGenerationRequest>()));

        GenerateApplicationAnalysisBackgroundJobArgs? capturedArgs = null;
        var backgroundJobManager = Substitute.For<IBackgroundJobManager>();
        backgroundJobManager.EnqueueAsync<GenerateApplicationAnalysisBackgroundJobArgs>(
                Arg.Any<GenerateApplicationAnalysisBackgroundJobArgs>(),
                Arg.Any<BackgroundJobPriority>(),
                Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                capturedArgs = callInfo.Arg<GenerateApplicationAnalysisBackgroundJobArgs>();
                return Task.FromResult(string.Empty);
            });

        var queue = new ApplicationAIGenerationQueue(backgroundJobManager, repository, new TestDistributedLockProvider());

        await queue.QueueApplicationAnalysisAsync(applicationId, tenantId, promptVersion);

        capturedArgs.ShouldNotBeNull();
        capturedArgs!.ApplicationId.ShouldBe(applicationId);
        capturedArgs.TenantId.ShouldBe(tenantId);
        capturedArgs.PromptVersion.ShouldBe(promptVersion);
        capturedArgs.RequestKey.ShouldBe(AIGenerationRequestKeyHelper.BuildRequestKey(tenantId, applicationId, AIGenerationRequestKeyHelper.ApplicationAnalysisOperationType));
        await repository.Received(1).InsertAsync(Arg.Is<AIGenerationRequest>(r =>
            r.ApplicationId == applicationId &&
            r.TenantId == tenantId &&
            r.OperationType == AIGenerationRequestKeyHelper.ApplicationAnalysisOperationType &&
            r.RequestKey == capturedArgs.RequestKey &&
            r.Status == AIGenerationRequestStatus.Queued), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueueAttachmentSummaryAsync_Should_Not_Enqueue_When_An_Active_Request_Already_Exists()
    {
        var tenantId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var promptVersion = "v1";
        var requestKey = AIGenerationRequestKeyHelper.BuildRequestKey(tenantId, applicationId, AIGenerationRequestKeyHelper.AttachmentSummaryOperationType);
        var request = new AIGenerationRequest(
            Guid.NewGuid(),
            tenantId,
            AIGenerationRequestKeyHelper.AttachmentSummaryOperationType,
            applicationId,
            requestKey);

        var repository = Substitute.For<IRepository<AIGenerationRequest, Guid>>();
        repository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIGenerationRequest>>(new[] { request }.AsQueryable()));

        var backgroundJobManager = Substitute.For<IBackgroundJobManager>();
        var queue = new ApplicationAIGenerationQueue(backgroundJobManager, repository, new TestDistributedLockProvider());

        await queue.QueueAttachmentSummaryAsync(applicationId, tenantId, promptVersion);

        await backgroundJobManager.DidNotReceive().EnqueueAsync(Arg.Any<GenerateAttachmentSummaryBackgroundJobArgs>());
        await repository.DidNotReceive().InsertAsync(Arg.Any<AIGenerationRequest>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueueAttachmentSummaryAsync_Should_Enqueue_New_Request_When_None_Exists()
    {
        var tenantId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var promptVersion = "v1";
        var repository = Substitute.For<IRepository<AIGenerationRequest, Guid>>();
        repository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIGenerationRequest>>(Array.Empty<AIGenerationRequest>().AsQueryable()));
        repository.InsertAsync(Arg.Any<AIGenerationRequest>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<AIGenerationRequest>()));

        GenerateAttachmentSummaryBackgroundJobArgs? capturedArgs = null;
        var backgroundJobManager = Substitute.For<IBackgroundJobManager>();
        backgroundJobManager.EnqueueAsync<GenerateAttachmentSummaryBackgroundJobArgs>(
                Arg.Any<GenerateAttachmentSummaryBackgroundJobArgs>(),
                Arg.Any<BackgroundJobPriority>(),
                Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                capturedArgs = callInfo.Arg<GenerateAttachmentSummaryBackgroundJobArgs>();
                return Task.FromResult(string.Empty);
            });

        var queue = new ApplicationAIGenerationQueue(backgroundJobManager, repository, new TestDistributedLockProvider());

        await queue.QueueAttachmentSummaryAsync(applicationId, tenantId, promptVersion);

        capturedArgs.ShouldNotBeNull();
        capturedArgs!.ApplicationId.ShouldBe(applicationId);
        capturedArgs.TenantId.ShouldBe(tenantId);
        capturedArgs.PromptVersion.ShouldBe(promptVersion);
        capturedArgs.RequestKey.ShouldBe(AIGenerationRequestKeyHelper.BuildRequestKey(tenantId, applicationId, AIGenerationRequestKeyHelper.AttachmentSummaryOperationType));
        await repository.Received(1).InsertAsync(Arg.Is<AIGenerationRequest>(r =>
            r.ApplicationId == applicationId &&
            r.TenantId == tenantId &&
            r.OperationType == AIGenerationRequestKeyHelper.AttachmentSummaryOperationType &&
            r.RequestKey == capturedArgs.RequestKey &&
            r.Status == AIGenerationRequestStatus.Queued), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueueApplicationScoringAsync_Should_Not_Enqueue_When_An_Active_Request_Already_Exists()
    {
        var tenantId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var promptVersion = "v1";
        var requestKey = AIGenerationRequestKeyHelper.BuildRequestKey(tenantId, applicationId, AIGenerationRequestKeyHelper.ApplicationScoringOperationType);
        var request = new AIGenerationRequest(
            Guid.NewGuid(),
            tenantId,
            AIGenerationRequestKeyHelper.ApplicationScoringOperationType,
            applicationId,
            requestKey);

        var repository = Substitute.For<IRepository<AIGenerationRequest, Guid>>();
        repository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIGenerationRequest>>(new[] { request }.AsQueryable()));

        var backgroundJobManager = Substitute.For<IBackgroundJobManager>();
        var queue = new ApplicationAIGenerationQueue(backgroundJobManager, repository, new TestDistributedLockProvider());

        await queue.QueueApplicationScoringAsync(applicationId, tenantId, promptVersion);

        await backgroundJobManager.DidNotReceive().EnqueueAsync(Arg.Any<GenerateApplicationScoringBackgroundJobArgs>());
        await repository.DidNotReceive().InsertAsync(Arg.Any<AIGenerationRequest>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueueApplicationScoringAsync_Should_Enqueue_New_Request_When_None_Exists()
    {
        var applicationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var promptVersion = "v1";
        var repository = Substitute.For<IRepository<AIGenerationRequest, Guid>>();
        repository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIGenerationRequest>>(Array.Empty<AIGenerationRequest>().AsQueryable()));
        repository.InsertAsync(Arg.Any<AIGenerationRequest>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<AIGenerationRequest>()));

        GenerateApplicationScoringBackgroundJobArgs? capturedArgs = null;
        var backgroundJobManager = Substitute.For<IBackgroundJobManager>();
        backgroundJobManager.EnqueueAsync<GenerateApplicationScoringBackgroundJobArgs>(
                Arg.Any<GenerateApplicationScoringBackgroundJobArgs>(),
                Arg.Any<BackgroundJobPriority>(),
                Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                capturedArgs = callInfo.Arg<GenerateApplicationScoringBackgroundJobArgs>();
                return Task.FromResult(string.Empty);
            });

        var queue = new ApplicationAIGenerationQueue(backgroundJobManager, repository, new TestDistributedLockProvider());

        await queue.QueueApplicationScoringAsync(applicationId, tenantId, promptVersion);

        capturedArgs.ShouldNotBeNull();
        capturedArgs!.ApplicationId.ShouldBe(applicationId);
        capturedArgs.TenantId.ShouldBe(tenantId);
        capturedArgs.PromptVersion.ShouldBe(promptVersion);
        capturedArgs.RequestKey.ShouldBe(AIGenerationRequestKeyHelper.BuildRequestKey(tenantId, applicationId, AIGenerationRequestKeyHelper.ApplicationScoringOperationType));
        await repository.Received(1).InsertAsync(Arg.Is<AIGenerationRequest>(r =>
            r.ApplicationId == applicationId &&
            r.TenantId == tenantId &&
            r.OperationType == AIGenerationRequestKeyHelper.ApplicationScoringOperationType &&
            r.RequestKey == capturedArgs.RequestKey &&
            r.Status == AIGenerationRequestStatus.Queued), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    private sealed class TestDistributedLockProvider : IDistributedLockProvider
    {
        public IDistributedLock CreateLock(string name) => new TestDistributedLock(name);
    }

    private sealed class TestDistributedLock(string name) : IDistributedLock
    {
        public string Name => name;

        public IDistributedSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            new TestDistributedSynchronizationHandle();

        public ValueTask<IDistributedSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IDistributedSynchronizationHandle>(new TestDistributedSynchronizationHandle());

        public IDistributedSynchronizationHandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            new TestDistributedSynchronizationHandle();

        public ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IDistributedSynchronizationHandle?>(new TestDistributedSynchronizationHandle());
    }

    private sealed class TestDistributedSynchronizationHandle : IDistributedSynchronizationHandle
    {
        public CancellationToken HandleLostToken => CancellationToken.None;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
