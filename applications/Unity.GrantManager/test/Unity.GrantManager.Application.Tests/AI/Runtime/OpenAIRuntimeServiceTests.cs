using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Operations;
using Unity.AI.Prompts;
using Unity.AI.Runtime;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace Unity.GrantManager.AI.Runtime;

public class OpenAIRuntimeServiceTests
{
    [Fact]
    public async Task Should_Throw_When_Analysis_Operation_Is_Not_Configured()
    {
        var resolver = CreateResolverWithNoOperations();

        await Should.ThrowAsync<Exception>(() =>
            resolver.ResolveOperationSettingsAsync(AIPromptTypes.ApplicationAnalysis));
    }

    private static OpenAIConfigurationResolver CreateResolverWithNoOperations()
    {
        var modelRepository = Substitute.For<IRepository<AIModel, Guid>>();
        modelRepository
            .GetListAsync(Arg.Any<Expression<Func<AIModel, bool>>>())
            .Returns(Task.FromResult(new List<AIModel>()));

        var operationRepository = Substitute.For<IRepository<AIOperation, Guid>>();
        operationRepository
            .GetListAsync(Arg.Any<Expression<Func<AIOperation, bool>>>())
            .Returns(Task.FromResult(new List<AIOperation>()));

        var promptRepository = Substitute.For<IRepository<AIPrompt, Guid>>();
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
        var multiTenantDataFilter = Substitute.For<IDataFilter<IMultiTenant>>();
        multiTenantDataFilter.Disable().Returns(Substitute.For<IDisposable>());

        return new OpenAIConfigurationResolver(
            modelRepository,
            operationRepository,
            promptRepository,
            configuration,
            multiTenantDataFilter);
    }
}
