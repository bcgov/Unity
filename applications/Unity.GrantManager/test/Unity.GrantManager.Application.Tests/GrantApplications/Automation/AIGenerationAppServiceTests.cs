using Microsoft.Extensions.Localization;
using NSubstitute;
using Shouldly;
using System;
using System.Threading.Tasks;
using Unity.AI.Generation;
using Unity.AI.Localization;
using Unity.AI.Operations;
using Unity.AI.Settings;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
using Unity.AI.Automation;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Features;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.GrantApplications.Automation;

public class AIGenerationAppServiceTests(ITestOutputHelper outputHelper) : GrantManagerApplicationTestBase(outputHelper)
{
    [Fact]
    public async Task GenerateContentAsync_Should_Return_Completed_Result()
    {
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries").Returns(true);
        featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis").Returns(true);
        featureChecker.IsEnabledAsync("Unity.AI.Scoring").Returns(true);
        var localizer = Substitute.For<IStringLocalizer<AIResource>>();
        var featureGuard = new AIFeatureGuard(featureChecker, localizer);

        var queue = Substitute.For<IApplicationAIGenerationQueue>();
        queue.QueueAllAIStagesAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<string?>())
            .Returns(Task.CompletedTask);
        var currentTenant = Substitute.For<Volo.Abp.MultiTenancy.ICurrentTenant>();
        currentTenant.Id.Returns(Guid.NewGuid());

        var service = new AIGenerationAppService(
            Substitute.For<IAttachmentSummaryService>(),
            queue,
            featureGuard,
            currentTenant);

        var result = await service.GenerateContentAsync(Guid.NewGuid());

        result.ShouldNotBeNull();
        result.Completed.ShouldBeTrue();
        await queue.Received(1).QueueAllAIStagesAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<string?>());
    }
}

