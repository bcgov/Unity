using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.AI.Operations;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Scoresheets.Enums;
using Unity.GrantManager.Applications;
using Xunit;

namespace Unity.GrantManager.AI.Operations;

public class AIApplicationInputBuilderTests
{
    [Fact]
    public async Task BuildApplicationAnalysisInputAsync_Uses_Shared_Prompt_Mapping_And_Excludes_Whitespace_Summaries()
    {
        var applicationId = Guid.NewGuid();
        var formVersionId = Guid.NewGuid();
        var builder = CreateBuilder(
            out _,
            out var applicationFormSubmissionRepository,
            out var applicationFormVersionRepository,
            out var applicationChefsFileAttachmentRepository,
            out _);

        var submission = new ApplicationFormSubmission
        {
            ApplicationId = applicationId,
            ApplicationFormVersionId = formVersionId,
            Submission = JsonSerializer.Serialize(new
            {
                data = new
                {
                    project_name = "Submitted project",
                    ignored_field = "drop me"
                }
            })
        };
        applicationFormSubmissionRepository.GetByApplicationAsync(applicationId).Returns(submission);
        applicationFormVersionRepository.GetAsync(formVersionId).Returns(new ApplicationFormVersion
        {
            FormSchema = JsonSerializer.Serialize(new
            {
                components = new[]
                {
                    new
                    {
                        key = "project_name",
                        label = "Project Name",
                        type = "textfield",
                        input = true,
                        validate = new { required = true }
                    }
                }
            })
        });
        applicationChefsFileAttachmentRepository.GetListAsync(Arg.Any<Expression<Func<ApplicationChefsFileAttachment, bool>>>())
            .Returns(
                [
                    new ApplicationChefsFileAttachment
                    {
                        ApplicationId = applicationId,
                        FileName = "summary.pdf",
                        AISummary = "Summary text"
                    },
                    new ApplicationChefsFileAttachment
                    {
                        ApplicationId = applicationId,
                        FileName = "ignored.pdf",
                        AISummary = "   "
                    }
                ]);

        var input = await builder.BuildApplicationAnalysisInputAsync(CreatePromptData(applicationId), "v1");

        input.ApplicationId.ShouldBe(applicationId);
        input.PromptVersion.ShouldBe("v1");
        input.Attachments.Count.ShouldBe(1);
        input.Attachments[0].Name.ShouldBe("summary.pdf");
        input.Attachments[0].Summary.ShouldBe("Summary text");
        input.Data.GetProperty("project_name").GetString().ShouldBe("Submitted project");
        input.Data.TryGetProperty("ignored_field", out _).ShouldBeFalse();
        input.Schema.GetProperty("required_fields")[0].GetString().ShouldBe("Project Name (project_name)");
    }

