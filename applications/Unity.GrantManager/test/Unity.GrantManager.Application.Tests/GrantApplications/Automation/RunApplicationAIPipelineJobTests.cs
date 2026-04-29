using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Attachments;
using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
using Volo.Abp.Domain.Repositories;
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
        var request = CreateRequest();
        requests.Add(request);

        var job = BuildJob(featureChecker, repository);

        await job.ExecuteAsync(new RunApplicationAIPipelineJobArgs
        {
            ApplicationId = request.ApplicationId!.Value,
            RequestKey = request.RequestKey,
            TenantId = request.TenantId
        });

        request.Status.ShouldBe(AIGenerationRequestStatus.Completed);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Publish_Event_When_Pipeline_Scoring_Completes()
    {
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries").Returns(false);
        featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis").Returns(false);
        featureChecker.IsEnabledAsync("Unity.AI.Scoring").Returns(true);

        var repository = BuildRequestRepository(out var requests);
        var request = CreateRequest();
        requests.Add(request);

        var scoringAppService = Substitute.For<IApplicationScoringAppService>();
        scoringAppService.GenerateApplicationScoringForPipelineAsync(Arg.Any<Guid>(), Arg.Any<string?>())
            .Returns(new ApplicationScoringResultDto { Completed = true });

        var localEventBus = Substitute.For<ILocalEventBus>();

        var job = BuildJob(
            featureChecker,
            repository,
            localEventBus: localEventBus,
            applicationScoringAppService: scoringAppService);

        await job.ExecuteAsync(new RunApplicationAIPipelineJobArgs
        {
            ApplicationId = request.ApplicationId!.Value,
            RequestKey = request.RequestKey,
            TenantId = request.TenantId
        });

        request.Status.ShouldBe(AIGenerationRequestStatus.Completed);
        await localEventBus.Received(1).PublishAsync(
            Arg.Is<Automation.Events.ApplicationAIScoringGeneratedEvent>(x => x.ApplicationId == request.ApplicationId));
    }

    private static RunApplicationAIPipelineJob BuildJob(
        IFeatureChecker featureChecker,
        IRepository<AIGenerationRequest, Guid> generationRequestRepository,
        ILocalEventBus? localEventBus = null,
        IAttachmentSummaryAppService? attachmentSummaryAppService = null,
        IApplicationAnalysisAppService? applicationAnalysisAppService = null,
        IApplicationScoringAppService? applicationScoringAppService = null)
    {
        return new RunApplicationAIPipelineJob(
            Substitute.For<IApplicationChefsFileAttachmentRepository>(),
            attachmentSummaryAppService ?? Substitute.For<IAttachmentSummaryAppService>(),
            applicationAnalysisAppService ?? Substitute.For<IApplicationAnalysisAppService>(),
            applicationScoringAppService ?? Substitute.For<IApplicationScoringAppService>(),
            featureChecker,
            localEventBus ?? Substitute.For<ILocalEventBus>(),
            generationRequestRepository,
            Substitute.For<ICurrentTenant>(),
            NullLogger<RunApplicationAIPipelineJob>.Instance);
    }

    private static IRepository<AIGenerationRequest, Guid> BuildRequestRepository(out List<AIGenerationRequest> requests)
    {
        var requestList = new List<AIGenerationRequest>();
        requests = requestList;
        var repository = Substitute.For<IRepository<AIGenerationRequest, Guid>>();

        repository.GetQueryableAsync()
            .Returns(Task.FromResult<IQueryable<AIGenerationRequest>>(requestList.AsQueryable()));
        repository.UpdateAsync(Arg.Any<AIGenerationRequest>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<AIGenerationRequest>()));

        return repository;
    }

    private static AIGenerationRequest CreateRequest()
    {
        var applicationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        return new AIGenerationRequest(
            Guid.NewGuid(),
            tenantId,
            AIGenerationRequestKeyHelper.PipelineOperationType,
            applicationId,
            AIGenerationRequestKeyHelper.BuildRequestKey(tenantId, applicationId, AIGenerationRequestKeyHelper.PipelineOperationType));
    }
}
