using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Automation;
using Unity.AI;
using Unity.AI.Domain;
using Unity.AI.Execution;
using Unity.AI.Models;
using Unity.AI.Operations;
using Unity.AI.Validation;
using Unity.AI.Prompts;
using Unity.AI.Requests;
using Unity.AI.Responses;
using Unity.AI.Runtime;
using Unity.Flex.Scoresheets.Enums;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace Unity.GrantManager.AI.Operations;

public class ApplicationScoringServiceTests
{
    [Fact]
    public async Task RegenerateAsync_Sequential_Mode_Uses_Per_Section_Requests()
    {
        var capturedRequests = new List<ApplicationScoringRequest>();
        var aiService = Substitute.For<IAIService>();
        aiService.GenerateApplicationScoringAsync(Arg.Do<ApplicationScoringRequest>(request => capturedRequests.Add(request)))
            .Returns(new ApplicationScoringResponse
            {
                Answers = new Dictionary<string, ApplicationScoringAnswer>
                {
                    ["q1"] = new()
                    {
                        Answer = JsonSerializer.SerializeToElement("Yes"),
                        Rationale = "ok",
                        Confidence = 80
                    }
                }
            });
        var dataProvider = Substitute.For<IApplicationGenerationDataProvider>();
        dataProvider.GetApplicationFormAsync(Arg.Any<Guid>()).Returns(new ApplicationFormSnapshot
        {
            ScoresheetId = Guid.NewGuid()
        });
        dataProvider.GetScoresheetAsync(Arg.Any<Guid>()).Returns(new ScoresheetSnapshot
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
        dataProvider.GetApplicationSubmissionAsync(Arg.Any<Guid>()).Returns(new ApplicationSubmissionSnapshot
        {
            ApplicationFormVersionId = Guid.NewGuid(),
            Submission = JsonSerializer.Serialize(new { data = new { project_name = "Project Alpha" } })
        });
        dataProvider.GetAttachmentSummariesAsync(Arg.Any<Guid>()).Returns([]);
        dataProvider.GetApplicationFormVersionAsync(Arg.Any<Guid?>()).Returns(new ApplicationFormVersionSnapshot
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

        var operationRepository = CreateOperationRepository(ExecutionMode.Sequential);
        var service = CreateService(aiService, operationRepository, dataProvider);

        var applicationId = Guid.NewGuid();
        var result = await service.GenerateApplicationScoringAsync(applicationId);

        result.ShouldContain("\"q1\"");
        capturedRequests.Count.ShouldBe(1);
        capturedRequests[0].SectionName.ShouldBe("Section A");
        capturedRequests[0].Attachments.ShouldBeEmpty();
        capturedRequests[0].PromptVersion.ShouldBeNull();
    }

    [Fact]
    public async Task RegenerateAsync_Batch_Mode_Uses_Aggregated_Section_Schema()
    {
        var capturedRequests = new List<ApplicationScoringRequest>();
        var aiService = Substitute.For<IAIService>();
        aiService.GenerateApplicationScoringAsync(Arg.Do<ApplicationScoringRequest>(request => capturedRequests.Add(request)))
            .Returns(new ApplicationScoringResponse
            {
                Answers = new Dictionary<string, ApplicationScoringAnswer>
                {
                    ["q1"] = new()
                    {
                        Answer = JsonSerializer.SerializeToElement("No"),
                        Rationale = "batch",
                        Confidence = 70
                    }
                }
            });
        var dataProvider = Substitute.For<IApplicationGenerationDataProvider>();
        dataProvider.GetApplicationFormAsync(Arg.Any<Guid>()).Returns(new ApplicationFormSnapshot
        {
            ScoresheetId = Guid.NewGuid()
        });
        dataProvider.GetScoresheetAsync(Arg.Any<Guid>()).Returns(new ScoresheetSnapshot
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
                },
                new ScoresheetSectionSnapshot
                {
                    Name = "Section B",
                    Order = 2,
                    Fields =
                    [
                        new ScoresheetFieldSnapshot
                        {
                            Id = Guid.NewGuid(),
                            Label = "Question 2",
                            Description = "Description",
                            Type = QuestionType.Text.ToString(),
                            Order = 1,
                            Definition = null
                        }
                    ]
                }
            ]
        });
        dataProvider.GetApplicationSubmissionAsync(Arg.Any<Guid>()).Returns(new ApplicationSubmissionSnapshot
        {
            ApplicationFormVersionId = Guid.NewGuid(),
            Submission = JsonSerializer.Serialize(new { data = new { project_name = "Project Beta" } })
        });
        dataProvider.GetAttachmentSummariesAsync(Arg.Any<Guid>()).Returns([]);
        dataProvider.GetApplicationFormVersionAsync(Arg.Any<Guid?>()).Returns(new ApplicationFormVersionSnapshot
        {
            FormSchema = JsonSerializer.Serialize(new { components = Array.Empty<object>() })
        });

        var operationRepository = CreateOperationRepository(ExecutionMode.Batch);
        var service = CreateService(aiService, operationRepository, dataProvider);

        var result = await service.GenerateApplicationScoringAsync(Guid.NewGuid());

        result.ShouldContain("\"q1\"");
        capturedRequests.Count.ShouldBe(1);
        capturedRequests[0].SectionName.ShouldBe("All Sections");
        capturedRequests[0].SectionSchema.ValueKind.ShouldBe(JsonValueKind.Array);
        capturedRequests[0].SectionSchema.GetArrayLength().ShouldBe(2);
    }

    [Fact]
    public async Task RegenerateAsync_Batch_Mode_Rejects_Missing_Scoresheet()
    {
        var aiService = Substitute.For<IAIService>();
        var dataProvider = Substitute.For<IApplicationGenerationDataProvider>();
        dataProvider.GetApplicationFormAsync(Arg.Any<Guid>()).Returns(new ApplicationFormSnapshot
        {
            ScoresheetId = null
        });
        var operationRepository = CreateOperationRepository(ExecutionMode.Batch);
        var service = CreateService(aiService, operationRepository, dataProvider);

        await Should.ThrowAsync<UserFriendlyException>(() => service.GenerateApplicationScoringAsync(Guid.NewGuid()));
    }

    private static ApplicationScoringService CreateService(
        IAIService aiService,
        IRepository<AIOperation, Guid> operationRepository,
        IApplicationGenerationDataProvider dataProvider)
    {
        return new ApplicationScoringService(
            aiService,
            operationRepository,
            dataProvider,
            NullLogger<ApplicationScoringService>.Instance);
    }

    private static IRepository<AIOperation, Guid> CreateOperationRepository(ExecutionMode executionMode)
    {
        var operationRepository = Substitute.For<IRepository<AIOperation, Guid>>();
        operationRepository.GetListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<AIOperation, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var filter = callInfo.ArgAt<System.Linq.Expressions.Expression<Func<AIOperation, bool>>>(0).Compile();
                var operations = new[]
                {
                    new AIOperation(Guid.NewGuid(), AIPromptTypes.ApplicationScoring, Guid.NewGuid(), Guid.NewGuid())
                    {
                        ExecutionMode = executionMode,
                        IsActive = true
                    }
                };

                return Task.FromResult(operations.Where(filter).ToList());
            });

        return operationRepository;
    }
}
