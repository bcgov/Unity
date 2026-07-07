using Medallion.Threading;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Features;
using Unity.AI.Localization;
using Unity.AI.Operations;
using Unity.AI.RateLimit;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
using Volo.Abp;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;
using Volo.Abp.Linq;
using Volo.Abp.Users;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.GrantApplications.Automation;

public class AIGenerationQueueTests(ITestOutputHelper outputHelper) : GrantManagerApplicationTestBase(outputHelper)
{
    private static readonly Guid AttachmentSummaryOperationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ApplicationAnalysisOperationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ApplicationScoringOperationId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    [Fact]
    public async Task QueueAllAIStagesAsync_Should_Enqueue_Pipeline_Job_When_None_Exists()
    {
        var applicationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var backgroundJobManager = Substitute.For<IBackgroundJobManager>();
        var attachmentJobs = new List<GenerateAttachmentSummaryBackgroundJobArgs>();
        var analysisJobs = new List<GenerateApplicationAnalysisBackgroundJobArgs>();
        var scoringJobs = new List<GenerateApplicationScoringBackgroundJobArgs>();
        backgroundJobManager.EnqueueAsync(Arg.Any<GenerateAttachmentSummaryBackgroundJobArgs>(), Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                attachmentJobs.Add(callInfo.Arg<GenerateAttachmentSummaryBackgroundJobArgs>());
                return Task.FromResult(string.Empty);
            });
        backgroundJobManager.EnqueueAsync(Arg.Any<GenerateApplicationAnalysisBackgroundJobArgs>(), Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                analysisJobs.Add(callInfo.Arg<GenerateApplicationAnalysisBackgroundJobArgs>());
                return Task.FromResult(string.Empty);
            });
        backgroundJobManager.EnqueueAsync(Arg.Any<GenerateApplicationScoringBackgroundJobArgs>(), Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                scoringJobs.Add(callInfo.Arg<GenerateApplicationScoringBackgroundJobArgs>());
                return Task.FromResult(string.Empty);
            });

        var queue = CreateQueue(backgroundJobManager);

        await queue.QueueAllAIStagesAsync(applicationId, tenantId, "v1");

        attachmentJobs.Single().ApplicationId.ShouldBe(applicationId);
        analysisJobs.Single().ApplicationId.ShouldBe(applicationId);
        scoringJobs.Single().ApplicationId.ShouldBe(applicationId);
        attachmentJobs.Single().TenantId.ShouldBe(tenantId);
        analysisJobs.Single().TenantId.ShouldBe(tenantId);
        scoringJobs.Single().TenantId.ShouldBe(tenantId);
    }

    [Fact]
    public async Task QueueAllAIStagesAsync_Should_Enqueue_When_Any_Enabled_Stage_Has_Required_Input()
    {
        var applicationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var backgroundJobManager = Substitute.For<IBackgroundJobManager>();
        var attachmentJobs = new List<GenerateAttachmentSummaryBackgroundJobArgs>();
        var analysisJobs = new List<GenerateApplicationAnalysisBackgroundJobArgs>();
        var scoringJobs = new List<GenerateApplicationScoringBackgroundJobArgs>();
        backgroundJobManager.EnqueueAsync(Arg.Any<GenerateAttachmentSummaryBackgroundJobArgs>(), Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                attachmentJobs.Add(callInfo.Arg<GenerateAttachmentSummaryBackgroundJobArgs>());
                return Task.FromResult(string.Empty);
            });
        backgroundJobManager.EnqueueAsync(Arg.Any<GenerateApplicationAnalysisBackgroundJobArgs>(), Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                analysisJobs.Add(callInfo.Arg<GenerateApplicationAnalysisBackgroundJobArgs>());
                return Task.FromResult(string.Empty);
            });
        backgroundJobManager.EnqueueAsync(Arg.Any<GenerateApplicationScoringBackgroundJobArgs>(), Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                scoringJobs.Add(callInfo.Arg<GenerateApplicationScoringBackgroundJobArgs>());
                return Task.FromResult(string.Empty);
            });

        var queue = CreateQueue(backgroundJobManager);

        await queue.QueueAllAIStagesAsync(applicationId, tenantId);

        attachmentJobs.Count.ShouldBe(1);
        analysisJobs.Count.ShouldBe(1);
        scoringJobs.Count.ShouldBe(1);
    }

    [Fact]
    public async Task QueueAllAIStagesAsync_Should_Not_Enqueue_When_No_Enabled_Stage_Has_Required_Input()
    {
        var applicationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
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

        var queue = CreateQueue(backgroundJobManager, rateLimiter: rateLimiter, prerequisiteValidator: prerequisiteValidator);

        await Should.ThrowAsync<UserFriendlyException>(() => queue.QueueAllAIStagesAsync(applicationId, tenantId));

        await rateLimiter.DidNotReceive().EnsureAsync();
        await backgroundJobManager.DidNotReceive().EnqueueAsync(Arg.Any<GenerateAttachmentSummaryBackgroundJobArgs>(), Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>());
        await backgroundJobManager.DidNotReceive().EnqueueAsync(Arg.Any<GenerateApplicationAnalysisBackgroundJobArgs>(), Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>());
        await backgroundJobManager.DidNotReceive().EnqueueAsync(Arg.Any<GenerateApplicationScoringBackgroundJobArgs>(), Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>());
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
        backgroundJobManager.EnqueueAsync<GenerateApplicationAnalysisBackgroundJobArgs>(Arg.Any<GenerateApplicationAnalysisBackgroundJobArgs>(), Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>())
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
        await repository.Received(1).InsertAsync(Arg.Is<AIGenerationRequest>(r =>
            r.ApplicationId == applicationId &&
            r.TenantId == tenantId &&
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
        backgroundJobManager.EnqueueAsync<GenerateAttachmentSummaryBackgroundJobArgs>(Arg.Any<GenerateAttachmentSummaryBackgroundJobArgs>(), Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                capturedArgs = callInfo.Arg<GenerateAttachmentSummaryBackgroundJobArgs>();
                return Task.FromResult(string.Empty);
            });

        var queue = CreateQueue(backgroundJobManager, repository);

        await queue.QueueAttachmentSummaryAsync(applicationId, tenantId, promptVersion);

        capturedArgs.ShouldNotBeNull();
        capturedArgs!.ApplicationId.ShouldBe(applicationId);
        capturedArgs.TenantId.ShouldBe(tenantId);
        capturedArgs.PromptVersion.ShouldBe(promptVersion);
        capturedArgs.RequestedByUserId.ShouldBe(CreateQueueCurrentUserId);
        await repository.Received(1).InsertAsync(Arg.Is<AIGenerationRequest>(r =>
            r.ApplicationId == applicationId &&
            r.TenantId == tenantId &&
            r.Status == AIGenerationRequestStatus.Queued), Arg.Any<bool>(), Arg.Any<CancellationToken>());
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
        backgroundJobManager.EnqueueAsync<GenerateApplicationScoringBackgroundJobArgs>(Arg.Any<GenerateApplicationScoringBackgroundJobArgs>(), Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                capturedArgs = callInfo.Arg<GenerateApplicationScoringBackgroundJobArgs>();
                return Task.FromResult(string.Empty);
            });

        var queue = CreateQueue(backgroundJobManager, repository);

        await queue.QueueApplicationScoringAsync(applicationId, tenantId, promptVersion);

        capturedArgs.ShouldNotBeNull();
        capturedArgs!.ApplicationId.ShouldBe(applicationId);
        capturedArgs.TenantId.ShouldBe(tenantId);
        capturedArgs.PromptVersion.ShouldBe(promptVersion);
        capturedArgs.RequestedByUserId.ShouldBe(CreateQueueCurrentUserId);
        await repository.Received(1).InsertAsync(Arg.Is<AIGenerationRequest>(r =>
            r.ApplicationId == applicationId &&
            r.TenantId == tenantId &&
            r.OperationId != Guid.Empty &&
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
        IRepository<AIGenerationRequest, Guid>? repository = null,
        IAIRateLimiter? rateLimiter = null,
        IAIGenerationPrerequisiteValidator? prerequisiteValidator = null,
        IFeatureChecker? featureChecker = null,
        IRepository<AIOperation, Guid>? operationRepository = null,
        IAsyncQueryableExecuter? asyncQueryableExecuter = null)
    {
        repository ??= Substitute.For<IRepository<AIGenerationRequest, Guid>>();
        repository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIGenerationRequest>>(Array.Empty<AIGenerationRequest>().AsQueryable()));

        if (rateLimiter == null)
        {
            rateLimiter = Substitute.For<IAIRateLimiter>();
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

        asyncQueryableExecuter ??= Substitute.For<IAsyncQueryableExecuter>();
        asyncQueryableExecuter.ToListAsync(Arg.Any<IQueryable<AIOperation>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<IQueryable<AIOperation>>().ToList()));
        asyncQueryableExecuter.FirstOrDefaultAsync(Arg.Any<IQueryable<AIOperation>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<IQueryable<AIOperation>>().FirstOrDefault()));
        asyncQueryableExecuter.FirstOrDefaultAsync(Arg.Any<IQueryable<AIGenerationRequest>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<IQueryable<AIGenerationRequest>>().FirstOrDefault()));

        return new ApplicationAIGenerationQueue(
            backgroundJobManager,
            repository,
            operationRepository ?? CreateOperationRepository(),
            new TestDistributedLockProvider(),
            prerequisiteValidator,
            featureChecker,
            rateLimiter,
            asyncQueryableExecuter,
            CreateCurrentUser(),
            Substitute.For<ILogger<ApplicationAIGenerationQueue>>(),
            Substitute.For<IStringLocalizer<AIResource>>());
    }

    private static readonly Guid CreateQueueCurrentUserId = Guid.NewGuid();

    private static IRepository<AIOperation, Guid> CreateOperationRepository()
    {
        var operations = new List<AIOperation>
        {
            new(AttachmentSummaryOperationId, "AttachmentSummary", Guid.NewGuid(), Guid.NewGuid()) { IsActive = true },
            new(ApplicationAnalysisOperationId, "ApplicationAnalysis", Guid.NewGuid(), Guid.NewGuid()) { IsActive = true },
            new(ApplicationScoringOperationId, "ApplicationScoring", Guid.NewGuid(), Guid.NewGuid()) { IsActive = true }
        };

        var repository = Substitute.For<IRepository<AIOperation, Guid>>();
        repository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIOperation>>(operations.AsQueryable()));
        return repository;
    }

    private static ICurrentUser CreateCurrentUser()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(CreateQueueCurrentUserId);
        return currentUser;
    }
}

