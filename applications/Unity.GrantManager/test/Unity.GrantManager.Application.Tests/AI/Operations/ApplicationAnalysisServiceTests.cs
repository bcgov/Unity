using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Models;
using Unity.AI.Operations;
using Unity.AI.Requests;
using Unity.AI.Responses;
using Unity.GrantManager.Applications;
using Volo.Abp.Domain.Entities;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.AI.Operations;

public class ApplicationAnalysisServiceTests : GrantManagerApplicationTestBase
{
    public ApplicationAnalysisServiceTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
    }

    [Fact]
    public async Task RegenerateAndSaveAsync_Uses_Input_Dto_And_Persists_Analysis()
    {
        var applicationId = Guid.NewGuid();
        var application = WithId(new Application(), applicationId);
        ApplicationAnalysisRequest? capturedRequest = null;

        var applicationRepository = Substitute.For<IApplicationRepository>();
        applicationRepository.GetAsync(applicationId).Returns(application);

        var aiService = Substitute.For<IAIService>();
        aiService.GenerateApplicationAnalysisAsync(Arg.Do<ApplicationAnalysisRequest>(request => capturedRequest = request))
            .Returns(new ApplicationAnalysisResponse { Decision = "ok" });

        var prerequisiteValidator = Substitute.For<IAIGenerationPrerequisiteValidator>();

        var service = new ApplicationAnalysisService(
            applicationRepository,
            aiService,
            prerequisiteValidator);

        var result = await service.RegenerateAndSaveAsync(new ApplicationAnalysisOperationInputDto
        {
            ApplicationId = applicationId,
            Schema = JsonSerializer.SerializeToElement(new { projectName = "Project Name" }),
            Data = JsonSerializer.SerializeToElement(new { projectName = "Submitted project" }),
            Attachments = new List<AIAttachmentItem>
            {
                new()
                {
                    Name = "summary.pdf",
                    Summary = "Summary text"
                }
            },
            PromptVersion = "v1"
        });

        result.ShouldContain("\"Decision\": \"ok\"");
        capturedRequest.ShouldNotBeNull();
        capturedRequest.PromptVersion.ShouldBe("v1");
        capturedRequest.Attachments.Count.ShouldBe(1);
        capturedRequest.Attachments[0].Name.ShouldBe("summary.pdf");
        capturedRequest.Attachments[0].Summary.ShouldBe("Summary text");
        capturedRequest.Data.GetProperty("projectName").GetString().ShouldBe("Submitted project");
        capturedRequest.Schema.GetProperty("projectName").GetString().ShouldBe("Project Name");

        await prerequisiteValidator.Received(1).EnsureApplicationAnalysisAvailableAsync(applicationId);
        await applicationRepository.Received(1).UpdateAsync(application);
    }

    [Fact]
    public async Task RegenerateAndSaveAsync_Flows_Without_Repository_Loading_Inputs()
    {
        var applicationId = Guid.NewGuid();
        var application = WithId(new Application(), applicationId);
        ApplicationAnalysisRequest? capturedRequest = null;

        var applicationRepository = Substitute.For<IApplicationRepository>();
        applicationRepository.GetAsync(applicationId).Returns(application);

        var aiService = Substitute.For<IAIService>();
        aiService.GenerateApplicationAnalysisAsync(Arg.Do<ApplicationAnalysisRequest>(request => capturedRequest = request))
            .Returns(new ApplicationAnalysisResponse());

        var prerequisiteValidator = Substitute.For<IAIGenerationPrerequisiteValidator>();

        var service = new ApplicationAnalysisService(
            applicationRepository,
            aiService,
            prerequisiteValidator);

        await service.RegenerateAndSaveAsync(new ApplicationAnalysisOperationInputDto
        {
            ApplicationId = applicationId,
            Schema = JsonSerializer.SerializeToElement(new { }),
            Data = JsonSerializer.SerializeToElement(new { project_name = "Fallback project" }),
            Attachments = [],
            PromptVersion = null
        });

        capturedRequest.ShouldNotBeNull();
        capturedRequest.Data.GetProperty("project_name").GetString().ShouldBe("Fallback project");
        capturedRequest.Attachments.ShouldBeEmpty();
        capturedRequest.PromptVersion.ShouldBeNull();
    }

    private static T WithId<T>(T entity, Guid id) where T : Entity<Guid>
    {
        typeof(Entity<Guid>)
            .GetProperty(nameof(Entity<Guid>.Id))!
            .SetValue(entity, id);
        return entity;
    }
}
