using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Operations;
using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
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
        IApplicationScoringService? scoringService = null,
        IAIService? aiService = null)
    {
        var ai = aiService ?? Substitute.For<IAIService>();
        ai.IsAvailableAsync().Returns(true);
        return new RunApplicationAIPipelineJob(
            Substitute.For<IAttachmentSummaryService>(),
            Substitute.For<IApplicationAnalysisService>(),
            scoringService ?? Substitute.For<IApplicationScoringService>(),
            ai,
            featureChecker,
            Substitute.For<ILocalEventBus>(),
            Substitute.For<ICurrentTenant>(),
            NullLogger<RunApplicationAIPipelineJob>.Instance);
    }
    [Fact]
    public async Task ExecuteAsync_Should_Skip_Scoring_When_Feature_Disabled()
    {
        // Arrange - scoring feature OFF
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.Scoring").Returns(false);
        featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries").Returns(false);
        featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis").Returns(false);
        var scoringService = Substitute.For<IApplicationScoringService>();
        var job = BuildJob(featureChecker, scoringService);
        // Act
        await job.ExecuteAsync(new RunApplicationAIPipelineJobArgs { ApplicationId = Guid.NewGuid() });
        // Assert - scoring service never called
        await scoringService.DidNotReceive().RegenerateAndSaveAsync(Arg.Any<Guid>(), Arg.Any<string?>());
    }
}
