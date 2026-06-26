using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Operations;
using Unity.AI.Requests;
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
        var modelRepository = Substitute.For<IRepository<AIModel, Guid>>();
        modelRepository
            .GetListAsync(Arg.Any<Expression<Func<AIModel, bool>>>())
            .Returns(new List<AIModel>());

        var operationRepository = Substitute.For<IRepository<AIOperation, Guid>>();
        operationRepository
            .GetListAsync(Arg.Any<Expression<Func<AIOperation, bool>>>())
            .Returns(new List<AIOperation>());

        var promptRepository = Substitute.For<IRepository<AIPrompt, Guid>>();
        var configuration = new ConfigurationBuilder().Build();
        var multiTenantDataFilter = Substitute.For<IDataFilter<IMultiTenant>>();
        multiTenantDataFilter.Disable().Returns(Substitute.For<IDisposable>());

        var resolver = new OpenAIConfigurationResolver(
            modelRepository,
            operationRepository,
            promptRepository,
            configuration,
            multiTenantDataFilter);

        var logger = Substitute.For<ILogger<OpenAIRuntimeService>>();
        var runtimeService = new OpenAIRuntimeService(
            logger,
            null!,
            resolver,
            null!,
            null!);

        var ex = await Should.ThrowAsync<InvalidOperationException>(() =>
            runtimeService.GenerateApplicationAnalysisAsync(new ApplicationAnalysisRequest
            {
                Schema = JsonSerializer.SerializeToElement(new { project = "x" }),
                Data = JsonSerializer.SerializeToElement(new { project = "x" })
            }));

        ex.Message.ShouldContain("AI operation");
    }
}
