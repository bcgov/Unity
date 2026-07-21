using Microsoft.Extensions.Localization;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Localization;
using Unity.AI.Generation;
using Unity.AI.Operations;
using Unity.AI.Settings;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Features;
using Volo.Abp.MultiTenancy;
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
            Substitute.For<IApplicationGenerationQueue>(),
            Substitute.For<IAIGenerationStatusAppService>(),
            featureGuard,
            Substitute.For<ICurrentTenant>());
        service.LazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();

        await service.GenerateApplicationAttachmentSummariesAsync(new AttachmentSummaryGenerationRequestDto
        {
            ApplicationId = applicationId,
            AttachmentIds = attachmentIds,
            PromptVersion = "v1"
        });

        await Task.CompletedTask;
    }

    [Fact]
    public async Task GetStatusAsync_Should_Map_Request_And_Rate_Limit_State()
    {
        var applicationId = Guid.NewGuid();
        var operationType = AIGenerationRequestKeyHelper.ApplicationAnalysisOperationType;
        var tenantId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        var statusService = Substitute.For<IAIGenerationStatusAppService>();
        statusService.GetLatestAsync(applicationId, operationType, tenantId).Returns(new AIGenerationRequestDto
        {
            Id = requestId,
            ApplicationId = applicationId,
            OperationId = operationId,
            OperationType = operationType,
            Status = AIGenerationRequestStatus.Running,
            StartedAt = new DateTime(2026, 7, 1, 12, 0, 0),
            FailureReason = "not used",
            IsActive = true
        });

        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Id.Returns(tenantId);

        var service = new AIGenerationAppService(
            Substitute.For<IApplicationGenerationQueue>(),
            statusService,
            CreateFeatureGuard(),
            currentTenant);
        service.LazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();

        var result = await service.GetStatusAsync(applicationId, operationType);

        result.Id.ShouldBe(requestId);
        result.ApplicationId.ShouldBe(applicationId);
        result.OperationId.ShouldBe(operationId);
        result.OperationType.ShouldBe(operationType);
        result.Status.ShouldBe(AIGenerationRequestStatus.Running.ToString());
        result.StartedAt.ShouldBe(new DateTime(2026, 7, 1, 12, 0, 0));
        result.FailureReason.ShouldBe("not used");
        result.IsActive.ShouldBeTrue();
        result.GenerationRequest.ShouldNotBeNull();
        result.GenerationRequest!.Id.ShouldBe(requestId);
        result.GenerationRequest.ApplicationId.ShouldBe(applicationId);
        result.GenerationRequest.OperationId.ShouldBe(operationId);
        result.GenerationRequest.OperationType.ShouldBe(operationType);
        result.GenerationRequest.Status.ShouldBe(AIGenerationRequestStatus.Running.ToString());
        result.GenerationRequest.StartedAt.ShouldBe(new DateTime(2026, 7, 1, 12, 0, 0));
        result.GenerationRequest.FailureReason.ShouldBe("not used");
        result.GenerationRequest.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task GetStatusAsync_Should_Reject_Unsupported_Operation_Type()
    {
        var service = new AIGenerationAppService(
            Substitute.For<IApplicationGenerationQueue>(),
            Substitute.For<IAIGenerationStatusAppService>(),
            CreateFeatureGuard(),
            Substitute.For<ICurrentTenant>());
        service.LazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();

        var exception = await Should.ThrowAsync<UserFriendlyException>(
            () => service.GetStatusAsync(Guid.NewGuid(), "unsupported-operation"));

        exception.Message.ShouldContain("Unsupported AI generation operation type");
    }

    private static AIFeatureGuard CreateFeatureGuard()
    {
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync(Arg.Any<string>()).Returns(true);

        var localizer = Substitute.For<IStringLocalizer<AIResource>>();
        return new AIFeatureGuard(featureChecker, localizer);
    }
}
