using Microsoft.Extensions.Configuration;
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

        var service = CreateService(aiService, "Sequential");

        var result = await service.RegenerateAsync(new ApplicationScoringOperationInputDto
        {
            ApplicationId = Guid.NewGuid(),
            Data = JsonSerializer.SerializeToElement(new { project_name = "Project Alpha" }),
            Attachments = [new AIAttachmentItem { Name = "summary.pdf", Summary = "Summary text" }],
            Sections =
            [
                CreateSection("Section A", "q1")
            ]
        });

        result.ShouldContain("\"q1\"");
        capturedRequests.Count.ShouldBe(1);
        capturedRequests[0].SectionName.ShouldBe("Section A");
        capturedRequests[0].Attachments.Count.ShouldBe(1);
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

        var service = CreateService(aiService, "Batch");

        var result = await service.RegenerateAsync(new ApplicationScoringOperationInputDto
        {
            ApplicationId = Guid.NewGuid(),
            Data = JsonSerializer.SerializeToElement(new { project_name = "Project Beta" }),
            Attachments = [],
            Sections =
            [
                CreateSection("Section A", "q1"),
                CreateSection("Section B", "q2")
            ]
        });

        result.ShouldContain("\"q1\"");
        capturedRequests.Count.ShouldBe(1);
        capturedRequests[0].SectionName.ShouldBe("All Sections");
        capturedRequests[0].SectionSchema.ValueKind.ShouldBe(JsonValueKind.Array);
        capturedRequests[0].SectionSchema.GetArrayLength().ShouldBe(2);
    }

    [Fact]
    public async Task RegenerateAsync_Batch_Mode_Rejects_Non_Array_Section_Schemas()
    {
        var aiService = Substitute.For<IAIService>();
        var service = CreateService(aiService, "Batch");

        var result = await service.RegenerateAsync(new ApplicationScoringOperationInputDto
        {
            ApplicationId = Guid.NewGuid(),
            Data = JsonSerializer.SerializeToElement(new { project_name = "Project Gamma" }),
            Attachments = [],
            Sections =
            [
                new ApplicationScoringSectionOperationInputDto
                {
                    SectionName = "Broken Section",
                    SectionSchema = JsonSerializer.SerializeToElement(new { q1 = "bad" })
                }
            ]
        });

        result.ShouldBe("{}");
        await aiService.DidNotReceive().GenerateApplicationScoringAsync(Arg.Any<ApplicationScoringRequest>(), Arg.Any<CancellationToken>());
    }

    private static ApplicationScoringService CreateService(IAIService aiService, string executionMode)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Azure:Operations:Defaults:ExecutionMode"] = executionMode
            })
            .Build();

        return new ApplicationScoringService(
            aiService,
            new AIExecutionModeResolver(configuration),
            NullLogger<ApplicationScoringService>.Instance);
    }

    private static ApplicationScoringSectionOperationInputDto CreateSection(string name, string questionId)
    {
        return new ApplicationScoringSectionOperationInputDto
        {
            SectionName = name,
            SectionSchema = JsonSerializer.SerializeToElement(new[]
            {
                new { id = questionId }
            })
        };
    }
}
