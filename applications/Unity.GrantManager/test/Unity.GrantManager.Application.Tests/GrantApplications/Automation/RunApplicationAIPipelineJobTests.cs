using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Attachments;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
using Unity.GrantManager.GrantApplications.Automation.Events;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Features;
using Volo.Abp.MultiTenancy;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.GrantApplications.Automation;

public class RunApplicationAIPipelineJobTests(ITestOutputHelper outputHelper) : GrantManagerApplicationTestBase(outputHelper)
{
    private static RunApplicationAIPipelineJob BuildJob(
        IFeatureChecker featureChecker,
        IApplicationScoringAppService? scoringService = null)
    {
        var attachmentService = Substitute.For<IAttachmentSummaryAppService>();
        attachmentService.GenerateAttachmentSummariesForPipelineAsync(Arg.Any<List<Guid>>(), Arg.Any<string?>())
            .Returns(Task.FromResult(new List<AttachmentSummaryResultDto>()));

        var analysisService = Substitute.For<IApplicationAnalysisAppService>();
        analysisService.GenerateApplicationAnalysisForPipelineAsync(Arg.Any<Guid>(), Arg.Any<string?>())
            .Returns(Task.FromResult(new ApplicationAnalysisResultDto { Completed = true }));

        return new RunApplicationAIPipelineJob(
            Substitute.For<IApplicationChefsFileAttachmentRepository>(),
            attachmentService,
            analysisService,
            scoringService ?? Substitute.For<IApplicationScoringAppService>(),
            featureChecker,
            Substitute.For<ILocalEventBus>(),
            Substitute.For<ICurrentTenant>(),
            NullLogger<RunApplicationAIPipelineJob>.Instance);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Skip_Scoring_When_Feature_Disabled()
    {
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.Scoring").Returns(false);
        featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries").Returns(false);
        featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis").Returns(false);

        var scoringService = Substitute.For<IApplicationScoringAppService>();
        scoringService.GenerateApplicationScoringForPipelineAsync(Arg.Any<Guid>(), Arg.Any<string?>())
            .Returns(Task.FromResult(new ApplicationScoringResultDto { Completed = true }));

        var job = BuildJob(featureChecker, scoringService);

        await job.ExecuteAsync(new RunApplicationAIPipelineJobArgs { ApplicationId = Guid.NewGuid() });

        await scoringService.DidNotReceive().GenerateApplicationScoringForPipelineAsync(Arg.Any<Guid>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task ExecuteAsync_Should_Run_Scoring_When_Feature_Enabled()
    {
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.Scoring").Returns(true);
        featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries").Returns(false);
        featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis").Returns(false);

        var scoringService = Substitute.For<IApplicationScoringAppService>();
        scoringService.GenerateApplicationScoringForPipelineAsync(Arg.Any<Guid>(), Arg.Any<string?>())
            .Returns(Task.FromResult(new ApplicationScoringResultDto { Completed = true }));

        var job = BuildJob(featureChecker, scoringService);

        await job.ExecuteAsync(new RunApplicationAIPipelineJobArgs { ApplicationId = Guid.NewGuid() });

        await scoringService.Received(1).GenerateApplicationScoringForPipelineAsync(Arg.Any<Guid>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task ExecuteAsync_Should_Publish_Scoring_Event_When_Scoring_Completes()
    {
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.Scoring").Returns(true);
        featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries").Returns(false);
        featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis").Returns(false);

        var scoringService = Substitute.For<IApplicationScoringAppService>();
        scoringService.GenerateApplicationScoringForPipelineAsync(Arg.Any<Guid>(), Arg.Any<string?>())
            .Returns(Task.FromResult(new ApplicationScoringResultDto { Completed = true }));

        var eventBus = Substitute.For<ILocalEventBus>();
        var job = new RunApplicationAIPipelineJob(
            Substitute.For<IApplicationChefsFileAttachmentRepository>(),
            Substitute.For<IAttachmentSummaryAppService>(),
            Substitute.For<IApplicationAnalysisAppService>(),
            scoringService,
            featureChecker,
            eventBus,
            Substitute.For<ICurrentTenant>(),
            NullLogger<RunApplicationAIPipelineJob>.Instance);

        await job.ExecuteAsync(new RunApplicationAIPipelineJobArgs { ApplicationId = Guid.NewGuid() });

        await eventBus.Received(1).PublishAsync(Arg.Is<ApplicationAIScoringGeneratedEvent>(e => e.ApplicationId != Guid.Empty));
    }
}
