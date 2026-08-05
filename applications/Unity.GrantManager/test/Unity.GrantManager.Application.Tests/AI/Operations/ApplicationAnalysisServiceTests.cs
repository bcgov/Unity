using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Models;
using Unity.AI.Operations;
using Unity.AI.Requests;
using Unity.AI.Responses;
using Xunit;

namespace Unity.GrantManager.AI.Operations;

public class ApplicationAnalysisServiceTests
{
    [Fact]
    public async Task RegenerateAsync_Uses_Input_And_Serializes_Response()
    {
        var applicationId = Guid.NewGuid();
        ApplicationAnalysisRequest? capturedRequest = null;

        var aiService = Substitute.For<IAIService>();
        aiService.GenerateApplicationAnalysisAsync(Arg.Do<ApplicationAnalysisRequest>(request => capturedRequest = request), Arg.Any<CancellationToken>())
            .Returns(new ApplicationAnalysisResponse { Decision = "ok" });
        var prerequisiteValidator = Substitute.For<IAIGenerationPrerequisiteValidator>();

        var service = new ApplicationAnalysisService(aiService, prerequisiteValidator);

        var result = await service.RegenerateAsync(new ApplicationAnalysisOperationInputDto
        {
            ApplicationId = applicationId,
            Data = JsonSerializer.SerializeToElement(new { projectName = "Submitted project" }),
            Schema = JsonSerializer.SerializeToElement(new { required_fields = new[] { "Project Name (projectName)" } }),
            Attachments = new List<AIAttachmentItem>(),
            PromptVersion = "v1"
        });

        result.ShouldContain("\"Decision\": \"ok\"");
        capturedRequest.ShouldNotBeNull();
        capturedRequest.PromptVersion.ShouldBe("v1");
        capturedRequest.Data.GetProperty("projectName").GetString().ShouldBe("Submitted project");

        await prerequisiteValidator.Received(1).EnsureApplicationAnalysisAvailableAsync(applicationId);
        await aiService.Received(1).GenerateApplicationAnalysisAsync(Arg.Any<ApplicationAnalysisRequest>(), Arg.Any<CancellationToken>());
    }
}
