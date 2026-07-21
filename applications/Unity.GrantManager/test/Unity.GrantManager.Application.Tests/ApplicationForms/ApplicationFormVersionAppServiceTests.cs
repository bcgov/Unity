using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Cooldown;
using Unity.AI.Features;
using Unity.AI.Operations;
using Unity.AI.Requests;
using Unity.AI.Responses;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Domain.WorksheetLinks;
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
    public async Task GenerateMappingAsync_Should_Save_SubmissionHeaderMapping_From_Ai_Response()
    {
        var formVersionId = Guid.NewGuid();
        FormMappingRequest? capturedRequest = null;
        var repository = Substitute.For<IRepository<ApplicationFormVersion, Guid>>();
        var formVersion = new ApplicationFormVersion
        {
            ApplicationFormId = Guid.NewGuid(),
            SubmissionHeaderMapping = "{}"
        };
        repository.GetAsync(formVersionId).Returns(formVersion);
        repository.UpdateAsync(formVersion, true).Returns(formVersion);

        var readService = Substitute.For<IApplicationFormVersionMappingReadService>();
        readService.GetAsync(formVersionId).Returns(new ApplicationFormMappingReadModelDto
        {
            ApplicationFormVersionId = formVersionId,
            ApplicationFormId = formVersion.ApplicationFormId,
            ChefsApplicationFormGuid = "chefs-form",
            ChefsFormVersionGuid = "chefs-version",
            ExistingMapping = "{\"ProjectName\":\"projectName\"}",
            ChefsFields = new List<MappingFieldDto>
            {
                new() { Name = "ProjectName", Label = "Project Name", Type = "Text", IsCustom = false }
            },
            UnityCoreFields = new List<MappingFieldDto>
            {
                new() { Name = "ProjectName", Label = "Project Name", Type = "String", IsCustom = false }
            }
        });

        var aiService = Substitute.For<IFormMappingService>();
        aiService.GenerateFormMappingAsync(Arg.Do<FormMappingRequest>(request => capturedRequest = request), Arg.Any<System.Threading.CancellationToken>())
            .Returns(new FormMappingResponse
            {
                Mapping = """{"ProjectName":"projectName"}"""
        });

        var service = CreateService(repository, readService, aiService);
        service.LazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();

        var result = await service.GenerateMappingAsync(formVersionId);

        result.ApplicationFormVersionId.ShouldBe(formVersionId);
        capturedRequest.ShouldNotBeNull();
        capturedRequest!.Data.GetProperty("chefsData").GetProperty("fields").ValueKind.ShouldBe(System.Text.Json.JsonValueKind.Array);
        capturedRequest.Data.GetProperty("unityData").GetProperty("coreFields").ValueKind.ShouldBe(System.Text.Json.JsonValueKind.Array);
        capturedRequest.Data.GetProperty("unityData").GetProperty("customFields").ValueKind.ShouldBe(System.Text.Json.JsonValueKind.Array);
        capturedRequest.Data.GetProperty("existingMapping").GetProperty("ProjectName").GetString().ShouldBe("projectName");
        formVersion.SubmissionHeaderMapping.ShouldBe("""{"ProjectName":"projectName"}""");
        await repository.Received(1).UpdateAsync(formVersion, true);
    }

    [Fact]
    public void FormMappingPromptData_Should_UseEmptyObject_When_NoExistingMappingIsAvailable()
    {
        var promptData = FormMappingPromptDataBuilder.Build(new ApplicationFormMappingReadModelDto());

        promptData.GetProperty("existingMapping").GetRawText().ShouldBe("{}");
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
            Substitute.For<IApplicationFormVersionMappingReadService>(),
            Substitute.For<IFormMappingService>(),
            formVersionRepository,
            worksheetRepository);
        service.LazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();

        var result = await service.GetPendingAiWorksheetAsync(formVersionId);

        result.ShouldNotBeNull();
        result!.WorksheetId.ShouldBe(worksheet.Id);
        result.WorksheetTitle.ShouldBe("AI Worksheet");
        result.Sections.Single().Fields.Single().Label.ShouldBe("Project Name");
        result.Sections.Single().Fields.Single().Selected.ShouldBeTrue();
    }

    [Fact]
    public async Task ConfirmAiWorksheetAsync_Should_Delete_Unselected_Fields_Publish_And_Link_Worksheet()
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
        var worksheetLinkRepository = Substitute.For<IWorksheetLinkRepository>();

        var service = CreateService(
            Substitute.For<IRepository<ApplicationFormVersion, Guid>>(),
            Substitute.For<IApplicationFormVersionMappingReadService>(),
            Substitute.For<IFormMappingService>(),
            formVersionRepository,
            worksheetRepository,
            customFieldRepository,
            worksheetLinkRepository);
        service.LazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();

        var selectedFieldId = worksheet.Sections.Single().Fields.First().Id;
        await service.ConfirmAiWorksheetAsync(formVersionId, new ConfirmAiWorksheetDto
        {
            WorksheetId = worksheet.Id,
            SelectedFieldIds = [selectedFieldId]
        });

        worksheet.Published.ShouldBeTrue();
        worksheet.Sections.Single().Fields.Select(field => field.Id).ShouldBe([selectedFieldId]);
        await customFieldRepository.Received(1).DeleteAsync(Arg.Is<Guid>(id => id != selectedFieldId));
        await worksheetRepository.Received(1).UpdateAsync(worksheet, true);
        await worksheetLinkRepository.Received(1).InsertAsync(Arg.Any<WorksheetLink>(), true);
    }

    [Fact]
    public async Task ConfirmAiWorksheetAsync_Should_Allow_All_Suggested_Fields_To_Be_Deselected()
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
        var worksheetLinkRepository = Substitute.For<IWorksheetLinkRepository>();

        var service = CreateService(
            Substitute.For<IRepository<ApplicationFormVersion, Guid>>(),
            Substitute.For<IApplicationFormVersionMappingReadService>(),
            Substitute.For<IFormMappingService>(),
            formVersionRepository,
            worksheetRepository,
            customFieldRepository,
            worksheetLinkRepository);
        service.LazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();

        await service.ConfirmAiWorksheetAsync(formVersionId, new ConfirmAiWorksheetDto
        {
            WorksheetId = worksheet.Id,
            SelectedFieldIds = []
        });

        worksheet.Published.ShouldBeTrue();
        worksheet.Sections.Single().Fields.ShouldBeEmpty();
        await customFieldRepository.Received(2).DeleteAsync(Arg.Any<Guid>());
        await worksheetLinkRepository.Received(1).InsertAsync(Arg.Any<WorksheetLink>(), true);
    }

    private static Worksheet BuildAiWorksheet(Guid formId, Guid formVersionId, bool published, int fieldCount = 1)
    {
        var worksheet = new Worksheet(
            Guid.NewGuid(),
            $"ai-form-{formId}-version-{formVersionId}-worksheet",
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
        IApplicationFormVersionMappingReadService mappingReadService,
        IFormMappingService aiService,
        IApplicationFormVersionRepository? formVersionRepository = null,
        IWorksheetRepository? worksheetRepository = null,
        IRepository<CustomField, Guid>? customFieldRepository = null,
        IWorksheetLinkRepository? worksheetLinkRepository = null)
    {
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync(AIFeatures.FormMapping).Returns(true);

        var cooldownService = Substitute.For<IAICooldownService>();
        cooldownService.EnsureAsync(Arg.Any<Guid?>())
            .Returns(Task.CompletedTask);

        var service = new ApplicationFormVersionAppService(
            repository,
            Substitute.For<IIntakeFormSubmissionMapper>(),
            Substitute.For<IUnitOfWorkManager>(),
            Substitute.For<IFormsApiService>(),
            formVersionRepository ?? Substitute.For<IApplicationFormVersionRepository>(),
            Substitute.For<IApplicationFormSubmissionRepository>(),
            Substitute.For<IReportingFieldsGeneratorService>(),
            featureChecker,
            mappingReadService,
            cooldownService,
            aiService,
            worksheetRepository ?? Substitute.For<IWorksheetRepository>(),
            customFieldRepository ?? Substitute.For<IRepository<CustomField, Guid>>(),
            worksheetLinkRepository ?? Substitute.For<IWorksheetLinkRepository>());
        return service;
    }
}
