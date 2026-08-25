using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.AI.Models;
using Unity.AI.Operations;
using Unity.Flex.Scoresheets.Enums;
using Unity.GrantManager.Applications;
using Xunit;

namespace Unity.GrantManager.AI.Operations;

public class AIApplicationInputBuilderTests
{
    [Fact]
    public async Task BuildApplicationAnalysisInputAsync_Uses_Shared_Prompt_Mapping_And_Attachments()
    {
        var applicationId = Guid.NewGuid();
        var formVersionId = Guid.NewGuid();
        var builder = CreateBuilder(out var dataProvider);

        dataProvider.GetApplicationSubmissionAsync(applicationId).Returns(new ApplicationSubmissionSnapshot
        {
            ApplicationFormVersionId = formVersionId,
            Submission = JsonSerializer.Serialize(new
            {
                data = new
                {
                    project_name = "Submitted project",
                    ignored_field = "drop me"
                }
            })
        });
        dataProvider.GetApplicationFormVersionAsync(formVersionId).Returns(new ApplicationFormVersionSnapshot
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
        dataProvider.GetAttachmentSummariesAsync(applicationId).Returns(Task.FromResult(new List<AttachmentSummarySnapshot>
        {
            new("summary.pdf", "Summary text")
        }));

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
    public async Task BuildApplicationScoringInputAsync_Uses_Shared_Input_Data_And_Builds_Section_Schema()
    {
        var applicationId = Guid.NewGuid();
        var formVersionId = Guid.NewGuid();
        var scoresheetId = Guid.NewGuid();
        var builder = CreateBuilder(out var dataProvider);

        dataProvider.GetApplicationFormAsync(applicationId).Returns(new ApplicationFormSnapshot
        {
            ScoresheetId = scoresheetId
        });
        dataProvider.GetApplicationSubmissionAsync(applicationId).Returns(new ApplicationSubmissionSnapshot
        {
            ApplicationFormVersionId = formVersionId,
            Submission = JsonSerializer.Serialize(new { data = new { project_name = "Submitted project" } })
        });
        dataProvider.GetApplicationFormVersionAsync(formVersionId).Returns(new ApplicationFormVersionSnapshot
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
        dataProvider.GetAttachmentSummariesAsync(applicationId).Returns(Task.FromResult(new List<AttachmentSummarySnapshot>
        {
            new("summary.pdf", "Summary text")
        }));
        dataProvider.GetScoresheetAsync(scoresheetId).Returns(new ScoresheetSnapshot
        {
            Sections =
            [
                new ScoresheetSectionSnapshot
                {
                    Name = "Section A",
                    Order = 1,
                    Fields =
                    [
                        new ScoresheetFieldSnapshot
                        {
                            Id = Guid.NewGuid(),
                            Label = "Question 1",
                            Description = "Description",
                            Type = QuestionType.Text.ToString(),
                            Order = 1,
                            Definition = null
                        }
                    ]
                }
            ]
        });

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

    private static AIApplicationInputBuilder CreateBuilder(out IAIApplicationInputDataProvider applicationInputDataProvider)
    {
        applicationInputDataProvider = Substitute.For<IAIApplicationInputDataProvider>();

        return new AIApplicationInputBuilder(
            applicationInputDataProvider,
            NullLogger<AIApplicationInputBuilder>.Instance);
    }

    private static AIApplicationPromptDataDto CreatePromptData(Guid applicationId, Guid? applicationFormId = null)
    {
        return new AIApplicationPromptDataDto
        {
            ApplicationId = applicationId,
            ApplicationFormId = applicationFormId ?? Guid.NewGuid(),
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
