using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.AI;
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
    public async Task RegenerateAndSaveAsync_Builds_Filtered_Prompt_Payload_From_Form_Submission()
    {
        var applicationId = Guid.NewGuid();
        var formVersionId = Guid.NewGuid();
        ApplicationAnalysisRequest? capturedRequest = null;
        var application = WithId(new Application
        {
            ApplicationFormId = Guid.NewGuid(),
            ProjectName = "Fallback project",
            ReferenceNo = "REF-001",
            RequestedAmount = 100,
            TotalProjectBudget = 200,
            SubmissionDate = new DateTime(2026, 1, 2)
        }, applicationId);
        var submission = new ApplicationFormSubmission
        {
            ApplicationId = applicationId,
            ApplicationFormVersionId = formVersionId,
            Submission = """
            {
              "data": {
                "projectName": "Submitted project",
                "requestedAmount": 12345,
                "metadata": { "timezone": "utc" },
                "files": [{ "name": "large.pdf" }],
                "ignoredField": "not in schema"
              }
            }
            """
        };
        var formVersion = new ApplicationFormVersion
        {
            FormSchema = """
            {
              "components": [
                { "key": "projectName", "label": "Project Name", "type": "textfield", "input": true, "validate": { "required": true } },
                { "key": "requestedAmount", "label": "Requested Amount", "type": "number", "input": true },
                { "key": "applicantAgent", "label": "Applicant Agent", "type": "textfield", "input": true },
                { "key": "ignoredField", "label": "Ignored Field", "type": "html", "input": false }
              ]
            }
            """
        };

        var applicationRepository = Substitute.For<IApplicationRepository>();
        applicationRepository.GetAsync(applicationId).Returns(application);

        var submissionRepository = Substitute.For<IApplicationFormSubmissionRepository>();
        submissionRepository.GetByApplicationAsync(applicationId).Returns(submission);

        var formVersionRepository = Substitute.For<IApplicationFormVersionRepository>();
        formVersionRepository.GetAsync(formVersionId).Returns(formVersion);

        var attachmentRepository = Substitute.For<IApplicationChefsFileAttachmentRepository>();
        attachmentRepository
            .GetListAsync(Arg.Any<Expression<Func<ApplicationChefsFileAttachment, bool>>>())
            .Returns([
                new ApplicationChefsFileAttachment
                {
                    FileName = " summary.pdf ",
                    AISummary = " Summary text "
                },
                new ApplicationChefsFileAttachment
                {
                    FileName = "empty.txt",
                    AISummary = " "
                }
            ]);

        var aiService = Substitute.For<IAIService>();
        aiService.GenerateApplicationAnalysisAsync(Arg.Do<ApplicationAnalysisRequest>(request => capturedRequest = request))
            .Returns(new ApplicationAnalysisResponse { Decision = "ok" });
        var prerequisiteValidator = Substitute.For<IAIGenerationPrerequisiteValidator>();

        var service = new ApplicationAnalysisService(
            applicationRepository,
            submissionRepository,
            formVersionRepository,
            attachmentRepository,
            aiService,
            prerequisiteValidator,
            NullLogger<ApplicationAnalysisService>.Instance);

        var result = await service.RegenerateAndSaveAsync(applicationId, "v1");

        result.ShouldContain("\"Decision\": \"ok\"");
        capturedRequest.ShouldNotBeNull();
        capturedRequest.PromptVersion.ShouldBe("v1");
        capturedRequest.Attachments.Count.ShouldBe(1);
        capturedRequest.Attachments[0].Name.ShouldBe("summary.pdf");
        capturedRequest.Attachments[0].Summary.ShouldBe("Summary text");

        capturedRequest.Data.GetProperty("projectName").GetString().ShouldBe("Submitted project");
        capturedRequest.Data.GetProperty("requestedAmount").GetInt32().ShouldBe(12345);
        capturedRequest.Data.TryGetProperty("metadata", out _).ShouldBeFalse();
        capturedRequest.Data.TryGetProperty("files", out _).ShouldBeFalse();
        capturedRequest.Data.TryGetProperty("ignoredField", out _).ShouldBeFalse();

        var schemaJson = JsonSerializer.Serialize(capturedRequest.Schema);
        schemaJson.ShouldContain("Project Name (projectName)");
        schemaJson.ShouldContain("Requested Amount (requestedAmount)");
        schemaJson.ShouldNotContain("Applicant Agent (applicantAgent)");
        await applicationRepository.Received(1).UpdateAsync(application);
    }

    [Fact]
    public async Task RegenerateAndSaveAsync_Falls_Back_To_Application_Data_When_Submission_Is_Missing()
    {
        var applicationId = Guid.NewGuid();
        var application = WithId(new Application
        {
            ProjectName = "Fallback project",
            ReferenceNo = "REF-002",
            RequestedAmount = 1500,
            TotalProjectBudget = 3000,
            ProjectSummary = "Fallback summary",
            SubmissionDate = new DateTime(2026, 2, 3)
        }, applicationId);
        ApplicationAnalysisRequest? capturedRequest = null;

        var applicationRepository = Substitute.For<IApplicationRepository>();
        applicationRepository.GetAsync(applicationId).Returns(application);

        var submissionRepository = Substitute.For<IApplicationFormSubmissionRepository>();
        submissionRepository.GetByApplicationAsync(applicationId).Returns((ApplicationFormSubmission?)null);

        var formVersionRepository = Substitute.For<IApplicationFormVersionRepository>();
        var attachmentRepository = Substitute.For<IApplicationChefsFileAttachmentRepository>();
        attachmentRepository
            .GetListAsync(Arg.Any<Expression<Func<ApplicationChefsFileAttachment, bool>>>())
            .Returns([]);

        var aiService = Substitute.For<IAIService>();
        aiService.GenerateApplicationAnalysisAsync(Arg.Do<ApplicationAnalysisRequest>(request => capturedRequest = request))
            .Returns(new ApplicationAnalysisResponse());
        var prerequisiteValidator = Substitute.For<IAIGenerationPrerequisiteValidator>();

        var service = new ApplicationAnalysisService(
            applicationRepository,
            submissionRepository,
            formVersionRepository,
            attachmentRepository,
            aiService,
            prerequisiteValidator,
            NullLogger<ApplicationAnalysisService>.Instance);

        await service.RegenerateAndSaveAsync(applicationId);

        capturedRequest.ShouldNotBeNull();
        capturedRequest.Data.GetProperty("project_name").GetString().ShouldBe("Fallback project");
        capturedRequest.Data.GetProperty("reference_number").GetString().ShouldBe("REF-002");
        capturedRequest.Data.GetProperty("requested_amount").GetDecimal().ShouldBe(1500);
        capturedRequest.Attachments.ShouldBeEmpty();
    }

    private static T WithId<T>(T entity, Guid id) where T : Entity<Guid>
    {
        typeof(Entity<Guid>)
            .GetProperty(nameof(Entity<Guid>.Id))!
            .SetValue(entity, id);
        return entity;
    }
}
