using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Medallion.Threading;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Unity.AI.RateLimit;
using Unity.AI.Domain;
using Unity.AI.Features;
using Unity.AI.Generation;
using Unity.AI.Operations;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
using Volo.Abp.BackgroundJobs;
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
    [Fact]
    public async Task QueueAsync_Should_Enqueue_A_Generic_Catalog_Keyed_Job()
    {
        var applicationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var formVersionId = Guid.NewGuid();
        var jobs = new List<AIGenerationBackgroundJobArgs>();
        var backgroundJobManager = CreateBackgroundJobManager(jobs);
        var queue = CreateQueue(backgroundJobManager);

        await queue.QueueAsync(
            AIGenerationOperations.FormMapping,
            new AIGenerationSubmissionDto
            {
                ApplicationId = applicationId,
                ApplicationFormVersionId = formVersionId,
                PromptVersion = "v1"
            },
            tenantId);

        var job = jobs.ShouldHaveSingleItem();
        job.OperationType.ShouldBe(AIGenerationOperations.FormMapping);
        job.ApplicationId.ShouldBe(applicationId);
        job.ApplicationFormVersionId.ShouldBe(formVersionId);
        job.TenantId.ShouldBe(tenantId);
        job.PromptVersion.ShouldBe("v1");
        job.RequestedByUserId.ShouldBe(CurrentUserId);
    }

    [Fact]
    public async Task QueueApplicationIntakeAsync_Should_Route_Stages_Through_Generic_Jobs()
    {
        var applicationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var jobs = new List<AIGenerationBackgroundJobArgs>();
        var queue = CreateQueue(CreateBackgroundJobManager(jobs));

        await queue.QueueApplicationIntakeAsync(applicationId, tenantId, "v1");

        jobs.Select(job => job.OperationType).ShouldBe([
            AIGenerationOperations.AttachmentSummary,
            AIGenerationOperations.ApplicationAnalysis,
            AIGenerationOperations.ApplicationScoring]);
        jobs.ShouldAllBe(job => job.ApplicationId == applicationId && job.TenantId == tenantId);
    }

    [Fact]
    public async Task QueueAsync_Should_Not_Enqueue_An_Active_Operation_Request()
    {
        var applicationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var repository = Substitute.For<IRepository<AIGenerationRequest, Guid>>();
        repository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIGenerationRequest>>(
            new[] { new AIGenerationRequest(Guid.NewGuid(), tenantId, ApplicationAnalysisOperationId, applicationId) }
                .AsQueryable()));
        var jobs = new List<AIGenerationBackgroundJobArgs>();
        var queue = CreateQueue(CreateBackgroundJobManager(jobs), repository);

        await queue.QueueAsync(
            AIGenerationOperations.ApplicationAnalysis,
            new AIGenerationSubmissionDto { ApplicationId = applicationId },
            tenantId);

        jobs.ShouldBeEmpty();
        await repository.DidNotReceive().InsertAsync(
            Arg.Any<AIGenerationRequest>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    private static IBackgroundJobManager CreateBackgroundJobManager(List<AIGenerationBackgroundJobArgs> jobs)
    {
        var backgroundJobManager = Substitute.For<IBackgroundJobManager>();
        backgroundJobManager.EnqueueAsync(
                Arg.Any<AIGenerationBackgroundJobArgs>(),
                Arg.Any<BackgroundJobPriority>(),
                Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                jobs.Add(callInfo.Arg<AIGenerationBackgroundJobArgs>());
                return Task.FromResult(string.Empty);
            });
        return backgroundJobManager;
    }

    private static ApplicationGenerationQueue CreateQueue(
        IBackgroundJobManager backgroundJobManager,
        IRepository<AIGenerationRequest, Guid>? repository = null)
    {
        if (repository == null)
        {
            repository = Substitute.For<IRepository<AIGenerationRequest, Guid>>();
            repository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIGenerationRequest>>(
                Array.Empty<AIGenerationRequest>().AsQueryable()));
        }

        var prerequisiteValidator = Substitute.For<IAIGenerationPrerequisiteValidator>();
        prerequisiteValidator.EnsureAvailableAsync(
                Arg.Any<string>(),
                Arg.Any<AIGenerationSubmissionDto>())
            .Returns(Task.CompletedTask);

        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync(AIFeatures.AttachmentSummaries).Returns(Task.FromResult(true));
        featureChecker.IsEnabledAsync(AIFeatures.ApplicationAnalysis).Returns(Task.FromResult(true));
        featureChecker.IsEnabledAsync(AIFeatures.Scoring).Returns(Task.FromResult(true));

        var rateLimiter = Substitute.For<IAIRateLimiter>();
        rateLimiter.EnsureAsync(Arg.Any<Guid?>()).Returns(Task.CompletedTask);

        var asyncQueryableExecuter = Substitute.For<IAsyncQueryableExecuter>();
        asyncQueryableExecuter.FirstOrDefaultAsync(
                Arg.Any<IQueryable<AIOperation>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<IQueryable<AIOperation>>().FirstOrDefault()));
        asyncQueryableExecuter.FirstOrDefaultAsync(
                Arg.Any<IQueryable<AIGenerationRequest>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<IQueryable<AIGenerationRequest>>().FirstOrDefault()));

        return new ApplicationGenerationQueue(
            backgroundJobManager,
            repository,
            CreateOperationRepository(),
            new TestDistributedLockProvider(),
            prerequisiteValidator,
            featureChecker,
            rateLimiter,
            asyncQueryableExecuter,
            CreateCurrentUser(),
            Substitute.For<ILogger<ApplicationGenerationQueue>>());
    }

    private static readonly Guid CurrentUserId = Guid.NewGuid();
    private static readonly Guid ApplicationAnalysisOperationId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static IRepository<AIOperation, Guid> CreateOperationRepository()
    {
        var operations = new List<AIOperation>
        {
            new(Guid.NewGuid(), "AttachmentSummary", Guid.NewGuid(), Guid.NewGuid()) { IsActive = true },
            new(ApplicationAnalysisOperationId, "ApplicationAnalysis", Guid.NewGuid(), Guid.NewGuid()) { IsActive = true },
            new(Guid.NewGuid(), "ApplicationScoring", Guid.NewGuid(), Guid.NewGuid()) { IsActive = true },
            new(Guid.NewGuid(), "FormMapping", Guid.NewGuid(), Guid.NewGuid()) { IsActive = true }
        };

        var repository = Substitute.For<IRepository<AIOperation, Guid>>();
        repository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIOperation>>(operations.AsQueryable()));
        return repository;
    }

    private static ICurrentUser CreateCurrentUser()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(CurrentUserId);
        return currentUser;
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