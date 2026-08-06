using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Features;
using Unity.AI.Generation;
using Unity.AI.Operations;
using Unity.AI.Requests;
using Unity.AI.Responses;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Worksheets;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.ApplicationForms.Mapping;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Forms;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Intakes.Mapping;
using Unity.GrantManager.Reporting.FieldGenerators;
using Unity.GrantManager.Integrations.Chefs;
using Unity.Modules.Shared.Features;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Features;
using Volo.Abp.Uow;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.ApplicationForms;

public class ApplicationFormVersionAppServiceTests(ITestOutputHelper outputHelper) : GrantManagerApplicationTestBase(outputHelper)
{
    [Fact]
    public async Task GenerateMappingAsync_Should_Submit_Form_Mapping_Generation()
    {
        var formVersionId = Guid.NewGuid();
        var repository = Substitute.For<IRepository<ApplicationFormVersion, Guid>>();
        var formVersion = new ApplicationFormVersion
        {
            ApplicationFormId = Guid.NewGuid()
        };
        repository.GetAsync(formVersionId).Returns(formVersion);

        var generationService = Substitute.For<IAIGenerationAppService>();
        var service = CreateService(repository, generationService);
        service.LazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();

        var result = await service.GenerateMappingAsync(formVersionId);

        result.ApplicationFormVersionId.ShouldBe(formVersionId);
        await generationService.Received(1).SubmitAsync(
            AIGenerationOperations.FormMapping,
            Arg.Is<AIGenerationSubmissionDto>(request =>
                request != null
                && request.ApplicationId == formVersion.ApplicationFormId
                && request.ApplicationFormVersionId == formVersionId));
    }

    [Fact]
    public void FormMappingPromptData_Should_UseEmptyObject_When_NoExistingMappingIsAvailable()
    {
        var promptData = FormMappingPromptDataBuilder.Build(new ApplicationFormMappingReadModelDto());

        promptData.GetProperty("existingMapping").GetRawText().ShouldBe("{}");
    }

    [Fact]
    public void FormMappingPromptData_Should_Include_Accepted_Worksheet_Fields()
    {
        var promptData = FormMappingPromptDataBuilder.Build(
            new ApplicationFormMappingReadModelDto(),
            ["custom_project_name"]);

        promptData.GetProperty("unityData")
            .GetProperty("customFields")[0]
            .GetProperty("Fields")[0]
            .GetProperty("Name")
            .GetString()
            .ShouldBe("custom_project_name");
    }

    [Fact]
    public async Task GetPendingAiWorksheetAsync_Should_Return_Unpublished_Worksheet_Fields()
    {
        var formVersionId = Guid.NewGuid();
        var formId = Guid.NewGuid();
        var formVersion = new ApplicationFormVersion
        {
            ApplicationFormId = formId
        };
        var worksheet = BuildAiWorksheet(formId, formVersionId, published: false);
        var formVersionRepository = Substitute.For<IApplicationFormVersionRepository>();
        formVersionRepository.GetAsync(formVersionId).Returns(formVersion);
        var worksheetRepository = Substitute.For<IWorksheetRepository>();
        worksheetRepository.GetByNameAsync(Arg.Any<string>(), true).Returns(worksheet);

        var service = CreateService(
            Substitute.For<IRepository<ApplicationFormVersion, Guid>>(),
            Substitute.For<IAIGenerationAppService>(),
            formVersionRepository,
            worksheetRepository);
        service.LazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();

        var result = await service.GetPendingAiWorksheetAsync(formVersionId);

        result.ShouldNotBeNull();
        result!.SessionId.ShouldBe(worksheet.Id);
        result.Fields.Single().Label.ShouldBe("Project Name");
        result.Fields.Single().Selected.ShouldBeTrue();
    }

