using NSubstitute;
using Shouldly;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Operations;
using Unity.AI.Runtime.Prompts;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace Unity.GrantManager.AI.Operations;

public class AIExecutionModeResolverTests
{
    [Fact]
    public async Task ResolveMode_Uses_Persisted_Operation_Mode()
    {
        var repository = Substitute.For<IRepository<AIOperation, Guid>>();
        repository.GetListAsync(
                Arg.Any<Expression<Func<AIOperation, bool>>>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var predicateExpression = callInfo.Arg<Expression<Func<AIOperation, bool>>>();
                ArgumentNullException.ThrowIfNull(predicateExpression);
                var predicate = predicateExpression.Compile();
                return Task.FromResult(new[]
                {
                    new AIOperation(Guid.NewGuid(), AIPromptTypes.ApplicationScoring, Guid.NewGuid())
                    {
                        ExecutionMode = AIExecutionMode.Batch,
                        IsActive = true
                    }
                }.Where(predicate).ToList());
            });

        var resolver = new AIExecutionModeResolver(repository);

        (await resolver.ResolveModeAsync(AIPromptTypes.ApplicationScoring)).ShouldBe(AIExecutionMode.Batch);
    }

    [Fact]
    public async Task ResolveMode_Throws_When_Operation_Is_Missing()
    {
        var repository = Substitute.For<IRepository<AIOperation, Guid>>();
        repository.GetListAsync(
                Arg.Any<Expression<Func<AIOperation, bool>>>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new System.Collections.Generic.List<AIOperation>()));

        var resolver = new AIExecutionModeResolver(repository);

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => resolver.ResolveModeAsync(AIPromptTypes.AttachmentSummary));
        exception.Message.ShouldContain(AIPromptTypes.AttachmentSummary);
    }
}
