using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
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
        aiService.GenerateApplicationScoringAsync(Arg.Do<ApplicationScoringRequest>(request => capturedRequests.Add(request)), Arg.Any<CancellationToken>())
            .Returns(new ApplicationScoringResponse());

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Azure:Operations:Defaults:ExecutionMode"] = "Sequential"
            })
            .Build();

        var service = new ApplicationScoringService(
            aiService,
            new AIExecutionModeResolver(configuration),
            NullLogger<ApplicationScoringService>.Instance);

        var result = await service.RegenerateAsync(new ApplicationScoringOperationInputDto
        {
            ApplicationId = Guid.NewGuid(),
            Data = JsonSerializer.SerializeToElement(new { projectName = "Project Alpha" }),
            Attachments = new List<AIAttachmentItem>(),
            Sections =
            [
                new ApplicationScoringSectionOperationInputDto
                {
                    SectionName = "Section A",
                    SectionSchema = JsonSerializer.SerializeToElement(new { questions = new[] { new { id = "q1" } } })
                }
            ]
        });

        result.ShouldNotBeNull();
        capturedRequests.Count.ShouldBe(1);
        capturedRequests[0].SectionName.ShouldBe("Section A");
    }
}
