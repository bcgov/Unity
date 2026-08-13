using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Unity.AI.Generation;
using Unity.AI.Operations;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Domain.WorksheetLinks;
using Unity.Flex.Worksheets;
using Unity.Flex.Domain.Scoresheets;
using Unity.GrantManager.ApplicationForms.Mapping;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Forms;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Integrations.Chefs;
using Unity.GrantManager.Reporting.FieldGenerators;
using Unity.Modules.Shared.Correlation;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.ApplicationForms;

public class ApplicationFormVersionAppServiceMappingReviewTests(ITestOutputHelper outputHelper)
    : GrantManagerApplicationTestBase(outputHelper)
{
    [Fact]
    public async Task AcceptMappingSuggestionsAsync_Should_Apply_Selected_Suggestions_And_Keep_Remaining()
    {
        var formVersionId = Guid.NewGuid();
        var selectedSuggestionId = Guid.NewGuid();
        var remainingSuggestionId = Guid.NewGuid();
        var formVersion = new ApplicationFormVersion { SubmissionHeaderMapping = "{}" };
        var repository = Substitute.For<IRepository<ApplicationFormVersion, Guid>>();
        repository.GetAsync(formVersionId).Returns(formVersion);
        var review = CreateReview(formVersionId, [
            new FormMappingSuggestionDto
            {
                Id = selectedSuggestionId,
                SourceField = "ChefsProjectName",
                TargetField = "ProjectName"
            },
            new FormMappingSuggestionDto
            {
                Id = remainingSuggestionId,
                SourceField = "ChefsAmount",
                TargetField = "RequestedAmount"
            }
        ]);
        var reviewRepository = Substitute.For<IGenerationReviewRepository>();
        reviewRepository.FindLatestByOperationAndFormVersionAsync(
                AIGenerationOperations.FormMapping,
                formVersionId)
            .Returns(review);
        var service = CreateService(repository, reviewRepository);

        var result = await service.AcceptMappingSuggestionsAsync(formVersionId, new AcceptMappingSuggestionsDto
        {
            SuggestionIds = [selectedSuggestionId]
        });

        result.SubmissionHeaderMapping.ShouldContain("ProjectName");
        formVersion.SubmissionHeaderMapping.ShouldContain("ProjectName");
        formVersion.SubmissionHeaderMapping.ShouldContain("ChefsProjectName");
        var payload = JsonSerializer.Deserialize<FormMappingReviewPayload>(review.ReviewData)!;
        payload.PendingSuggestions.Select(suggestion => suggestion.Id).ShouldBe([remainingSuggestionId]);
        await repository.Received(1).UpdateAsync(formVersion, true);
        await reviewRepository.Received(1).UpdateAsync(review, true);
    }

    [Fact]
    public async Task AcceptMappingSuggestionsAsync_Should_Leave_State_Unchanged_When_A_Suggestion_Is_Stale()
    {
        var formVersionId = Guid.NewGuid();
        var repository = Substitute.For<IRepository<ApplicationFormVersion, Guid>>();
        var review = CreateReview(formVersionId, [new FormMappingSuggestionDto { Id = Guid.NewGuid() }]);
        var reviewRepository = Substitute.For<IGenerationReviewRepository>();
        reviewRepository.FindLatestByOperationAndFormVersionAsync(
                AIGenerationOperations.FormMapping,
                formVersionId)
            .Returns(review);
        var service = CreateService(repository, reviewRepository);

        await Should.ThrowAsync<UserFriendlyException>(async () =>
        {
            await service.AcceptMappingSuggestionsAsync(
                formVersionId,
                new AcceptMappingSuggestionsDto { SuggestionIds = [Guid.NewGuid()] });
        });

        await repository.DidNotReceive().UpdateAsync(Arg.Any<ApplicationFormVersion>(), Arg.Any<bool>());
        await reviewRepository.DidNotReceive().UpdateAsync(Arg.Any<GenerationReview>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task FinalizeMappingReviewAsync_Should_Allow_Final_Mapping_When_One_Of_Multiple_Drafts_Is_Published_And_Assigned()
    {
        var formVersionId = Guid.NewGuid();
        var formVersion = new ApplicationFormVersion { ApplicationFormId = Guid.NewGuid() };
        var assignedDraft = new Worksheet(Guid.NewGuid(), "ai-assigned", "Assigned AI worksheet");
        assignedDraft.SetPublished(true);
        var unassignedDraft = new Worksheet(Guid.NewGuid(), "ai-unassigned", "Unassigned AI worksheet");
        var mappingReview = new GenerationReview(Guid.NewGuid(), AIGenerationOperations.FormMapping, formVersionId);
        mappingReview.Complete();
        var worksheetReview = new GenerationReview(Guid.NewGuid(), AIGenerationOperations.FormWorksheet, formVersionId);
        worksheetReview.Complete();
        worksheetReview.SetReviewData(JsonSerializer.Serialize(new FormWorksheetReviewPayload
        {
            DraftWorksheetIds = [assignedDraft.Id, unassignedDraft.Id]
        }));

        var repository = Substitute.For<IRepository<ApplicationFormVersion, Guid>>();
        repository.GetAsync(formVersionId).Returns(formVersion);
        var generationService = Substitute.For<IAIGenerationAppService>();
        var reviewRepository = Substitute.For<IGenerationReviewRepository>();
        reviewRepository.FindLatestByOperationAndFormVersionAsync(AIGenerationOperations.FormMapping, formVersionId)
            .Returns(mappingReview);
        reviewRepository.FindLatestByOperationAndFormVersionAsync(AIGenerationOperations.FormWorksheet, formVersionId)
            .Returns(worksheetReview);
        var worksheetRepository = Substitute.For<IWorksheetRepository>();
        worksheetRepository.FindAsync(assignedDraft.Id).Returns(assignedDraft);
        worksheetRepository.FindAsync(unassignedDraft.Id).Returns(unassignedDraft);
        var worksheetLinkRepository = Substitute.For<IWorksheetLinkRepository>();
        worksheetLinkRepository.GetListByCorrelationAsync(formVersionId, CorrelationConsts.FormVersion)
            .Returns([
                new WorksheetLink(Guid.NewGuid(), assignedDraft.Id, formVersionId, CorrelationConsts.FormVersion, string.Empty)
            ]);
        var service = CreateService(repository, reviewRepository, generationService, worksheetRepository, worksheetLinkRepository);

        await service.FinalizeMappingReviewAsync(formVersionId);

        mappingReview.Status.ShouldBe(GenerationReviewStatus.Completed);
        await generationService.Received(1).SubmitAsync(
            AIGenerationOperations.FormMapping,
            Arg.Is<AIGenerationSubmissionDto>(submission =>
                submission != null &&
                submission.ApplicationFormVersionId == formVersionId &&
                submission.ApplicationId == formVersion.ApplicationFormId));
    }

    private static GenerationReview CreateReview(
        Guid formVersionId,
        System.Collections.Generic.List<FormMappingSuggestionDto> suggestions)
    {
        var review = new GenerationReview(
            Guid.NewGuid(),
            AIGenerationOperations.FormMapping,
            formVersionId);
        review.SetReviewData(JsonSerializer.Serialize(new FormMappingReviewPayload
        {
            PendingSuggestions = suggestions
        }));
        return review;
    }

    private ApplicationFormVersionAppService CreateService(
        IRepository<ApplicationFormVersion, Guid> repository,
        IGenerationReviewRepository reviewRepository,
        IAIGenerationAppService? generationService = null,
        IWorksheetRepository? worksheetRepository = null,
        IWorksheetLinkRepository? worksheetLinkRepository = null)
    {
        var service = new ApplicationFormVersionAppService(
            repository,
            Substitute.For<IIntakeFormSubmissionMapper>(),
            Substitute.For<IUnitOfWorkManager>(),
            Substitute.For<IFormsApiService>(),
            Substitute.For<IApplicationFormVersionRepository>(),
            Substitute.For<IApplicationFormSubmissionRepository>(),
            Substitute.For<IReportingFieldsGeneratorService>(),
            Substitute.For<Volo.Abp.Features.IFeatureChecker>(),
            generationService ?? Substitute.For<IAIGenerationAppService>(),
            worksheetRepository ?? Substitute.For<IWorksheetRepository>(),
            Substitute.For<IRepository<CustomField, Guid>>(),
            reviewRepository,
            worksheetLinkRepository ?? Substitute.For<IWorksheetLinkRepository>(),
            Substitute.For<IScoresheetRepository>());
        service.LazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();
        return service;
    }
}
