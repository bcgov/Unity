using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Operations;
using Unity.AI.Runtime.Prompts;
using Unity.AI.Runtime.Execution;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace Unity.GrantManager.AI.Runtime;

public class OpenAIConfigurationResolverTests
{
    [Fact]
    public async Task Should_Throw_When_No_Operations_Are_Configured()
    {
        var resolver = CreateResolver();

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => resolver.ResolveOperationSettingsAsync(AIPromptTypes.ApplicationAnalysis));
        ex.Message.ShouldContain("AI operation");
    }

    [Fact]
    public async Task Should_Resolve_Operation_Strictly_From_Database()
    {
        var modelRepository = Substitute.For<IRepository<AIModel, Guid>>();
        var operationRepository = Substitute.For<IRepository<AIOperation, Guid>>();
        var promptRepository = Substitute.For<IRepository<AIPrompt, Guid>>();

        var modelId = Guid.NewGuid();
        var promptId = Guid.NewGuid();

        modelRepository
            .GetAsync(modelId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new AIModel(modelId, "gpt-5-mini", "OpenAI")
            {
                IsActive = true,
                SettingsJson = JsonSerializer.Serialize(new AIModelSettings
                {
                    MaxOutputTokenCountSupported = true,
                    Temperature = 0.25
                })
            });

        operationRepository
            .GetListAsync(Arg.Any<Expression<Func<AIOperation, bool>>>())
            .Returns(callInfo =>
            {
                var predicate = callInfo.Arg<Expression<Func<AIOperation, bool>>>();
                ArgumentNullException.ThrowIfNull(predicate);
                return Task.FromResult(new List<AIOperation>
                {
                new(Guid.NewGuid(), AIPromptTypes.ApplicationAnalysis, modelId)
                    {
                        ExecutionMode = AIExecutionMode.Sequential,
                        CompletionTokens = 2222,
                        IsActive = true
                    }
                }.Where(predicate.Compile()).ToList());
            });

        promptRepository
            .GetAsync(promptId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AIPrompt(promptId, AIPromptTypes.ApplicationAnalysis, 1, "system", "user")
            {
                MetadataJson = "{}",
                IsActive = true
            }));
        promptRepository
            .GetListAsync(Arg.Any<Expression<Func<AIPrompt, bool>>>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<AIPrompt>
            {
                new(promptId, AIPromptTypes.ApplicationAnalysis, 1, "system", "user")
                {
                    MetadataJson = "{}",
                    IsActive = true
                }
            }));

        var resolver = CreateResolver(
            new Dictionary<string, string?>
            {
                ["Azure:OpenAI:ApiKey"] = "secret",
            },
            modelRepository,
            operationRepository,
            promptRepository);

        var settings = await resolver.ResolveOperationSettingsAsync(AIPromptTypes.ApplicationAnalysis);

        settings.ProviderName.ShouldBe("OpenAI");
        settings.ProfileName.ShouldBe("gpt-5-mini");
        settings.Endpoint.ShouldBe(new Uri("https://example.test"));
        settings.DeploymentName.ShouldBe("gpt-5-mini");
        settings.Temperature.ShouldBe(0.25);
        settings.CompletionTokens.ShouldBe(2222);
        settings.PromptVersion.ShouldBe("v1");
    }

    [Fact]
    public async Task Should_Throw_When_Operation_Is_Missing()
    {
        var modelRepository = Substitute.For<IRepository<AIModel, Guid>>();
        var operationRepository = Substitute.For<IRepository<AIOperation, Guid>>();
        var promptRepository = Substitute.For<IRepository<AIPrompt, Guid>>();

        var modelId = Guid.NewGuid();
        var promptId = Guid.NewGuid();

        modelRepository
            .GetAsync(modelId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new AIModel(modelId, "gpt-5-mini", "OpenAI")
            {
                IsActive = true,
                SettingsJson = JsonSerializer.Serialize(new AIModelSettings
                {
                    Temperature = 0.2
                })
            });

        operationRepository
            .GetListAsync(Arg.Any<Expression<Func<AIOperation, bool>>>())
            .Returns(callInfo =>
            {
                var predicate = callInfo.Arg<Expression<Func<AIOperation, bool>>>();
                ArgumentNullException.ThrowIfNull(predicate);
                return Task.FromResult(new List<AIOperation>
                {
                new(Guid.NewGuid(), "Default", modelId)
                    {
                        ExecutionMode = AIExecutionMode.Sequential,
                        CompletionTokens = 2000,
                        IsActive = true
                    }
                }.Where(predicate.Compile()).ToList());
            });

        promptRepository
            .GetAsync(promptId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AIPrompt(promptId, AIPromptTypes.ApplicationAnalysis, 1, "system", "user")
            {
                MetadataJson = "{}",
                IsActive = true
            }));
        promptRepository
            .GetListAsync(Arg.Any<Expression<Func<AIPrompt, bool>>>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<AIPrompt>
            {
                new(promptId, AIPromptTypes.ApplicationAnalysis, 1, "system", "user")
                {
                    MetadataJson = "{}",
                    IsActive = true
                }
            }));

        var resolver = CreateResolver(
            new Dictionary<string, string?>
            {
                ["Azure:OpenAI:ApiKey"] = "secret",
            },
            modelRepository,
            operationRepository,
            promptRepository);

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => resolver.ResolveOperationSettingsAsync(AIPromptTypes.ApplicationAnalysis));

        exception.Message.ShouldContain("AI operation 'ApplicationAnalysis' is not configured.");
    }

    [Fact]
    public async Task Should_Resolve_Model_Values_Strictly_From_Database()
    {
        var modelRepository = Substitute.For<IRepository<AIModel, Guid>>();
        modelRepository
            .GetListAsync(Arg.Any<Expression<Func<AIModel, bool>>>())
            .Returns(callInfo =>
            {
                var predicate = callInfo.Arg<Expression<Func<AIModel, bool>>>();
                ArgumentNullException.ThrowIfNull(predicate);
                return Task.FromResult(new List<AIModel>
                {
                    new(Guid.NewGuid(), "gpt-5-mini", "OpenAI")
                    {
                        IsActive = true,
                        SettingsJson = JsonSerializer.Serialize(new AIModelSettings
                        {
                            MaxOutputTokenCountSupported = true,
                            Temperature = 0.35
                        })
                    }
                }.Where(predicate.Compile()).ToList());
            });

        var resolver = CreateResolver(modelRepository: modelRepository);

        resolver.ResolveProviderName().ShouldBe("OpenAI");
        (await resolver.ResolveDeploymentNameAsync("gpt-5-mini")).ShouldBe("gpt-5-mini");
        (await resolver.ResolveEndpointAsync("gpt-5-mini")).ShouldBe(new Uri("https://example.test"));
        (await resolver.ResolveConfiguredTemperatureAsync("gpt-5-mini")).ShouldBe(0.35);
        (await resolver.ResolveMaxOutputTokenCountSupportedAsync("gpt-5-mini")).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Resolve_ApiKey_From_Provider_Secret()
    {
        var modelRepository = Substitute.For<IRepository<AIModel, Guid>>();
        modelRepository
            .GetListAsync(Arg.Any<Expression<Func<AIModel, bool>>>())
            .Returns(callInfo =>
            {
                var predicate = callInfo.Arg<Expression<Func<AIModel, bool>>>();
                ArgumentNullException.ThrowIfNull(predicate);
                return Task.FromResult(new List<AIModel>
                {
                    new(Guid.NewGuid(), "Default", "OpenAI")
                    {
                        IsActive = true,
                        SettingsJson = JsonSerializer.Serialize(new AIModelSettings
                        {
                            MaxOutputTokenCountSupported = true
                        })
                    }
                }.Where(predicate.Compile()).ToList());
            });

        var resolver = CreateResolver(
            new Dictionary<string, string?>
            {
                ["Azure:OpenAI:ApiKey"] = "secret"
            },
            modelRepository);

        (await resolver.ResolveApiKeyAsync()).ShouldBe("secret");
    }

    [Fact]
    public async Task Should_Resolve_Operation_Settings_From_Host_Context()
    {
        var modelRepository = Substitute.For<IRepository<AIModel, Guid>>();
        var operationRepository = Substitute.For<IRepository<AIOperation, Guid>>();
        var promptRepository = Substitute.For<IRepository<AIPrompt, Guid>>();
        var multiTenantDataFilter = Substitute.For<IDataFilter<IMultiTenant>>();
        multiTenantDataFilter.Disable().Returns(Substitute.For<IDisposable>());

        var modelId = Guid.NewGuid();
        var promptId = Guid.NewGuid();

        modelRepository
            .GetAsync(modelId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new AIModel(modelId, "gpt-5-mini", "OpenAI")
            {
                IsActive = true,
                SettingsJson = JsonSerializer.Serialize(new AIModelSettings
                {
                    Temperature = 0.1
                })
            });

        operationRepository
            .GetListAsync(Arg.Any<Expression<Func<AIOperation, bool>>>())
            .Returns(callInfo =>
            {
                var predicate = callInfo.Arg<Expression<Func<AIOperation, bool>>>();
                ArgumentNullException.ThrowIfNull(predicate);
                return Task.FromResult(new List<AIOperation>
                {
                new(Guid.NewGuid(), AIPromptTypes.ApplicationAnalysis, modelId)
                    {
                        IsActive = true,
                        CompletionTokens = 2000
                    }
                }.Where(predicate.Compile()).ToList());
            });

        promptRepository
            .GetAsync(promptId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AIPrompt(promptId, AIPromptTypes.ApplicationAnalysis, 1, "system", "user")
            {
                MetadataJson = "{}",
                IsActive = true
            }));
        promptRepository
            .GetListAsync(Arg.Any<Expression<Func<AIPrompt, bool>>>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<AIPrompt>
            {
                new(promptId, AIPromptTypes.ApplicationAnalysis, 1, "system", "user")
                {
                    MetadataJson = "{}",
                    IsActive = true
                }
            }));

        var resolver = CreateResolver(
            new Dictionary<string, string?>
            {
                ["Azure:OpenAI:ApiKey"] = "secret"
            },
            modelRepository,
            operationRepository,
            promptRepository,
            multiTenantDataFilter);

        var settings = await resolver.ResolveOperationSettingsAsync(AIPromptTypes.ApplicationAnalysis);

        settings.PromptVersion.ShouldBe("v1");
        multiTenantDataFilter.Received().Disable();
    }

    [Fact]
    public async Task Should_Select_Newest_Tenant_Prompt_And_Isolate_It_From_Global_Cache()
    {
        var tenantId = Guid.NewGuid();
        var tenantWithoutOverrideId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var operationRepository = Substitute.For<IRepository<AIOperation, Guid>>();
        operationRepository.GetListAsync(Arg.Any<Expression<Func<AIOperation, bool>>>())
            .Returns(Task.FromResult(new List<AIOperation>
            {
                new(Guid.NewGuid(), AIPromptTypes.ApplicationAnalysis, modelId)
                {
                    IsActive = true,
                    CompletionTokens = 100
                }
            }));

        var modelRepository = Substitute.For<IRepository<AIModel, Guid>>();
        modelRepository.GetAsync(modelId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new AIModel(modelId, "gpt-5-mini", "OpenAI")
            {
                IsActive = true,
                SettingsJson = JsonSerializer.Serialize(new AIModelSettings())
            });

        var promptRepository = Substitute.For<IRepository<AIPrompt, Guid>>();
        promptRepository.GetListAsync(
                Arg.Any<Expression<Func<AIPrompt, bool>>>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<AIPrompt>
            {
                new(Guid.NewGuid(), AIPromptTypes.ApplicationAnalysis, 1, "global", "global")
                {
                    IsActive = true
                },
                new(Guid.NewGuid(), AIPromptTypes.ApplicationAnalysis, 2, "tenant", "tenant", tenantId)
                {
                    IsActive = true
                }
            }));

        var hostResolver = CreateResolver(
            modelRepository: modelRepository,
            operationRepository: operationRepository,
            promptRepository: promptRepository,
            memoryCache: cache);
        var tenantResolver = CreateResolver(
            modelRepository: modelRepository,
            operationRepository: operationRepository,
            promptRepository: promptRepository,
            memoryCache: cache,
            tenantId: tenantId);
        var tenantWithoutOverrideResolver = CreateResolver(
            modelRepository: modelRepository,
            operationRepository: operationRepository,
            promptRepository: promptRepository,
            memoryCache: cache,
            tenantId: tenantWithoutOverrideId);

        var hostSettings = await hostResolver.ResolveOperationSettingsAsync(AIPromptTypes.ApplicationAnalysis);
        var tenantSettings = await tenantResolver.ResolveOperationSettingsAsync(AIPromptTypes.ApplicationAnalysis);
        var tenantWithoutOverrideSettings = await tenantWithoutOverrideResolver.ResolveOperationSettingsAsync(AIPromptTypes.ApplicationAnalysis);

        hostSettings.PromptVersion.ShouldBe("v1");
        tenantSettings.PromptVersion.ShouldBe("v2");
        tenantWithoutOverrideSettings.PromptVersion.ShouldBe("v1");
    }

    private static OpenAIConfigurationResolver CreateResolver(
        IReadOnlyDictionary<string, string?>? values = null,
        IRepository<AIModel, Guid>? modelRepository = null,
        IRepository<AIOperation, Guid>? operationRepository = null,
        IRepository<AIPrompt, Guid>? promptRepository = null,
        IDataFilter<IMultiTenant>? multiTenantDataFilter = null,
        IMemoryCache? memoryCache = null,
        Guid? tenantId = null)
    {
        var configurationValues = new Dictionary<string, string?>
        {
            ["Azure:OpenAI:Endpoint"] = "https://example.test",
            ["Azure:OpenAI:ApiKey"] = "secret"
        };

        if (values != null)
        {
            foreach (var pair in values)
            {
                configurationValues[pair.Key] = pair.Value;
            }
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();

        var filter = multiTenantDataFilter ?? Substitute.For<IDataFilter<IMultiTenant>>();
        filter.Disable().Returns(Substitute.For<IDisposable>());
        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Id.Returns(tenantId);
        return new OpenAIConfigurationResolver(
            modelRepository ?? CreateEmptyModelRepository(),
            operationRepository ?? CreateEmptyOperationRepository(),
            promptRepository ?? CreateEmptyPromptRepository(),
            memoryCache ?? Substitute.For<IMemoryCache>(),
            configuration,
            filter,
            currentTenant);
    }

    private static IRepository<AIModel, Guid> CreateEmptyModelRepository()
    {
        var modelRepository = Substitute.For<IRepository<AIModel, Guid>>();
        modelRepository
            .GetListAsync(Arg.Any<Expression<Func<AIModel, bool>>>())
            .Returns(Task.FromResult(new List<AIModel>()));
        return modelRepository;
    }

    private static IRepository<AIOperation, Guid> CreateEmptyOperationRepository()
    {
        var operationRepository = Substitute.For<IRepository<AIOperation, Guid>>();
        operationRepository
            .GetListAsync(Arg.Any<Expression<Func<AIOperation, bool>>>())
            .Returns(Task.FromResult(new List<AIOperation>()));
        return operationRepository;
    }

    private static IRepository<AIPrompt, Guid> CreateEmptyPromptRepository()
    {
        var promptRepository = Substitute.For<IRepository<AIPrompt, Guid>>();
        promptRepository
            .GetAsync(Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(new AIPrompt(callInfo.Arg<Guid>(), "Default", 1, "system", "user")
            {
                MetadataJson = "{}"
            }));
        return promptRepository;
    }
}
