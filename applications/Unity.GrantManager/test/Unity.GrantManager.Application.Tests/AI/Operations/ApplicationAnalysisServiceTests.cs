using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.AI.Automation;
using Unity.AI;
using Unity.AI.Operations;
using Unity.AI.Models;
using Unity.AI.Validation;
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
        var prerequisiteValidator = Substitute.For<IGenerationPrerequisiteValidator>();
        var dataProvider = Substitute.For<IApplicationGenerationDataProvider>();
        dataProvider.GetApplicationSubmissionAsync(applicationId).Returns(new ApplicationSubmissionSnapshot
        {
            ApplicationFormVersionId = Guid.NewGuid(),
            Submission = JsonSerializer.Serialize(new
            {
                data = new
                {
                    projectName = "Submitted project"
                }
            })
        });
        dataProvider.GetAttachmentSummariesAsync(applicationId).Returns([]);
        dataProvider.GetApplicationFormVersionAsync(Arg.Any<Guid?>()).Returns(new ApplicationFormVersionSnapshot
        {
            FormSchema = JsonSerializer.Serialize(new
            {
                components = new[]
                {
                    new
                    {
                        key = "projectName",
                        label = "Project Name",
                        type = "textfield",
                        input = true,
                        validate = new { required = true }
                    }
                }
            })
        });

        var service = new ApplicationAnalysisService(
            aiService,
            prerequisiteValidator,
            dataProvider,
            NullLogger<ApplicationAnalysisService>.Instance);

        var result = await service.GenerateApplicationAnalysisAsync(applicationId, "v1");

        result.ShouldContain("\"Decision\": \"ok\"");
        capturedRequest.ShouldNotBeNull();
        capturedRequest.PromptVersion.ShouldBe("v1");
        capturedRequest.Attachments.ShouldBeEmpty();
        capturedRequest.Data.GetProperty("projectName").GetString().ShouldBe("Submitted project");
        capturedRequest.Schema.GetProperty("required_fields").EnumerateArray().First().GetString().ShouldBe("Project Name (projectName)");

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
        var prerequisiteValidator = Substitute.For<IGenerationPrerequisiteValidator>();
        var dataProvider = Substitute.For<IApplicationGenerationDataProvider>();
        dataProvider.GetApplicationSubmissionAsync(applicationId).Returns((ApplicationSubmissionSnapshot?)null);
        dataProvider.GetAttachmentSummariesAsync(applicationId).Returns([]);

        var service = new ApplicationAnalysisService(
            aiService,
            prerequisiteValidator,
            dataProvider,
            NullLogger<ApplicationAnalysisService>.Instance);

        await service.GenerateApplicationAnalysisAsync(applicationId);

        capturedRequest.ShouldNotBeNull();
        capturedRequest.Data.GetRawText().ShouldBe("{}");
        capturedRequest.Attachments.ShouldBeEmpty();
        capturedRequest.PromptVersion.ShouldBeNull();
    }
}
