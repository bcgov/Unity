using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using Unity.AI;
using Unity.AI.DataSeed;
using Unity.AI.Domain;
using Unity.AI.Prompts;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace Unity.GrantManager.AI.DataSeed;

public class AIOperationDataSeederTests
{
    [Fact]
    public async Task Should_Seed_BuiltIn_Model_And_Operations()
    {
        var operationRepository = Substitute.For<IRepository<AIOperation, Guid>>();
        operationRepository
            .GetQueryableAsync()
            .Returns(Task.FromResult(new List<AIOperation>().AsQueryable()));
        operationRepository
            .GetListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<AIOperation, bool>>>())
            .Returns(Task.FromResult(new List<AIOperation>()));

        var modelRepository = Substitute.For<IRepository<AIModel, Guid>>();
        var model = new AIModel(Guid.NewGuid(), "Gpt5Mini")
        {
            IsActive = true,
            SettingsJson = """
            {
              "MaxOutputTokenCountSupported": true,
              "Temperature": null
            }
            """
        };
        modelRepository
            .GetQueryableAsync()
            .Returns(Task.FromResult(new List<AIModel> { model }.AsQueryable()));
        modelRepository
            .GetListAsync()
            .Returns(Task.FromResult(new List<AIModel> { model }));
        modelRepository
            .GetListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<AIModel, bool>>>())
            .Returns(Task.FromResult(new List<AIModel> { model }));

        var insertedOperations = new List<AIOperation>();
        operationRepository
            .InsertAsync(Arg.Any<AIOperation>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var operation = callInfo.Arg<AIOperation>();
                insertedOperations.Add(operation);
                return Task.FromResult(operation);
            });

        var promptRepository = Substitute.For<IRepository<AIPrompt, Guid>>();
        var prompts = new List<AIPrompt>
        {
            new(Guid.NewGuid(), AIPromptTypes.ApplicationAnalysis, 1, "system", "user"),
            new(Guid.NewGuid(), AIPromptTypes.AttachmentSummary, 1, "system", "user"),
            new(Guid.NewGuid(), AIPromptTypes.ApplicationScoring, 1, "system", "user")
        };
        promptRepository
            .GetListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<AIPrompt, bool>>>())
            .Returns(callInfo =>
            {
                var predicate = callInfo.Arg<System.Linq.Expressions.Expression<Func<AIPrompt, bool>>>().Compile();
                return Task.FromResult(prompts.Where(predicate).ToList());
            });

        var requestRepository = Substitute.For<IRepository<AIGenerationRequest, Guid>>();
        requestRepository
            .GetListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<AIGenerationRequest, bool>>>())
            .Returns(Task.FromResult(new List<AIGenerationRequest>()));
        var currentTenant = Substitute.For<ICurrentTenant>();
        var seeder = new AIOperationDataSeeder(
            operationRepository,
            modelRepository,
            promptRepository,
            requestRepository,
            currentTenant,
            Substitute.For<ILogger<AIOperationDataSeeder>>());

        await seeder.SeedAsync(new DataSeedContext());

        insertedOperations.Count.ShouldBe(4);
        insertedOperations.ShouldContain(operation => operation.Name == "Default" && operation.IsActive);
        insertedOperations.ShouldContain(operation => operation.Name == AIPromptTypes.ApplicationAnalysis && operation.CompletionTokens == 4000);
        insertedOperations.ShouldContain(operation => operation.Name == AIPromptTypes.AttachmentSummary && operation.CompletionTokens == 2000);
        insertedOperations.ShouldContain(operation => operation.Name == AIPromptTypes.ApplicationScoring && operation.CompletionTokens == 8000);
    }
}
