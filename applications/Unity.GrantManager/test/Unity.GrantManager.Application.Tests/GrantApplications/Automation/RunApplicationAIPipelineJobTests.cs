using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Operations;
using Unity.AI.Settings;
using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Features;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Settings;
using Xunit;
using Xunit.Abstractions;
namespace Unity.GrantManager.GrantApplications.Automation;
public class RunApplicationAIPipelineJobTests(ITestOutputHelper outputHelper) : GrantManagerApplicationTestBase(outputHelper)
{
    private static RunApplicationAIPipelineJob BuildJob(
        IFeatureChecker featureChecker,
        IApplicationScoringService? scoringService = null,
        IAIService? aiService = null,
        ISettingProvider? settingProvider = null)
    {
        var ai = aiService ?? Substitute.For<IAIService>();
        ai.IsAvailableAsync().Returns(true);

        var settings = settingProvider ?? Substitute.For<ISettingProvider>();
        if (settingProvider == null)
        {
            settings.GetOrNullAsync(AISettings.AutomaticGenerationEnabled).Returns("true");
        }

        var aiServices = new AIOperationServices(
            Substitute.For<IAttachmentSummaryService>(),
            Substitute.For<IApplicationAnalysisService>(),
            scoringService ?? Substitute.For<IApplicationScoringService>(),
            ai);

        return new RunApplicationAIPipelineJob(
            aiServices,
            featureChecker,
            Substitute.For<ILocalEventBus>(),
            Substitute.For<ICurrentTenant>(),
            settings,
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

    [Fact]
    public async Task Should_Skip_Pipeline_When_AutomaticGenerationEnabled_Is_False()
    {
        // Arrange - automatic generation OFF at tenant level
        var settings = Substitute.For<ISettingProvider>();
        settings.GetOrNullAsync(AISettings.AutomaticGenerationEnabled).Returns("false");

        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync(Arg.Any<string>()).Returns(true);

        var scoringService = Substitute.For<IApplicationScoringService>();
        var job = BuildJob(featureChecker, scoringService, settingProvider: settings);

        // Act
        await job.ExecuteAsync(new RunApplicationAIPipelineJobArgs { ApplicationId = Guid.NewGuid() });

        // Assert - pipeline never reaches any AI service
        await scoringService.DidNotReceive().RegenerateAndSaveAsync(Arg.Any<Guid>(), Arg.Any<string?>());
        await featureChecker.DidNotReceive().IsEnabledAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task Should_Run_Pipeline_When_AutomaticGenerationEnabled_Is_True()
    {
        // Arrange - automatic generation ON, all features enabled
        var settings = Substitute.For<ISettingProvider>();
        settings.GetOrNullAsync(AISettings.AutomaticGenerationEnabled).Returns("true");

        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.Scoring").Returns(true);
        featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries").Returns(false);
        featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis").Returns(false);

        var scoringService = Substitute.For<IApplicationScoringService>();
        scoringService.RegenerateAndSaveAsync(Arg.Any<Guid>(), Arg.Any<string?>()).Returns("{}");

        var job = BuildJob(featureChecker, scoringService, settingProvider: settings);

        // Act
        await job.ExecuteAsync(new RunApplicationAIPipelineJobArgs { ApplicationId = Guid.NewGuid() });

        // Assert - pipeline reached the scoring service
        await scoringService.Received(1).RegenerateAndSaveAsync(Arg.Any<Guid>(), Arg.Any<string?>());
    }
}