    [Fact]
    public async Task BuildApplicationScoringInputAsync_Uses_Shared_Attachment_Filtering_And_Builds_Section_Schema()
    {
        var applicationId = Guid.NewGuid();
        var formVersionId = Guid.NewGuid();
        var scoresheetId = Guid.NewGuid();
        var builder = CreateBuilder(
            out var applicationFormRepository,
            out var applicationFormSubmissionRepository,
            out var applicationFormVersionRepository,
            out var applicationChefsFileAttachmentRepository,
            out var scoresheetRepository);

        applicationFormRepository.GetAsync(Arg.Any<Guid>()).Returns(new ApplicationForm
        {
            ScoresheetId = scoresheetId
        });
        applicationFormSubmissionRepository.GetByApplicationAsync(applicationId).Returns(new ApplicationFormSubmission
        {
            ApplicationId = applicationId,
            ApplicationFormVersionId = formVersionId,
            Submission = JsonSerializer.Serialize(new
            {
                data = new
                {
                    project_name = "Submitted project"
                }
            })
        });
        applicationFormVersionRepository.GetAsync(formVersionId).Returns(new ApplicationFormVersion
        {
            FormSchema = JsonSerializer.Serialize(new
            {
                components = new[]
                {
                    new
                    {
                        key = "project_name",
                        label = "Project Name",
                        type = "textfield",
                        input = true,
                        validate = new { required = true }
                    }
                }
            })
        });
        applicationChefsFileAttachmentRepository.GetListAsync(Arg.Any<Expression<Func<ApplicationChefsFileAttachment, bool>>>())
            .Returns(
                [
                    new ApplicationChefsFileAttachment
                    {
                        ApplicationId = applicationId,
                        FileName = "summary.pdf",
                        AISummary = "Summary text"
                    },
                    new ApplicationChefsFileAttachment
                    {
                        ApplicationId = applicationId,
                        FileName = "ignored.pdf",
                        AISummary = "   "
                    }
                ]);

        var scoresheet = new Scoresheet(Guid.NewGuid(), "Scoresheet", "Scoresheet");
        var section = new ScoresheetSection(Guid.NewGuid(), "Section A", 1);
        section.Fields.Add(new Question(Guid.NewGuid(), "q1", "Question 1", QuestionType.Text, 1, "Description", null));
        scoresheet.Sections.Add(section);
        scoresheetRepository.GetWithChildrenAsync(scoresheetId).Returns(scoresheet);

        var input = await builder.BuildApplicationScoringInputAsync(CreatePromptData(applicationId), "v2");

        input.ApplicationId.ShouldBe(applicationId);
        input.PromptVersion.ShouldBe("v2");
        input.Attachments.Count.ShouldBe(1);
        input.Attachments[0].Name.ShouldBe("summary.pdf");
        input.Sections.Count.ShouldBe(1);
        input.Sections[0].SectionName.ShouldBe("Section A");
        input.Sections[0].SectionSchema.ValueKind.ShouldBe(JsonValueKind.Array);
        input.Sections[0].SectionSchema.GetArrayLength().ShouldBe(1);
        input.Data.GetProperty("project_name").GetString().ShouldBe("Submitted project");
    }

    private static AIApplicationInputBuilder CreateBuilder(
        out IApplicationFormRepository applicationFormRepository,
        out IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
        out IApplicationFormVersionRepository applicationFormVersionRepository,
        out IApplicationChefsFileAttachmentRepository applicationChefsFileAttachmentRepository,
        out IScoresheetRepository scoresheetRepository)
    {
        applicationFormRepository = Substitute.For<IApplicationFormRepository>();
        applicationFormSubmissionRepository = Substitute.For<IApplicationFormSubmissionRepository>();
        applicationFormVersionRepository = Substitute.For<IApplicationFormVersionRepository>();
        applicationChefsFileAttachmentRepository = Substitute.For<IApplicationChefsFileAttachmentRepository>();
        scoresheetRepository = Substitute.For<IScoresheetRepository>();

        return new AIApplicationInputBuilder(
            applicationFormRepository,
            applicationFormSubmissionRepository,
            applicationFormVersionRepository,
            applicationChefsFileAttachmentRepository,
            scoresheetRepository,
            NullLogger<AIApplicationInputBuilder>.Instance);
    }

    private static AIApplicationPromptDataDto CreatePromptData(Guid applicationId)
    {
        return new AIApplicationPromptDataDto
        {
            ApplicationId = applicationId,
            ApplicationFormId = Guid.NewGuid(),
            ProjectName = "Project Alpha",
            ReferenceNo = "REF-001",
            RequestedAmount = 15000m,
            TotalProjectBudget = 40000m,
            EconomicRegion = "Vancouver Island",
            City = "Victoria",
            SubmissionDate = new DateTime(2026, 6, 12),
            ProjectSummary = "Project summary",
            ProjectStartDate = new DateTime(2026, 7, 1),
            ProjectEndDate = new DateTime(2026, 12, 31),
            Community = "Community name"
        };
    }
}
