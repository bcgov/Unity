using NSubstitute;
using Shouldly;
using System;
using System.Threading.Tasks;
using Unity.AI.Automation;
using Unity.AI.RateLimit;
using Unity.GrantManager.Applicants;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Flex;
using Unity.GrantManager.GlobalTag;
using Unity.GrantManager.Identity;
using Unity.GrantManager.Payments;
using Unity.Payments.PaymentRequests;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Features;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.GrantApplications;

public class GrantApplicationAIGenerationStatusTests(ITestOutputHelper outputHelper) : GrantManagerApplicationTestBase(outputHelper)
{
    [Fact]
    public async Task GetAIGenerationStatusAsync_Should_Return_RateLimit_State_When_No_Generation_Request_Exists()
    {
        var applicationId = Guid.NewGuid();
        var aiGenerationStatusAppService = Substitute.For<IAIGenerationStatusAppService>();
        aiGenerationStatusAppService
            .GetLatestAsync(applicationId, AIGenerationRequestKeyHelper.ApplicationAnalysisOperationType, null)
            .Returns((AIGenerationRequestDto?)null);

        var aiRateLimiter = Substitute.For<IAIRateLimiter>();
        aiRateLimiter.GetStateAsync().Returns(new AIRateLimitStateDto
        {
            IsGenerating = true,
            RetryAfterSeconds = 17
        });

        var appService = CreateAppService(aiGenerationStatusAppService, aiRateLimiter);

        var result = await appService.GetAIGenerationStatusAsync(
            applicationId,
            AIGenerationRequestKeyHelper.ApplicationAnalysisOperationType);

        result.ShouldNotBeNull();
        result.GenerationRequest.ShouldBeNull();
        result.IsGenerating.ShouldBeTrue();
        result.RetryAfterSeconds.ShouldBe(17);
        await aiGenerationStatusAppService.Received(1)
            .GetLatestAsync(applicationId, AIGenerationRequestKeyHelper.ApplicationAnalysisOperationType, null);
        await aiRateLimiter.Received(1).GetStateAsync();
    }

    [Fact]
    public async Task QueueApplicationAnalysisAsync_Should_Throw_When_Latest_Request_Is_Null()
    {
        var applicationId = Guid.NewGuid();
        var aiGenerationStatusAppService = Substitute.For<IAIGenerationStatusAppService>();
        aiGenerationStatusAppService
            .GetLatestAsync(applicationId, AIGenerationRequestKeyHelper.ApplicationAnalysisOperationType, null)
            .Returns((AIGenerationRequestDto?)null);

        var aiRateLimiter = Substitute.For<IAIRateLimiter>();
        aiRateLimiter.GetStateAsync().Returns(new AIRateLimitStateDto
        {
            IsGenerating = true,
            RetryAfterSeconds = 11
        });

        var queue = Substitute.For<IApplicationAIGenerationQueue>();
        queue
            .QueueApplicationAnalysisAsync(applicationId, null, null)
            .Returns(Task.CompletedTask);

        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker
            .IsEnabledAsync("Unity.AI.ApplicationAnalysis")
            .Returns(true);

        var appService = CreateAppService(aiGenerationStatusAppService, aiRateLimiter, queue, featureChecker);

        await Should.ThrowAsync<UserFriendlyException>(() => appService.QueueApplicationAnalysisAsync(applicationId));

        await queue.Received(1).QueueApplicationAnalysisAsync(applicationId, null, null);
        await aiGenerationStatusAppService.Received(1)
            .GetLatestAsync(applicationId, AIGenerationRequestKeyHelper.ApplicationAnalysisOperationType, null);
        await aiRateLimiter.DidNotReceive().GetStateAsync();
    }

    private GrantApplicationAppService CreateAppService(
        IAIGenerationStatusAppService aiGenerationStatusAppService,
        IAIRateLimiter aiRateLimiter,
        IApplicationAIGenerationQueue? aiGenerationQueue = null,
        IFeatureChecker? featureChecker = null)
    {
        var appService = new GrantApplicationAppService(
            Substitute.For<IApplicationManager>(),
            Substitute.For<IApplicationRepository>(),
            Substitute.For<IApplicationChefsFileAttachmentRepository>(),
            Substitute.For<IApplicationStatusRepository>(),
            Substitute.For<IApplicationFormSubmissionRepository>(),
            Substitute.For<IApplicantRepository>(),
            Substitute.For<IApplicationFormRepository>(),
            Substitute.For<IApplicantAgentRepository>(),
            Substitute.For<IApplicantAddressRepository>(),
            Substitute.For<IApplicantSupplierAppService>(),
            Substitute.For<IPaymentRequestAppService>(),
            aiGenerationQueue ?? Substitute.For<IApplicationAIGenerationQueue>(),
            aiGenerationStatusAppService,
            aiRateLimiter,
            featureChecker ?? Substitute.For<IFeatureChecker>());

        appService.LazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();
        return appService;
    }
}
