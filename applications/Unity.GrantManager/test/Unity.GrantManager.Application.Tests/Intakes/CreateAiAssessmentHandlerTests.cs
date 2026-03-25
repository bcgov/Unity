using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI.Settings;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Assessments;
using Unity.GrantManager.Intakes.Events;
using Unity.GrantManager.Intakes.Handlers;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;
using Volo.Abp.Settings;
using Volo.Abp.Uow;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.Intakes;

public class CreateAiAssessmentHandlerTests : GrantManagerApplicationTestBase
{
    private readonly AssessmentManager _assessmentManager;
    private readonly IRepository<Assessment, System.Guid> _assessmentRepository;
    private readonly IRepository<Application, System.Guid> _applicationRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public CreateAiAssessmentHandlerTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        _assessmentManager = GetRequiredService<AssessmentManager>();
        _assessmentRepository = GetRequiredService<IRepository<Assessment, System.Guid>>();
        _applicationRepository = GetRequiredService<IRepository<Application, System.Guid>>();
        _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task HandleEventAsync_Should_Skip_When_Application_Is_Null()
    {
        // Arrange
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.Scoring").Returns(true);
        var settingProvider = Substitute.For<ISettingProvider>();
        settingProvider.GetOrNullAsync(AISettings.ScoringAssistantEnabled).Returns("true");
        var handler = new CreateAiAssessmentHandler(_assessmentManager, featureChecker, settingProvider, NullLogger<CreateAiAssessmentHandler>.Instance);

        using var uow = _unitOfWorkManager.Begin();
        var beforeCount = (await _assessmentRepository.GetQueryableAsync()).Count(a => a.IsAiAssessment);

        // Act — null application should be handled gracefully
        await handler.HandleEventAsync(new AiScoresheetAnswersGeneratedEvent { Application = null });

        // Assert — no AI assessment created
        var afterCount = (await _assessmentRepository.GetQueryableAsync()).Count(a => a.IsAiAssessment);
        afterCount.ShouldBe(beforeCount);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task HandleEventAsync_Should_Skip_When_Feature_Disabled()
    {
        // Arrange — feature disabled
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.Scoring").Returns(false);
        var settingProvider = Substitute.For<ISettingProvider>();
        var handler = new CreateAiAssessmentHandler(_assessmentManager, featureChecker, settingProvider, NullLogger<CreateAiAssessmentHandler>.Instance);

        using var uow = _unitOfWorkManager.Begin();
        var application = (await _applicationRepository.GetListAsync())[0];
        var beforeCount = (await _assessmentRepository.GetQueryableAsync()).Count(a => a.IsAiAssessment);

        // Act
        await handler.HandleEventAsync(new AiScoresheetAnswersGeneratedEvent { Application = application });

        // Assert — no new AI assessment created
        var afterCount = (await _assessmentRepository.GetQueryableAsync()).Count(a => a.IsAiAssessment);
        afterCount.ShouldBe(beforeCount);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task HandleEventAsync_Should_Skip_When_Feature_Enabled_But_Setting_Disabled()
    {
        // Arrange — feature ON but tenant setting OFF
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.Scoring").Returns(true);
        var settingProvider = Substitute.For<ISettingProvider>();
        settingProvider.GetOrNullAsync(AISettings.ScoringAssistantEnabled).Returns("false");
        var handler = new CreateAiAssessmentHandler(_assessmentManager, featureChecker, settingProvider, NullLogger<CreateAiAssessmentHandler>.Instance);

        using var uow = _unitOfWorkManager.Begin();
        var application = (await _applicationRepository.GetListAsync())[0];
        var beforeCount = (await _assessmentRepository.GetQueryableAsync()).Count(a => a.IsAiAssessment);

        // Act
        await handler.HandleEventAsync(new AiScoresheetAnswersGeneratedEvent { Application = application });

        // Assert — no new AI assessment created
        var afterCount = (await _assessmentRepository.GetQueryableAsync()).Count(a => a.IsAiAssessment);
        afterCount.ShouldBe(beforeCount);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task HandleEventAsync_Should_Create_AI_Assessment_When_Feature_Enabled_And_Setting_Enabled()
    {
        // Arrange — feature ON and tenant setting ON
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.Scoring").Returns(true);
        var settingProvider = Substitute.For<ISettingProvider>();
        settingProvider.GetOrNullAsync(AISettings.ScoringAssistantEnabled).Returns("true");
        var handler = new CreateAiAssessmentHandler(_assessmentManager, featureChecker, settingProvider, NullLogger<CreateAiAssessmentHandler>.Instance);

        using var uow = _unitOfWorkManager.Begin();
        // Application2 has no AI assessment seeded — ideal for the happy path
        var application = await _applicationRepository.GetAsync(GrantManagerTestData.Application2_Id);

        // Act
        await handler.HandleEventAsync(new AiScoresheetAnswersGeneratedEvent { Application = application });

        // Assert
        var aiAssessment = (await _assessmentRepository.GetQueryableAsync())
            .FirstOrDefault(a => a.ApplicationId == GrantManagerTestData.Application2_Id && a.IsAiAssessment);
        aiAssessment.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task HandleEventAsync_Should_Be_Idempotent()
    {
        // Arrange — AiAssessment1_Id is already seeded for Application1_Id
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.Scoring").Returns(true);
        var settingProvider = Substitute.For<ISettingProvider>();
        settingProvider.GetOrNullAsync(AISettings.ScoringAssistantEnabled).Returns("true");
        var handler = new CreateAiAssessmentHandler(_assessmentManager, featureChecker, settingProvider, NullLogger<CreateAiAssessmentHandler>.Instance);

        using var uow = _unitOfWorkManager.Begin();
        var application = await _applicationRepository.GetAsync(GrantManagerTestData.Application1_Id);
        var beforeCount = (await _assessmentRepository.GetQueryableAsync())
            .Count(a => a.ApplicationId == GrantManagerTestData.Application1_Id && a.IsAiAssessment);

        // Act — call handler twice
        await handler.HandleEventAsync(new AiScoresheetAnswersGeneratedEvent { Application = application });
        await handler.HandleEventAsync(new AiScoresheetAnswersGeneratedEvent { Application = application });

        // Assert — still only one AI assessment for this application
        var afterCount = (await _assessmentRepository.GetQueryableAsync())
            .Count(a => a.ApplicationId == GrantManagerTestData.Application1_Id && a.IsAiAssessment);
        afterCount.ShouldBe(beforeCount);
    }
}
