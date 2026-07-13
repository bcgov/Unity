using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Operations;
using Unity.AI.Requests;
using Unity.AI.Responses;
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
                Mapping = """{"ProjectName":"ProjectName"}"""
            });

        var service = CreateService(repository, readService, aiService);

        var result = await service.GenerateMappingAsync(formVersionId);

        result.ApplicationFormVersionId.ShouldBe(formVersionId);
        capturedRequest.ShouldNotBeNull();
        capturedRequest!.Data.GetProperty("chefsData").GetProperty("fields").ValueKind.ShouldBe(System.Text.Json.JsonValueKind.Array);
        capturedRequest.Data.GetProperty("unityData").GetProperty("coreFields").ValueKind.ShouldBe(System.Text.Json.JsonValueKind.Array);
        capturedRequest.Data.GetProperty("unityData").GetProperty("customFields").ValueKind.ShouldBe(System.Text.Json.JsonValueKind.Array);
        formVersion.SubmissionHeaderMapping.ShouldBe("""{"ProjectName":"ProjectName"}""");
        await repository.Received(1).UpdateAsync(formVersion, true);
    }

    private static ApplicationFormVersionAppService CreateService(
        IRepository<ApplicationFormVersion, Guid> repository,
        IApplicationFormVersionMappingReadService mappingReadService,
        IFormMappingService aiService)
    {
        var service = new ApplicationFormVersionAppService(
            repository,
            Substitute.For<IIntakeFormSubmissionMapper>(),
            Substitute.For<IUnitOfWorkManager>(),
            Substitute.For<IFormsApiService>(),
            Substitute.For<IApplicationFormVersionRepository>(),
            Substitute.For<IApplicationFormSubmissionRepository>(),
            Substitute.For<IReportingFieldsGeneratorService>(),
            Substitute.For<IFeatureChecker>(),
            mappingReadService,
            aiService);
        return service;
    }
}
