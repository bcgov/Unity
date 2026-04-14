using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI.Settings;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Assessments;
using Unity.GrantManager.GrantApplications.Automation.Events;
using Unity.GrantManager.GrantApplications.Automation.Handlers;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;
using Volo.Abp.Settings;
using Volo.Abp.Uow;
using Xunit;
using Xunit.Abstractions;
namespace Unity.GrantManager.GrantApplications.Automation;
public class CreateAIAssessmentOnScoringGeneratedHandlerTests : GrantManagerApplicationTestBase
{
    private readonly AssessmentManager _assessmentManager;
    private readonly IRepository<Assessment, Guid> _assessmentRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    public CreateAIAssessmentOnScoringGeneratedHandlerTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        _assessmentManager = GetRequiredService<AssessmentManager>();
        _assessmentRepository = GetRequiredService<IRepository<Assessment, Guid>>();
        _applicationRepository = GetRequiredService<IApplicationRepository>();
        _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
    }
    private CreateAIAssessmentOnScoringGeneratedHandler BuildHandler(
        IFeatureChecker featureChecker,
        ISettingProvider? settingProvider = null)
    {
        var settings = settingProvider ?? Substitute.For<ISettingProvider>();
        if (settingProvider == null)
        {
            settings.GetOrNullAsync(AISettings.AutomaticGenerationEnabled).Returns("true");
        }

        return new CreateAIAssessmentOnScoringGeneratedHandler(
            _assessmentManager,
            _applicationRepository,
            featureChecker,
            settings,
            _unitOfWorkManager,
            NullLogger<CreateAIAssessmentOnScoringGeneratedHandler>.Instance);
    }
    [Fact]
    [Trait("Category", "Integration")]
    public async Task HandleEventAsync_Should_Skip_When_Application_Id_Is_Empty()
    {
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.Scoring").Returns(true);
        var handler = BuildHandler(featureChecker);
        using var uow = _unitOfWorkManager.Begin();
        var beforeCount = (await _assessmentRepository.GetQueryableAsync()).Count(a => a.IsAiAssessment);
        await handler.HandleEventAsync(new ApplicationAIScoringGeneratedEvent { ApplicationId = Guid.Empty });
        var afterCount = (await _assessmentRepository.GetQueryableAsync()).Count(a => a.IsAiAssessment);
        afterCount.ShouldBe(beforeCount);
    }
    [Fact]
    [Trait("Category", "Integration")]
    public async Task HandleEventAsync_Should_Skip_When_Feature_Disabled()
    {
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.Scoring").Returns(false);
        var handler = BuildHandler(featureChecker);
        using var uow = _unitOfWorkManager.Begin();
        var application = (await _applicationRepository.GetListAsync())[0];
        var beforeCount = (await _assessmentRepository.GetQueryableAsync()).Count(a => a.IsAiAssessment);
        await handler.HandleEventAsync(new ApplicationAIScoringGeneratedEvent { ApplicationId = application.Id });
        var afterCount = (await _assessmentRepository.GetQueryableAsync()).Count(a => a.IsAiAssessment);
        afterCount.ShouldBe(beforeCount);
    }
    [Fact]
    [Trait("Category", "Integration")]
    public async Task HandleEventAsync_Should_Create_AI_Assessment_When_Feature_Enabled()
    {
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.Scoring").Returns(true);
        var handler = BuildHandler(featureChecker);
        using var uow = _unitOfWorkManager.Begin();
        var application = await _applicationRepository.GetAsync(GrantManagerTestData.Application2_Id);
        await handler.HandleEventAsync(new ApplicationAIScoringGeneratedEvent { ApplicationId = application.Id });
        var aiAssessment = (await _assessmentRepository.GetQueryableAsync())
            .FirstOrDefault(a => a.ApplicationId == GrantManagerTestData.Application2_Id && a.IsAiAssessment);
        aiAssessment.ShouldNotBeNull();
    }
    [Fact]
    [Trait("Category", "Integration")]
    public async Task HandleEventAsync_Should_Be_Idempotent()
    {
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.Scoring").Returns(true);
        var handler = BuildHandler(featureChecker);
        using var uow = _unitOfWorkManager.Begin();
        var application = await _applicationRepository.GetAsync(GrantManagerTestData.Application1_Id);
        var beforeCount = (await _assessmentRepository.GetQueryableAsync())
            .Count(a => a.ApplicationId == GrantManagerTestData.Application1_Id && a.IsAiAssessment);
        await handler.HandleEventAsync(new ApplicationAIScoringGeneratedEvent { ApplicationId = application.Id });
        await handler.HandleEventAsync(new ApplicationAIScoringGeneratedEvent { ApplicationId = application.Id });
        var afterCount = (await _assessmentRepository.GetQueryableAsync())
            .Count(a => a.ApplicationId == GrantManagerTestData.Application1_Id && a.IsAiAssessment);
        afterCount.ShouldBe(beforeCount);
    }
}
