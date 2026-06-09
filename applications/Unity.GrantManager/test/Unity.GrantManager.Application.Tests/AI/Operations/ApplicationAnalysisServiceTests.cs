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
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.AI.Operations;

public class ApplicationAnalysisServiceTests : GrantManagerApplicationTestBase
{
    public ApplicationAnalysisServiceTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
    }

    [Fact]
    public async Task RegenerateAsync_Uses_Input_Dto_And_Returns_Serialized_Response()
    {
        var applicationId = Guid.NewGuid();
        ApplicationAnalysisRequest? capturedRequest = null;

        var aiService = Substitute.For<IAIService>();
        aiService.GenerateApplicationAnalysisAsync(Arg.Do<ApplicationAnalysisRequest>(request => capturedRequest = request))
            .Returns(new ApplicationAnalysisResponse { Decision = "ok" });
        var prerequisiteValidator = Substitute.For<IAIGenerationPrerequisiteValidator>();

        var service = new ApplicationAnalysisService(
            aiService,
            prerequisiteValidator);

        var result = await service.RegenerateAsync(new ApplicationAnalysisOperationInputDto
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
        await aiService.Received(1).GenerateApplicationAnalysisAsync(Arg.Any<ApplicationAnalysisRequest>(), Arg.Any<System.Threading.CancellationToken>());
    }

    [Fact]
    public async Task RegenerateAsync_Flows_Api_Request_To_The_Runtime_Without_Repository_Dependencies()
    {
        var applicationId = Guid.NewGuid();
        ApplicationAnalysisRequest? capturedRequest = null;

        var aiService = Substitute.For<IAIService>();
        aiService.GenerateApplicationAnalysisAsync(Arg.Do<ApplicationAnalysisRequest>(request => capturedRequest = request))
            .Returns(new ApplicationAnalysisResponse());
        var prerequisiteValidator = Substitute.For<IAIGenerationPrerequisiteValidator>();

        var service = new ApplicationAnalysisService(
            aiService,
            prerequisiteValidator);

        await service.RegenerateAsync(new ApplicationAnalysisOperationInputDto
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
}
