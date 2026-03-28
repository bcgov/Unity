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
public class GenerateApplicationAIContentJobTests(ITestOutputHelper outputHelper) : GrantManagerApplicationTestBase(outputHelper)
{
    private static GenerateApplicationAIContentJob BuildJob(
        IFeatureChecker featureChecker,
        ISettingProvider settingProvider,
        IApplicationScoringService? scoringService = null,
        IAIService? aiService = null)
    {
        var ai = aiService ?? Substitute.For<IAIService>();
        ai.IsAvailableAsync().Returns(true);
        return new GenerateApplicationAIContentJob(
            Substitute.For<IAttachmentSummaryService>(),
            Substitute.For<IApplicationAnalysisService>(),
            scoringService ?? Substitute.For<IApplicationScoringService>(),
            ai,
            featureChecker,
            settingProvider,
            Substitute.For<ILocalEventBus>(),
            Substitute.For<ICurrentTenant>(),
            NullLogger<GenerateApplicationAIContentJob>.Instance);
    }
    [Fact]
    public async Task ExecuteAsync_Should_Skip_Scoring_When_Feature_Disabled()
    {
        // Arrange - scoring feature OFF
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.Scoring").Returns(false);
        featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries").Returns(false);
        featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis").Returns(false);
        var settingProvider = Substitute.For<ISettingProvider>();
        var scoringService = Substitute.For<IApplicationScoringService>();
        var job = BuildJob(featureChecker, settingProvider, scoringService);
        // Act
        await job.ExecuteAsync(new GenerateApplicationAIContentJobArgs { ApplicationId = Guid.NewGuid() });
        // Assert - setting never checked, scoring service never called
        await settingProvider.DidNotReceive().GetOrNullAsync(AISettings.ScoringAssistantEnabled);
        await scoringService.DidNotReceive().RegenerateAndSaveAsync(Arg.Any<Guid>(), Arg.Any<string?>());
    }
    [Fact]
    public async Task ExecuteAsync_Should_Skip_Scoring_When_Feature_Enabled_But_Setting_Disabled()
    {
        // Arrange - scoring feature ON, tenant setting OFF
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.Scoring").Returns(true);
        featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries").Returns(false);
        featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis").Returns(false);
        var settingProvider = Substitute.For<ISettingProvider>();
        settingProvider.GetOrNullAsync(AISettings.ScoringAssistantEnabled).Returns("false");
        var scoringService = Substitute.For<IApplicationScoringService>();
        var job = BuildJob(featureChecker, settingProvider, scoringService);
        // Act
        await job.ExecuteAsync(new GenerateApplicationAIContentJobArgs { ApplicationId = Guid.NewGuid() });
        // Assert - scoring service never called
        await scoringService.DidNotReceive().RegenerateAndSaveAsync(Arg.Any<Guid>(), Arg.Any<string?>());
    }
    [Fact]
    public async Task ExecuteAsync_Should_Not_Check_Setting_When_Feature_Disabled()
    {
        // Arrange - scoring feature OFF
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.Scoring").Returns(false);
        featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries").Returns(false);
        featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis").Returns(false);
        var settingProvider = Substitute.For<ISettingProvider>();
        var job = BuildJob(featureChecker, settingProvider);
        // Act
        await job.ExecuteAsync(new GenerateApplicationAIContentJobArgs { ApplicationId = Guid.NewGuid() });
        // Assert - setting provider never consulted when all features are OFF
        await settingProvider.DidNotReceive().GetOrNullAsync(Arg.Any<string>());
    }
}