    [Fact]
    public async Task DiscardAiWorksheetSuggestionsAsync_Should_Not_Delete_Published_Containers()
    {
        var formVersionId = Guid.NewGuid();
        var formId = Guid.NewGuid();
        var formVersion = new ApplicationFormVersion { ApplicationFormId = formId };
        var publishedSuggestion = BuildAiWorksheet(formId, formVersionId, published: true);
        var formVersionRepository = Substitute.For<IApplicationFormVersionRepository>();
        formVersionRepository.GetAsync(formVersionId).Returns(formVersion);
        var worksheetRepository = Substitute.For<IWorksheetRepository>();
        worksheetRepository.GetByNameAsync(Arg.Any<string>(), true).Returns(publishedSuggestion);

        var service = CreateService(
            Substitute.For<IRepository<ApplicationFormVersion, Guid>>(),
            Substitute.For<IAIGenerationAppService>(),
            formVersionRepository,
            worksheetRepository);
        service.LazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();

        await service.DiscardAiWorksheetSuggestionsAsync(formVersionId);

        await worksheetRepository.DidNotReceive().DeleteAsync(Arg.Any<Worksheet>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task CreateAiWorksheetDraftAsync_Should_Create_Unlinked_Unpublished_Draft_And_Keep_Remaining_Suggestions()
    {
        var formVersionId = Guid.NewGuid();
        var formId = Guid.NewGuid();
        var formVersion = new ApplicationFormVersion
        {
            ApplicationFormId = formId
        };
        var worksheet = BuildAiWorksheet(formId, formVersionId, published: false, fieldCount: 2);
        var formVersionRepository = Substitute.For<IApplicationFormVersionRepository>();
        formVersionRepository.GetAsync(formVersionId).Returns(formVersion);
        var worksheetRepository = Substitute.For<IWorksheetRepository>();
        worksheetRepository.GetByNameAsync(Arg.Any<string>(), true).Returns(worksheet);
        var customFieldRepository = Substitute.For<IRepository<CustomField, Guid>>();
        Worksheet? createdDraft = null;
        worksheetRepository.InsertAsync(Arg.Do<Worksheet>(worksheet => createdDraft = worksheet), true)
            .Returns(Task.FromResult<Worksheet>(null!));

        var service = CreateService(
            Substitute.For<IRepository<ApplicationFormVersion, Guid>>(),
            Substitute.For<IAIGenerationAppService>(),
            formVersionRepository,
            worksheetRepository,
            customFieldRepository);
        service.LazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();

        var selectedFieldId = worksheet.Sections.Single().Fields.First().Id;
        var remainingFieldId = worksheet.Sections.Single().Fields.Last().Id;
        worksheet.Sections.Single().Fields.First().SetDefinition(JsonSerializer.Serialize("""{"required":true}"""));
        await service.CreateAiWorksheetDraftAsync(formVersionId, new CreateAiWorksheetDraftDto
        {
            SessionId = worksheet.Id,
            Title = "Risk Review",
            SelectedFieldIds = [selectedFieldId]
        });

        createdDraft.ShouldNotBeNull();
        createdDraft!.Name.ShouldBe("ai-risk-review");
        createdDraft.Title.ShouldBe("Risk Review");
        createdDraft.Published.ShouldBeFalse();
        createdDraft.Links.ShouldBeEmpty();
        createdDraft.Sections.Single().Fields.Single().Key.ShouldBe("Field0");
        createdDraft.Sections.Single().Fields.Single().Label.ShouldBe("Project Name");
        createdDraft.Sections.Single().Fields.Single().Definition.ShouldBe("""{"required":true}""");
        worksheet.Published.ShouldBeFalse();
        worksheet.Sections.Single().Fields.Select(field => field.Id).ShouldBe([remainingFieldId]);
        await customFieldRepository.Received(1).DeleteAsync(selectedFieldId);
        await worksheetRepository.Received(1).UpdateAsync(worksheet, true);
    }

    [Fact]
    public async Task CreateAiWorksheetDraftAsync_Should_Number_Internal_Name_And_Delete_Empty_Suggestions()
    {
        var formVersionId = Guid.NewGuid();
        var formId = Guid.NewGuid();
        var formVersion = new ApplicationFormVersion
        {
            ApplicationFormId = formId
        };
        var worksheet = BuildAiWorksheet(formId, formVersionId, published: false, fieldCount: 2);
        var formVersionRepository = Substitute.For<IApplicationFormVersionRepository>();
        formVersionRepository.GetAsync(formVersionId).Returns(formVersion);
        var worksheetRepository = Substitute.For<IWorksheetRepository>();
        worksheetRepository.GetByNameAsync(Arg.Any<string>(), true).Returns(worksheet);
        var customFieldRepository = Substitute.For<IRepository<CustomField, Guid>>();
        Worksheet? createdDraft = null;
        worksheetRepository.GetByNameAsync("ai-risk-review", false).Returns(new Worksheet(Guid.NewGuid(), "ai-risk-review", "Existing"));
        worksheetRepository.GetByNameAsync("ai-risk-review-2", false).Returns((Worksheet?)null);
        worksheetRepository.InsertAsync(Arg.Do<Worksheet>(worksheet => createdDraft = worksheet), true)
            .Returns(Task.FromResult<Worksheet>(null!));

        var service = CreateService(
            Substitute.For<IRepository<ApplicationFormVersion, Guid>>(),
            Substitute.For<IAIGenerationAppService>(),
            formVersionRepository,
            worksheetRepository,
            customFieldRepository);
        service.LazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();

        await service.CreateAiWorksheetDraftAsync(formVersionId, new CreateAiWorksheetDraftDto
        {
            SessionId = worksheet.Id,
            Title = "Risk Review",
            SelectedFieldIds = worksheet.Sections.Single().Fields.Select(field => field.Id).ToList()
        });

        createdDraft!.Name.ShouldBe("ai-risk-review-2");
        createdDraft.Published.ShouldBeFalse();
        await customFieldRepository.Received(2).DeleteAsync(Arg.Any<Guid>());
        await worksheetRepository.Received(1).DeleteAsync(worksheet, true);
    }

    private static Worksheet BuildAiWorksheet(Guid formId, Guid formVersionId, bool published, int fieldCount = 1)
    {
        var worksheet = new Worksheet(
            Guid.NewGuid(),
            AiWorksheetSuggestionName.Build(formId, formVersionId),
            "AI Worksheet");
        worksheet.SetPublished(published);

        var section = new WorksheetSection(Guid.NewGuid(), "Suggested Fields")
        {
            Worksheet = worksheet
        };
        worksheet.AddSection(section);

        for (var index = 0; index < fieldCount; index++)
        {
            var field = new CustomField(
                Guid.NewGuid(),
                $"Field{index}",
                worksheet.Name,
                index == 0 ? "Project Name" : $"Field {index}",
                CustomFieldType.Text,
                "{}");
            field.Section = section;
            section.Fields.Add(field);
        }

        return worksheet;
    }

    [Fact]
    public void MappingReadService_Should_Use_Canonical_CustomField_Name_For_Worksheet_Field_Name()
    {
        var worksheet = new Worksheet(Guid.NewGuid(), "customfields-v1", "Custom Fields");
        var section = new WorksheetSection(Guid.NewGuid(), "section");
        section.Worksheet = worksheet;
        section.Fields.Add(new CustomField(
            Guid.NewGuid(),
            "CustomField2",
            worksheet.Name,
            "Custom Field 2",
            CustomFieldType.Text,
            "{\"required\": false, \"maxLength\": 4294967295, \"minLength\": 0}"));
        worksheet.AddSection(section);

        var method = typeof(ApplicationFormVersionMappingReadService)
            .GetMethod("MapWorksheet", BindingFlags.NonPublic | BindingFlags.Static);

        var result = (WorksheetMappingFieldsDto)method!.Invoke(null, [worksheet])!;

        var field = result.Fields.Single();
        field.Name.ShouldBe("custom_customfields-v1_customfield2.Text");
    }

    [Fact]
    public void MappingReadService_Should_Exclude_System_Fields_From_Chefs_Fields()
    {
        const string availableChefsFields = """
        {
          "ProjectName": { "type": "textfield", "label": "Project Name" },
          "SubmissionId": { "type": "textfield", "label": "Submission ID" },
          "SubmissionDate": { "type": "datetime", "label": "Submission Date" },
          "ConfirmationId": { "type": "textfield", "label": "Confirmation ID" }
        }
        """;

        var method = typeof(ApplicationFormVersionMappingReadService)
            .GetMethod("BuildChefsFields", BindingFlags.NonPublic | BindingFlags.Static);

        var result = (List<MappingFieldDto>)method!.Invoke(null, [availableChefsFields])!;

        result.Select(field => field.Name).ShouldBe(["ProjectName"]);
    }

    [Fact]
    public void MappingReadService_Should_Exclude_System_Fields_From_Unity_Core_Fields()
    {
        var method = typeof(ApplicationFormVersionMappingReadService)
            .GetMethod("BuildUnityCoreFields", BindingFlags.NonPublic | BindingFlags.Static);

        var result = (List<MappingFieldDto>)method!.Invoke(null, [])!;

        result.Select(field => field.Name).ShouldNotContain("ConfirmationId");
        result.Select(field => field.Name).ShouldNotContain("SubmissionDate");
        result.Select(field => field.Name).ShouldNotContain("SubmissionId");
        result.Select(field => field.Name).ShouldContain("ProjectName");
    }

    private static ApplicationFormVersionAppService CreateService(
        IRepository<ApplicationFormVersion, Guid> repository,
        IAIGenerationAppService aiGenerationAppService,
        IApplicationFormVersionRepository? formVersionRepository = null,
        IWorksheetRepository? worksheetRepository = null,
        IRepository<CustomField, Guid>? customFieldRepository = null)
    {
        var featureChecker = Substitute.For<IFeatureChecker>();
        var service = new ApplicationFormVersionAppService(
            repository,
            Substitute.For<IIntakeFormSubmissionMapper>(),
            Substitute.For<IUnitOfWorkManager>(),
            Substitute.For<IFormsApiService>(),
            formVersionRepository ?? Substitute.For<IApplicationFormVersionRepository>(),
            Substitute.For<IApplicationFormSubmissionRepository>(),
            Substitute.For<IReportingFieldsGeneratorService>(),
            featureChecker,
            aiGenerationAppService,
            worksheetRepository ?? Substitute.For<IWorksheetRepository>(),
            customFieldRepository ?? Substitute.For<IRepository<CustomField, Guid>>(),
            Substitute.For<IGenerationReviewRepository>(),
            Substitute.For<Unity.Flex.Domain.WorksheetLinks.IWorksheetLinkRepository>());
        return service;
    }
}
