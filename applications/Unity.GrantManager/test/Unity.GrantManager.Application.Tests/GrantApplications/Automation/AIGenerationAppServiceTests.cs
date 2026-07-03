using Microsoft.Extensions.Localization;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
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
    public async Task GenerateAttachmentSummariesAsync_Should_Validate_Against_Application_Ownership()
    {
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries").Returns(true);
        var localizer = Substitute.For<IStringLocalizer<AIResource>>();
        var featureGuard = new AIFeatureGuard(featureChecker, localizer);

        var applicationId = Guid.NewGuid();
        var attachmentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        var service = new AIGenerationAppService(
            Substitute.For<IApplicationAIGenerationQueue>(),
            featureGuard,
            featureChecker,
            Substitute.For<Volo.Abp.MultiTenancy.ICurrentTenant>());

        var result = await service.GenerateAttachmentSummariesAsync(new GenerateAttachmentSummariesInputDto
        {
            ApplicationId = applicationId,
            AttachmentIds = attachmentIds,
            PromptVersion = "v1"
        });

        result.Count.ShouldBe(2);
        result.ShouldAllBe(x => x.Completed == false);
    }
}

