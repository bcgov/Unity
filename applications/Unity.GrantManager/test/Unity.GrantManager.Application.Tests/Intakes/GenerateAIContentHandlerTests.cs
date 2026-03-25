using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Threading.Tasks;
using Unity.AI.Settings;
using Unity.GrantManager.AI;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Intakes.Events;
using Unity.GrantManager.Intakes.Handlers;
using Unity.Flex.Domain.Scoresheets;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Features;
using Volo.Abp.Settings;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.Intakes;

public class GenerateAIContentHandlerTests(ITestOutputHelper outputHelper) : GrantManagerApplicationTestBase(outputHelper)
{
    private static GenerateAIContentHandler BuildHandler(
        IFeatureChecker featureChecker,
        ISettingProvider settingProvider,
        IAIService? aiService = null)
    {
        var handler = new GenerateAIContentHandler(
            aiService ?? Substitute.For<IAIService>(),
            Substitute.For<ISubmissionAppService>(),
            Substitute.For<IApplicationChefsFileAttachmentRepository>(),
            Substitute.For<IApplicationRepository>(),
            Substitute.For<IApplicationFormSubmissionRepository>(),
            NullLogger<GenerateAIContentHandler>.Instance,
            Substitute.For<IScoresheetRepository>(),
            Substitute.For<IApplicationFormRepository>(),
            Substitute.For<IApplicationFormVersionRepository>(),
            featureChecker)
        {
            LocalEventBus = Substitute.For<ILocalEventBus>(),
            SettingProvider = settingProvider
        };
        return handler;
    }

    [Fact]
    public async Task HandleEventAsync_Should_Skip_Scoring_When_Feature_Disabled()
    {
        // Arrange — scoring feature OFF
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.Scoring").Returns(false);
        featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries").Returns(false);
        featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis").Returns(false);
        var settingProvider = Substitute.For<ISettingProvider>();
        var aiService = Substitute.For<IAIService>();

        var handler = BuildHandler(featureChecker, settingProvider, aiService);
        var application = new Application();

        // Act
        await handler.HandleEventAsync(new ApplicationProcessEvent { Application = application });

        // Assert — setting never checked, AI service never called
        await settingProvider.DidNotReceive().GetOrNullAsync(AISettings.ScoringAssistantEnabled);
        await aiService.DidNotReceive().GenerateScoresheetSectionAsync(Arg.Any<ScoresheetSectionRequest>());
    }

    [Fact]
    public async Task HandleEventAsync_Should_Skip_Scoring_When_Feature_Enabled_But_Setting_Disabled()
    {
        // Arrange — scoring feature ON, tenant setting OFF
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.Scoring").Returns(true);
        featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries").Returns(false);
        featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis").Returns(false);
        var settingProvider = Substitute.For<ISettingProvider>();
        settingProvider.GetOrNullAsync(AISettings.ScoringAssistantEnabled).Returns("false");
        var aiService = Substitute.For<IAIService>();
        aiService.IsAvailableAsync().Returns(true);

        var handler = BuildHandler(featureChecker, settingProvider, aiService);
        var application = new Application();

        // Act
        await handler.HandleEventAsync(new ApplicationProcessEvent { Application = application });

        // Assert — all three features effectively disabled → early exit, scoresheet never called
        await aiService.DidNotReceive().GenerateScoresheetSectionAsync(Arg.Any<ScoresheetSectionRequest>());
    }

    [Fact]
    public async Task HandleEventAsync_Should_Not_Check_Setting_When_Feature_Disabled()
    {
        // Arrange — scoring feature OFF
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.Scoring").Returns(false);
        featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries").Returns(false);
        featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis").Returns(false);
        var settingProvider = Substitute.For<ISettingProvider>();

        var handler = BuildHandler(featureChecker, settingProvider);
        var application = new Application();

        // Act
        await handler.HandleEventAsync(new ApplicationProcessEvent { Application = application });

        // Assert — setting provider is never consulted when feature is OFF
        await settingProvider.DidNotReceive().GetOrNullAsync(Arg.Any<string>());
    }
}
