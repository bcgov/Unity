using Microsoft.Extensions.Logging.Abstractions;
using Medallion.Threading;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Operations;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Attachments;
using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.DistributedLocking;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Features;
using Volo.Abp.MultiTenancy;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.GrantApplications.Automation;

public class RunApplicationAIPipelineJobTests(ITestOutputHelper outputHelper) : GrantManagerApplicationTestBase(outputHelper)
{
    [Fact]
    public async Task ExecuteAsync_Should_Mark_Request_Completed_When_Features_Disabled()
    {
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync(Arg.Any<string>()).Returns(false);

        var repository = BuildRequestRepository(out var requests);
        var applicationId = Guid.NewGuid();
        requests.Add(CreateRequest(applicationId));

        var job = BuildJob(featureChecker, repository);

        await job.ExecuteAsync(new RunApplicationAIPipelineJobArgs
        {
            ApplicationId = applicationId,
            RequestKey = requests[0].RequestKey
        });

        Assert.Equal(AIGenerationRequestStatus.Completed, requests[0].Status);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Not_Run_When_Request_Already_Completed()
    {
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync(Arg.Any<string>()).Returns(true);

        var repository = BuildRequestRepository(out var requests);
        var applicationId = Guid.NewGuid();
        var request = CreateRequest(applicationId);
        request.MarkCompleted(DateTime.UtcNow);
        requests.Add(request);

        var scoringService = Substitute.For<IApplicationScoringService>();

        var job = BuildJob(featureChecker, repository, scoringService: scoringService);

        await job.ExecuteAsync(new RunApplicationAIPipelineJobArgs
        {
            ApplicationId = applicationId,
            RequestKey = request.RequestKey
        });

        await scoringService.DidNotReceive().RegenerateAndSaveAsync(Arg.Any<Guid>(), Arg.Any<string?>());
    }

    private static RunApplicationAIPipelineJob BuildJob(
        IFeatureChecker featureChecker,
        IRepository<AIGenerationRequest, Guid> generationRequestRepository,
        IAttachmentSummaryService? attachmentSummaryService = null,
        IApplicationAnalysisService? applicationAnalysisService = null,
        IApplicationScoringService? scoringService = null,
        IDistributedLockProvider? distributedLockProvider = null)
    {
        var application = (Application)RuntimeHelpers.GetUninitializedObject(typeof(Application));
        application.ApplicationFormId = Guid.NewGuid();

        var applicationForm = (ApplicationForm)RuntimeHelpers.GetUninitializedObject(typeof(ApplicationForm));
        applicationForm.AutomaticallyGenerateAIAnalysis = true;

        var applicationRepository = Substitute.For<IApplicationRepository>();
        applicationRepository.GetAsync(Arg.Any<Guid>(), Arg.Any<bool>()).Returns(application);

        var applicationFormRepository = Substitute.For<IApplicationFormRepository>();
        applicationFormRepository.GetAsync(Arg.Any<Guid>(), Arg.Any<bool>()).Returns(applicationForm);

        return new RunApplicationAIPipelineJob(
            Substitute.For<IAIService>(),
            attachmentSummaryService ?? Substitute.For<IAttachmentSummaryService>(),
            applicationAnalysisService ?? Substitute.For<IApplicationAnalysisService>(),
            scoringService ?? Substitute.For<IApplicationScoringService>(),
            featureChecker,
            Substitute.For<ILocalEventBus>(),
            Substitute.For<ICurrentTenant>(),
            applicationRepository,
            applicationFormRepository,
            generationRequestRepository,
            distributedLockProvider ?? new TestDistributedLockProvider(),
            NullLogger<RunApplicationAIPipelineJob>.Instance);
    }

    private static IRepository<AIGenerationRequest, Guid> BuildRequestRepository(out List<AIGenerationRequest> requests)
    {
        var requestList = new List<AIGenerationRequest>();
        requests = requestList;
        var repository = Substitute.For<IRepository<AIGenerationRequest, Guid>>();

        repository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIGenerationRequest>>(requestList.AsQueryable()));
        repository.InsertAsync(Arg.Any<AIGenerationRequest>(), Arg.Any<bool>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(callInfo =>
            {
                var request = callInfo.Arg<AIGenerationRequest>();
                requestList.Add(request);
                return Task.FromResult(request);
            });
        repository.UpdateAsync(Arg.Any<AIGenerationRequest>(), Arg.Any<bool>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<AIGenerationRequest>()));

        return repository;
    }

    private static AIGenerationRequest CreateRequest(Guid applicationId)
    {
        return new AIGenerationRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            AIGenerationRequestKeyHelper.PipelineOperationType,
            applicationId,
            null,
            null,
            $"tenant:{Guid.NewGuid():D}:application:{applicationId:D}:none:pipeline:default");
    }

    private sealed class TestDistributedLockProvider : IDistributedLockProvider
    {
        public IDistributedLock CreateLock(string name) => new TestDistributedLock(name);
    }

    private sealed class TestDistributedLock(string name) : IDistributedLock
    {
        public string Name => name;

        public IDistributedSynchronizationHandle Acquire(TimeSpan? timeout = null, System.Threading.CancellationToken cancellationToken = default) =>
            new TestDistributedSynchronizationHandle();

        public ValueTask<IDistributedSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, System.Threading.CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IDistributedSynchronizationHandle>(new TestDistributedSynchronizationHandle());

        public IDistributedSynchronizationHandle? TryAcquire(TimeSpan timeout = default, System.Threading.CancellationToken cancellationToken = default) =>
            new TestDistributedSynchronizationHandle();

        public ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync(TimeSpan timeout = default, System.Threading.CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IDistributedSynchronizationHandle?>(new TestDistributedSynchronizationHandle());
    }

    private sealed class TestDistributedSynchronizationHandle : IDistributedSynchronizationHandle
    {
        public System.Threading.CancellationToken HandleLostToken => System.Threading.CancellationToken.None;

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
