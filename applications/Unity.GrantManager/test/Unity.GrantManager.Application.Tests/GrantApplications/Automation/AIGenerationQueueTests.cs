using Medallion.Threading;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Features;
using Unity.AI.Localization;
using Unity.AI.RateLimit;
using Unity.AI.Operations;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.GrantApplications.Automation;
using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.DistributedLocking;
using Volo.Abp;
using Volo.Abp.Features;
using Volo.Abp.Users;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.GrantApplications.Automation;

public class AIGenerationQueueTests(ITestOutputHelper outputHelper) : GrantManagerApplicationTestBase(outputHelper)
{
    [Fact]
    public async Task QueueAllAIStagesAsync_Should_Enqueue_Pipeline_Job_When_None_Exists()
    {
        var applicationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var repository = Substitute.For<IRepository<AIGenerationRequest, Guid>>();
        repository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIGenerationRequest>>(Array.Empty<AIGenerationRequest>().AsQueryable()));
        repository.InsertAsync(Arg.Any<AIGenerationRequest>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<AIGenerationRequest>()));
        var operationRepository = CreateOperationRepository();

        RunApplicationAIPipelineJobArgs? capturedArgs = null;
        var backgroundJobManager = Substitute.For<IBackgroundJobManager>();
        backgroundJobManager.EnqueueAsync<RunApplicationAIPipelineJobArgs>(
                Arg.Any<RunApplicationAIPipelineJobArgs>(),
                Arg.Any<BackgroundJobPriority>(),
                Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                capturedArgs = callInfo.Arg<RunApplicationAIPipelineJobArgs>();
                return Task.FromResult(string.Empty);
            });

        var queue = CreateQueue(backgroundJobManager, repository, operationRepository: operationRepository);

        await queue.QueueAllAIStagesAsync(applicationId, tenantId, "v1");

        capturedArgs.ShouldNotBeNull();
        capturedArgs!.ApplicationId.ShouldBe(applicationId);
        capturedArgs.TenantId.ShouldBe(tenantId);
        capturedArgs.PromptVersion.ShouldBe("v1");
        capturedArgs.RequestedByUserId.ShouldBe(CreateQueueCurrentUserId);
        capturedArgs.RequestKey.ShouldBe(AIGenerationRequestKeyHelper.BuildRequestKey(tenantId, applicationId, AIGenerationRequestKeyHelper.PipelineOperationType));
        await backgroundJobManager.Received(1).EnqueueAsync(Arg.Any<RunApplicationAIPipelineJobArgs>(), Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>());
        await repository.Received(1).InsertAsync(Arg.Is<AIGenerationRequest>(r =>
            r.ApplicationId == applicationId &&
            r.TenantId == tenantId &&
            r.OperationId != Guid.Empty &&
            r.RequestKey == capturedArgs.RequestKey &&
            r.Status == AIGenerationRequestStatus.Queued), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueueAllAIStagesAsync_Should_Enqueue_When_Any_Enabled_Stage_Has_Required_Input()
    {
        var applicationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var repository = Substitute.For<IRepository<AIGenerationRequest, Guid>>();
        repository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIGenerationRequest>>(Array.Empty<AIGenerationRequest>().AsQueryable()));
        repository.InsertAsync(Arg.Any<AIGenerationRequest>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<AIGenerationRequest>()));
        var operationRepository = CreateOperationRepository();

        var backgroundJobManager = Substitute.For<IBackgroundJobManager>();
        var prerequisiteValidator = Substitute.For<IAIGenerationPrerequisiteValidator>();
        prerequisiteValidator.EnsureAttachmentSummaryAvailableAsync(applicationId)
            .Returns<Task>(_ => throw new UserFriendlyException("No attachments are available to summarize."));
        prerequisiteValidator.EnsureApplicationAnalysisAvailableAsync(applicationId).Returns(Task.CompletedTask);
        prerequisiteValidator.EnsureApplicationScoringAvailableAsync(applicationId)
            .Returns<Task>(_ => throw new UserFriendlyException("AI scoring requires a configured scoresheet."));

        var queue = CreateQueue(
            backgroundJobManager,
            repository,
            operationRepository: operationRepository,
            prerequisiteValidator: prerequisiteValidator);

        await queue.QueueAllAIStagesAsync(applicationId, tenantId);

        await repository.Received(1).InsertAsync(Arg.Any<AIGenerationRequest>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await backgroundJobManager.Received(1).EnqueueAsync(Arg.Any<RunApplicationAIPipelineJobArgs>(), Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task QueueAllAIStagesAsync_Should_Not_Insert_Or_Enqueue_When_No_Enabled_Stage_Has_Required_Input()
    {
        var applicationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var repository = Substitute.For<IRepository<AIGenerationRequest, Guid>>();
        repository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIGenerationRequest>>(Array.Empty<AIGenerationRequest>().AsQueryable()));
        var operationRepository = CreateOperationRepository();

        var backgroundJobManager = Substitute.For<IBackgroundJobManager>();
        var rateLimiter = Substitute.For<IAIRateLimiter>();
        rateLimiter.EnsureAsync().Returns(Task.CompletedTask);
        var prerequisiteValidator = Substitute.For<IAIGenerationPrerequisiteValidator>();
        prerequisiteValidator.EnsureAttachmentSummaryAvailableAsync(applicationId)
            .Returns<Task>(_ => throw new UserFriendlyException("No attachments are available to summarize."));
        prerequisiteValidator.EnsureApplicationAnalysisAvailableAsync(applicationId)
            .Returns<Task>(_ => throw new UserFriendlyException("AI application analysis requires application submission data."));
        prerequisiteValidator.EnsureApplicationScoringAvailableAsync(applicationId)
            .Returns<Task>(_ => throw new UserFriendlyException("AI scoring requires a configured scoresheet."));

        var queue = CreateQueue(
            backgroundJobManager,
            repository,
            rateLimiter,
            prerequisiteValidator,
            operationRepository: operationRepository);

        await Should.ThrowAsync<UserFriendlyException>(() => queue.QueueAllAIStagesAsync(applicationId, tenantId));

        await rateLimiter.DidNotReceive().EnsureAsync();
        await repository.DidNotReceive().InsertAsync(Arg.Any<AIGenerationRequest>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await backgroundJobManager.DidNotReceive().EnqueueAsync(Arg.Any<RunApplicationAIPipelineJobArgs>(), Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task QueueAllAIStagesAsync_Should_Not_Insert_Or_Enqueue_When_No_AI_Stages_Are_Enabled()
    {
        var applicationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var repository = Substitute.For<IRepository<AIGenerationRequest, Guid>>();
        repository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIGenerationRequest>>(Array.Empty<AIGenerationRequest>().AsQueryable()));
        var operationRepository = CreateOperationRepository();

        var backgroundJobManager = Substitute.For<IBackgroundJobManager>();
        var rateLimiter = Substitute.For<IAIRateLimiter>();
        rateLimiter.EnsureAsync().Returns(Task.CompletedTask);
        var prerequisiteValidator = Substitute.For<IAIGenerationPrerequisiteValidator>();
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync(AIFeatures.AttachmentSummaries).Returns(Task.FromResult(false));
        featureChecker.IsEnabledAsync(AIFeatures.ApplicationAnalysis).Returns(Task.FromResult(false));
        featureChecker.IsEnabledAsync(AIFeatures.Scoring).Returns(Task.FromResult(false));

        var queue = CreateQueue(
            backgroundJobManager,
            repository,
            rateLimiter,
            prerequisiteValidator,
            featureChecker,
            operationRepository: operationRepository);

        await Should.ThrowAsync<UserFriendlyException>(() => queue.QueueAllAIStagesAsync(applicationId, tenantId));

        await prerequisiteValidator.DidNotReceive().EnsureAttachmentSummaryAvailableAsync(Arg.Any<Guid>());
        await prerequisiteValidator.DidNotReceive().EnsureApplicationAnalysisAvailableAsync(Arg.Any<Guid>());
        await prerequisiteValidator.DidNotReceive().EnsureApplicationScoringAvailableAsync(Arg.Any<Guid>());
        await rateLimiter.DidNotReceive().EnsureAsync();
        await repository.DidNotReceive().InsertAsync(Arg.Any<AIGenerationRequest>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await backgroundJobManager.DidNotReceive().EnqueueAsync(Arg.Any<RunApplicationAIPipelineJobArgs>(), Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>());
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
            Guid.NewGuid(),
            applicationId,
            requestKey);

        var repository = Substitute.For<IRepository<AIGenerationRequest, Guid>>();
        repository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIGenerationRequest>>(new[] { request }.AsQueryable()));

        var backgroundJobManager = Substitute.For<IBackgroundJobManager>();
        var rateLimiter = Substitute.For<Unity.AI.RateLimit.IAIRateLimiter>();
        rateLimiter.EnsureAsync().Returns(Task.CompletedTask);
        var queue = CreateQueue(backgroundJobManager, repository, rateLimiter: rateLimiter);

        await queue.QueueApplicationAnalysisAsync(applicationId, tenantId, promptVersion);

        await backgroundJobManager.DidNotReceive().EnqueueAsync(Arg.Any<GenerateApplicationAnalysisBackgroundJobArgs>());
        await repository.DidNotReceive().InsertAsync(Arg.Any<AIGenerationRequest>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await rateLimiter.DidNotReceive().EnsureAsync();
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

        var queue = CreateQueue(backgroundJobManager, repository);

        await queue.QueueApplicationAnalysisAsync(applicationId, tenantId, promptVersion);

        capturedArgs.ShouldNotBeNull();
        capturedArgs!.ApplicationId.ShouldBe(applicationId);
        capturedArgs.TenantId.ShouldBe(tenantId);
        capturedArgs.PromptVersion.ShouldBe(promptVersion);
        capturedArgs.RequestedByUserId.ShouldBe(CreateQueueCurrentUserId);
        capturedArgs.RequestKey.ShouldBe(AIGenerationRequestKeyHelper.BuildRequestKey(tenantId, applicationId, AIGenerationRequestKeyHelper.ApplicationAnalysisOperationType));
        await repository.Received(1).InsertAsync(Arg.Is<AIGenerationRequest>(r =>
            r.ApplicationId == applicationId &&
            r.TenantId == tenantId &&
            r.OperationId != Guid.Empty &&
            r.RequestKey == capturedArgs.RequestKey &&
            r.Status == AIGenerationRequestStatus.Queued), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueueApplicationAnalysisAsync_Should_Check_Rate_Limit_Before_Enqueueing_New_Request()
    {
        var applicationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var repository = Substitute.For<IRepository<AIGenerationRequest, Guid>>();
        repository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIGenerationRequest>>(Array.Empty<AIGenerationRequest>().AsQueryable()));
        repository.InsertAsync(Arg.Any<AIGenerationRequest>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<AIGenerationRequest>()));

        var backgroundJobManager = Substitute.For<IBackgroundJobManager>();
        var rateLimiter = Substitute.For<IAIRateLimiter>();
        rateLimiter.EnsureAsync().Returns(Task.CompletedTask);
        var queue = CreateQueue(backgroundJobManager, repository, rateLimiter: rateLimiter);

        await queue.QueueApplicationAnalysisAsync(applicationId, tenantId);

        await rateLimiter.Received(1).EnsureAsync();
        await repository.Received(1).InsertAsync(Arg.Any<AIGenerationRequest>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await backgroundJobManager.Received(1).EnqueueAsync(Arg.Any<GenerateApplicationAnalysisBackgroundJobArgs>(), Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task QueueApplicationAnalysisAsync_Should_Not_Insert_Or_Enqueue_When_Rate_Limited()
    {
        var applicationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var repository = Substitute.For<IRepository<AIGenerationRequest, Guid>>();
        repository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIGenerationRequest>>(Array.Empty<AIGenerationRequest>().AsQueryable()));

        var backgroundJobManager = Substitute.For<IBackgroundJobManager>();
        var rateLimiter = Substitute.For<IAIRateLimiter>();
        rateLimiter.EnsureAsync().Returns<Task>(_ => throw new InvalidOperationException("rate limited"));
        var queue = CreateQueue(backgroundJobManager, repository, rateLimiter: rateLimiter);

        await Should.ThrowAsync<InvalidOperationException>(() => queue.QueueApplicationAnalysisAsync(applicationId, tenantId));

        await rateLimiter.Received(1).EnsureAsync();
        await repository.DidNotReceive().InsertAsync(Arg.Any<AIGenerationRequest>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await backgroundJobManager.DidNotReceive().EnqueueAsync(Arg.Any<GenerateApplicationAnalysisBackgroundJobArgs>(), Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task QueueAttachmentSummaryAsync_Should_Not_Insert_Or_Enqueue_When_No_Attachments_Are_Available()
    {
        var applicationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var repository = Substitute.For<IRepository<AIGenerationRequest, Guid>>();
        repository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIGenerationRequest>>(Array.Empty<AIGenerationRequest>().AsQueryable()));

        var backgroundJobManager = Substitute.For<IBackgroundJobManager>();
        var rateLimiter = Substitute.For<IAIRateLimiter>();
        rateLimiter.EnsureAsync().Returns(Task.CompletedTask);
        var prerequisiteValidator = Substitute.For<IAIGenerationPrerequisiteValidator>();
        prerequisiteValidator.EnsureAttachmentSummaryAvailableAsync(applicationId)
            .Returns<Task>(_ => throw new UserFriendlyException("No attachments are available to summarize."));
        var queue = CreateQueue(backgroundJobManager, repository, rateLimiter: rateLimiter, prerequisiteValidator: prerequisiteValidator);

        await Should.ThrowAsync<UserFriendlyException>(() => queue.QueueAttachmentSummaryAsync(applicationId, tenantId));

        await rateLimiter.DidNotReceive().EnsureAsync();
        await repository.DidNotReceive().InsertAsync(Arg.Any<AIGenerationRequest>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await backgroundJobManager.DidNotReceive().EnqueueAsync(Arg.Any<GenerateAttachmentSummaryBackgroundJobArgs>(), Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>());
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
            Guid.NewGuid(),
            applicationId,
            requestKey);

        var repository = Substitute.For<IRepository<AIGenerationRequest, Guid>>();
        repository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIGenerationRequest>>(new[] { request }.AsQueryable()));

        var backgroundJobManager = Substitute.For<IBackgroundJobManager>();
        var queue = CreateQueue(backgroundJobManager, repository);

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
        var operationRepository = CreateOperationRepository();

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

        var queue = CreateQueue(backgroundJobManager, repository, operationRepository: operationRepository);

        await queue.QueueAttachmentSummaryAsync(applicationId, tenantId, promptVersion);

        capturedArgs.ShouldNotBeNull();
        capturedArgs!.ApplicationId.ShouldBe(applicationId);
        capturedArgs.TenantId.ShouldBe(tenantId);
        capturedArgs.PromptVersion.ShouldBe(promptVersion);
        capturedArgs.RequestedByUserId.ShouldBe(CreateQueueCurrentUserId);
        capturedArgs.RequestKey.ShouldBe(AIGenerationRequestKeyHelper.BuildRequestKey(tenantId, applicationId, AIGenerationRequestKeyHelper.AttachmentSummaryOperationType));
        await repository.Received(1).InsertAsync(Arg.Is<AIGenerationRequest>(r =>
            r.ApplicationId == applicationId &&
            r.TenantId == tenantId &&
            r.OperationId != Guid.Empty &&
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
            Guid.NewGuid(),
            applicationId,
            requestKey);

        var repository = Substitute.For<IRepository<AIGenerationRequest, Guid>>();
        repository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIGenerationRequest>>(new[] { request }.AsQueryable()));

        var backgroundJobManager = Substitute.For<IBackgroundJobManager>();
        var queue = CreateQueue(backgroundJobManager, repository);

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
        var operationRepository = CreateOperationRepository();

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

        var queue = CreateQueue(backgroundJobManager, repository, operationRepository: operationRepository);

        await queue.QueueApplicationScoringAsync(applicationId, tenantId, promptVersion);

        capturedArgs.ShouldNotBeNull();
        capturedArgs!.ApplicationId.ShouldBe(applicationId);
        capturedArgs.TenantId.ShouldBe(tenantId);
        capturedArgs.PromptVersion.ShouldBe(promptVersion);
        capturedArgs.RequestedByUserId.ShouldBe(CreateQueueCurrentUserId);
        capturedArgs.RequestKey.ShouldBe(AIGenerationRequestKeyHelper.BuildRequestKey(tenantId, applicationId, AIGenerationRequestKeyHelper.ApplicationScoringOperationType));
        await repository.Received(1).InsertAsync(Arg.Is<AIGenerationRequest>(r =>
            r.ApplicationId == applicationId &&
            r.TenantId == tenantId &&
            r.OperationId != Guid.Empty &&
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

    private static ApplicationAIGenerationQueue CreateQueue(
        IBackgroundJobManager backgroundJobManager,
        IRepository<AIGenerationRequest, Guid> repository,
        Unity.AI.RateLimit.IAIRateLimiter? rateLimiter = null,
        IAIGenerationPrerequisiteValidator? prerequisiteValidator = null,
        IFeatureChecker? featureChecker = null,
        IRepository<AIOperation, Guid>? operationRepository = null)
    {
        if (rateLimiter == null)
        {
            rateLimiter = Substitute.For<Unity.AI.RateLimit.IAIRateLimiter>();
            rateLimiter.EnsureAsync().Returns(Task.CompletedTask);
        }

        if (prerequisiteValidator == null)
        {
            prerequisiteValidator = Substitute.For<IAIGenerationPrerequisiteValidator>();
            prerequisiteValidator.EnsureAttachmentSummaryAvailableAsync(Arg.Any<Guid>()).Returns(Task.CompletedTask);
            prerequisiteValidator.EnsureApplicationAnalysisAvailableAsync(Arg.Any<Guid>()).Returns(Task.CompletedTask);
            prerequisiteValidator.EnsureApplicationScoringAvailableAsync(Arg.Any<Guid>()).Returns(Task.CompletedTask);
        }

        if (featureChecker == null)
        {
            featureChecker = Substitute.For<IFeatureChecker>();
            featureChecker.IsEnabledAsync(AIFeatures.AttachmentSummaries).Returns(Task.FromResult(true));
            featureChecker.IsEnabledAsync(AIFeatures.ApplicationAnalysis).Returns(Task.FromResult(true));
            featureChecker.IsEnabledAsync(AIFeatures.Scoring).Returns(Task.FromResult(true));
        }

        return new ApplicationAIGenerationQueue(
            backgroundJobManager,
            repository,
            operationRepository ?? CreateOperationRepository(),
            new TestDistributedLockProvider(),
            prerequisiteValidator,
            featureChecker,
            rateLimiter,
            CreateCurrentUser(),
            Substitute.For<ILogger<ApplicationAIGenerationQueue>>(),
            Substitute.For<IStringLocalizer<AIResource>>());
    }

    private static readonly Guid CreateQueueCurrentUserId = Guid.NewGuid();

    private static IRepository<AIOperation, Guid> CreateOperationRepository()
    {
        var operations = new List<AIOperation>
        {
            new(Guid.NewGuid(), "AttachmentSummary", Guid.NewGuid(), Guid.NewGuid()) { IsActive = true },
            new(Guid.NewGuid(), "ApplicationAnalysis", Guid.NewGuid(), Guid.NewGuid()) { IsActive = true },
            new(Guid.NewGuid(), "ApplicationScoring", Guid.NewGuid(), Guid.NewGuid()) { IsActive = true },
            new(Guid.NewGuid(), "Default", Guid.NewGuid(), Guid.NewGuid()) { IsActive = true }
        };

        var repository = Substitute.For<IRepository<AIOperation, Guid>>();
        repository.GetQueryableAsync()
            .Returns(Task.FromResult<IQueryable<AIOperation>>(operations.AsQueryable()));

        return repository;
    }

    private static ICurrentUser CreateCurrentUser()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(CreateQueueCurrentUserId);
        return currentUser;
    }
}
