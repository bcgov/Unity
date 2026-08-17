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
using Unity.AI.Domain;
using Unity.AI.Models;
using Unity.AI.Operations;
using Unity.AI.Requests;
using Unity.AI.Responses;
using Unity.AI.Runtime.Prompts;
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
        aiService.GenerateApplicationScoringAsync(Arg.Do<ApplicationScoringRequest>(request => capturedRequests.Add(request)), Arg.Any<CancellationToken>())
            .Returns(new ApplicationScoringResponse());

        var operationRepository = Substitute.For<IRepository<AIOperation, Guid>>();
        operationRepository.GetListAsync(
                Arg.Any<System.Linq.Expressions.Expression<Func<AIOperation, bool>>>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var predicateExpression = callInfo.Arg<System.Linq.Expressions.Expression<Func<AIOperation, bool>>>();
                ArgumentNullException.ThrowIfNull(predicateExpression);
                var predicate = predicateExpression.Compile();
                return Task.FromResult(new[]
                {
                    new AIOperation(Guid.NewGuid(), AIPromptTypes.ApplicationScoring, Guid.NewGuid())
                    {
                        ExecutionMode = AIExecutionMode.Sequential,
                        IsActive = true
                    }
                }.Where(predicate).ToList());
            });

        var service = new ApplicationScoringService(
            aiService,
            new AIExecutionModeResolver(operationRepository),
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
