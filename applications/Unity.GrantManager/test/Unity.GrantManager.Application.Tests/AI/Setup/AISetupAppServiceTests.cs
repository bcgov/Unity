using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Domain;
using Unity.AI.Operations;
using Unity.AI.Runtime.Execution;
using Unity.AI.Runtime.Prompts;
using Unity.AI.Setup;
using Unity.Modules.Shared.Permissions;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.AI.Setup;

public class AISetupAppServiceTests(ITestOutputHelper outputHelper) : GrantManagerApplicationTestBase(outputHelper)
{
    private IAIModelAppService ModelService => GetRequiredService<IAIModelAppService>();
    private IAIOperationAppService OperationService => GetRequiredService<IAIOperationAppService>();

    [Fact]
    public async Task Should_Create_List_And_Edit_Models()
    {
        var created = await WithUnitOfWorkAsync(() => ModelService.CreateAsync(NewModel("  gpt-5-mini  ")));

        created.Name.ShouldBe("gpt-5-mini");
        created.IsActive.ShouldBeTrue();

        var updated = await WithUnitOfWorkAsync(() => ModelService.UpdateAsync(created.Id, new CreateUpdateAIModelDto
        {
            Name = "gpt-5-mini",
            Provider = AISetupConsts.OpenAIProvider,
            ApiVersion = "2024-10-21",
            IsActive = false
        }));

        updated.IsActive.ShouldBeFalse();

        var list = await WithUnitOfWorkAsync(() => ModelService.GetListAsync());
        list.Items.ShouldContain(model => model.Id == created.Id && !model.IsActive);
    }

    [Fact]
    public async Task Should_Reject_Duplicate_Model_Name()
    {
        await WithUnitOfWorkAsync(() => ModelService.CreateAsync(NewModel("gpt-5-nano")));

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            WithUnitOfWorkAsync(() => ModelService.CreateAsync(NewModel("GPT-5-NANO"))));
        exception.Code.ShouldBe(AIErrorCodes.SetupModelNameDuplicate);
    }

    [Fact]
    public async Task Should_Reject_Unsupported_Provider()
    {
        var input = NewModel("some-model");
        input.Provider = "Other";

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            WithUnitOfWorkAsync(() => ModelService.CreateAsync(input)));
        exception.Code.ShouldBe(AIErrorCodes.SetupProviderNotSupported);
    }

    [Fact]
    public async Task Should_Block_Deactivate_And_Delete_When_Model_Is_Used()
    {
        var model = await WithUnitOfWorkAsync(() => ModelService.CreateAsync(NewModel("gpt-5-mini")));
        await WithUnitOfWorkAsync(() => OperationService.UpdateAsync(
            AIPromptTypes.AttachmentSummary,
            NewOperation(model.Id)));

        var deactivate = await Should.ThrowAsync<BusinessException>(() =>
            WithUnitOfWorkAsync(() => ModelService.UpdateAsync(model.Id, new CreateUpdateAIModelDto
            {
                Name = model.Name,
                Provider = model.Provider,
                ApiVersion = model.ApiVersion,
                IsActive = false
            })));
        deactivate.Code.ShouldBe(AIErrorCodes.SetupModelInUse);

        var delete = await Should.ThrowAsync<BusinessException>(() =>
            WithUnitOfWorkAsync(() => ModelService.DeleteAsync(model.Id)));
        delete.Code.ShouldBe(AIErrorCodes.SetupModelInUse);

        var reloaded = await WithUnitOfWorkAsync(() => ModelService.GetAsync(model.Id));
        reloaded.IsActive.ShouldBeTrue();
        reloaded.UsedByOperations.ShouldBe([AIPromptTypes.AttachmentSummary]);
    }

    [Fact]
    public async Task Should_Delete_Model_When_Unused()
    {
        var model = await WithUnitOfWorkAsync(() => ModelService.CreateAsync(NewModel("gpt-retired")));

        await WithUnitOfWorkAsync(() => ModelService.DeleteAsync(model.Id));

        var list = await WithUnitOfWorkAsync(() => ModelService.GetListAsync());
        list.Items.ShouldNotContain(item => item.Id == model.Id);
    }

    [Fact]
    public async Task Should_List_All_Built_In_Operations_When_None_Configured()
    {
        var list = await WithUnitOfWorkAsync(() => OperationService.GetListAsync());

        list.Items.Select(item => item.Name).ShouldBe(AIPromptTypes.All);
        list.Items.ShouldAllBe(item => item.IsBuiltIn && !item.IsConfigured && item.Id == null);
    }

    [Fact]
    public async Task Should_Flag_Only_Application_Scoring_As_Using_Execution_Mode()
    {
        var list = await WithUnitOfWorkAsync(() => OperationService.GetListAsync());

        list.Items.Where(item => item.UsesExecutionMode).Select(item => item.Name)
            .ShouldBe([AIPromptTypes.ApplicationScoring]);
    }

    [Fact]
    public async Task Should_Create_Then_Update_Operation_When_Not_Configured()
    {
        var model = await WithUnitOfWorkAsync(() => ModelService.CreateAsync(NewModel("gpt-5-mini")));
        var other = await WithUnitOfWorkAsync(() => ModelService.CreateAsync(NewModel("gpt-5-nano")));

        var created = await WithUnitOfWorkAsync(() => OperationService.UpdateAsync(
            "applicationscoring",
            NewOperation(model.Id, executionMode: nameof(AIExecutionMode.Parallel))));

        created.Name.ShouldBe(AIPromptTypes.ApplicationScoring);
        created.IsConfigured.ShouldBeTrue();
        created.AIModelName.ShouldBe("gpt-5-mini");
        created.ExecutionMode.ShouldBe(nameof(AIExecutionMode.Parallel));

        var updated = await WithUnitOfWorkAsync(() => OperationService.UpdateAsync(
            AIPromptTypes.ApplicationScoring,
            NewOperation(other.Id, isActive: false)));

        updated.Id.ShouldBe(created.Id);
        updated.AIModelName.ShouldBe("gpt-5-nano");
        updated.IsActive.ShouldBeFalse();

        var list = await WithUnitOfWorkAsync(() => OperationService.GetListAsync());
        list.Items.Count(item => item.Name == AIPromptTypes.ApplicationScoring).ShouldBe(1);
    }

    [Fact]
    public async Task Should_Reject_Operation_When_Model_Inactive()
    {
        var model = await WithUnitOfWorkAsync(() => ModelService.CreateAsync(NewModel("gpt-4o-mini", isActive: false)));

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            WithUnitOfWorkAsync(() => OperationService.UpdateAsync(AIPromptTypes.FormMapping, NewOperation(model.Id))));
        exception.Code.ShouldBe(AIErrorCodes.SetupModelInactive);
    }

    [Fact]
    public async Task Should_Reject_Operation_When_Execution_Mode_Invalid()
    {
        var model = await WithUnitOfWorkAsync(() => ModelService.CreateAsync(NewModel("gpt-5-mini")));

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            WithUnitOfWorkAsync(() => OperationService.UpdateAsync(
                AIPromptTypes.FormMapping,
                NewOperation(model.Id, executionMode: "Sideways"))));
        exception.Code.ShouldBe(AIErrorCodes.SetupExecutionModeInvalid);
    }

    [Theory]
    [InlineData(nameof(AIExecutionMode.Parallel))]
    [InlineData(nameof(AIExecutionMode.Batch))]
    public async Task Should_Reject_Parallel_Or_Batch_When_Operation_Ignores_Mode(string executionMode)
    {
        var model = await WithUnitOfWorkAsync(() => ModelService.CreateAsync(NewModel("gpt-5-mini")));

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            WithUnitOfWorkAsync(() => OperationService.UpdateAsync(
                AIPromptTypes.FormMapping,
                NewOperation(model.Id, executionMode: executionMode))));
        exception.Code.ShouldBe(AIErrorCodes.SetupExecutionModeNotUsed);
    }

    [Fact]
    public async Task Should_Reject_Unknown_Operation()
    {
        var model = await WithUnitOfWorkAsync(() => ModelService.CreateAsync(NewModel("gpt-5-mini")));

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            WithUnitOfWorkAsync(() => OperationService.UpdateAsync("MadeUpOperation", NewOperation(model.Id))));
        exception.Code.ShouldBe(AIErrorCodes.SetupOperationUnknown);
    }

    [Fact]
    public async Task Should_Reset_Runtime_Cache_When_Saving()
    {
        var signal = GetRequiredService<AIConfigurationCacheSignal>();
        var token = signal.GetChangeToken();

        await WithUnitOfWorkAsync(() => ModelService.CreateAsync(NewModel("gpt-5-mini")));

        token.HasChanged.ShouldBeTrue();
    }

    [Theory]
    [InlineData(typeof(AIModelAppService))]
    [InlineData(typeof(AIOperationAppService))]
    public void Should_Let_Both_IT_Roles_Read_App_Services(Type appServiceType)
    {
        var attribute = appServiceType.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        attribute.ShouldNotBeNull();
        attribute.Policy.ShouldBe(IdentityConsts.ITAdminOrITOperationsPolicyName);
    }

    [Theory]
    [InlineData(typeof(AIModelAppService), nameof(AIModelAppService.CreateAsync), IdentityConsts.ITAdminPolicyName)]
    [InlineData(typeof(AIModelAppService), nameof(AIModelAppService.UpdateAsync), IdentityConsts.ITAdminPolicyName)]
    [InlineData(typeof(AIModelAppService), nameof(AIModelAppService.DeleteAsync), IdentityConsts.ITAdminPolicyName)]
    [InlineData(typeof(AIModelAppService), nameof(AIModelAppService.TestAsync), null)]
    [InlineData(typeof(AIModelAppService), nameof(AIModelAppService.GetListAsync), null)]
    [InlineData(typeof(AIOperationAppService), nameof(AIOperationAppService.UpdateAsync), IdentityConsts.ITOperationsPolicyName)]
    [InlineData(typeof(AIOperationAppService), nameof(AIOperationAppService.GetListAsync), null)]
    public void Should_Restrict_Changes_To_The_Right_IT_Role(Type appServiceType, string methodName, string? policy)
    {
        var attribute = appServiceType.GetMethod(methodName)!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        // null: no method attribute, so only the class policy (both IT roles) applies.
        attribute?.Policy.ShouldBe(policy);
        if (policy == null)
        {
            attribute.ShouldBeNull();
        }
    }

    [Fact]
    public void Should_Match_Execution_Mode_Enum_Names()
    {
        AISetupConsts.ExecutionModes.ShouldBe(Enum.GetNames<AIExecutionMode>());
    }

    [Fact]
    public void Should_Match_Model_Type_Enum_Names()
    {
        AISetupConsts.ModelTypes.ShouldBe(Enum.GetNames<AIModelType>());
    }

    [Fact]
    public async Task Should_Create_Embedding_Model_And_List_Its_Type()
    {
        var created = await WithUnitOfWorkAsync(() => ModelService.CreateAsync(
            NewModel("text-embedding-3-large", type: AISetupConsts.EmbeddingModelType)));

        created.Type.ShouldBe(AISetupConsts.EmbeddingModelType);
        var list = await WithUnitOfWorkAsync(() => ModelService.GetListAsync());
        list.Items.ShouldContain(model => model.Id == created.Id && model.Type == AISetupConsts.EmbeddingModelType);
    }

    [Fact]
    public async Task Should_Reject_Embedding_Model_For_Operation()
    {
        var embedding = await WithUnitOfWorkAsync(() => ModelService.CreateAsync(
            NewModel("text-embedding-3-large", type: AISetupConsts.EmbeddingModelType)));

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            WithUnitOfWorkAsync(() => OperationService.UpdateAsync(AIPromptTypes.FormMapping, NewOperation(embedding.Id))));
        exception.Code.ShouldBe(AIErrorCodes.SetupModelNotChat);
    }

    [Fact]
    public async Task Should_Reject_Malformed_Api_Version()
    {
        var input = NewModel("gpt-5-mini");
        input.ApiVersion = "latest";

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            WithUnitOfWorkAsync(() => ModelService.CreateAsync(input)));
        exception.Code.ShouldBe(AIErrorCodes.SetupApiVersionInvalid);
    }

    [Fact]
    public async Task Should_Keep_Each_Models_Api_Version()
    {
        var created = await WithUnitOfWorkAsync(() => ModelService.CreateAsync(NewModel("gpt-5-mini")));
        var update = NewModel("gpt-5-mini");
        update.ApiVersion = "2025-04-01-preview";

        var updated = await WithUnitOfWorkAsync(() => ModelService.UpdateAsync(created.Id, update));

        created.ApiVersion.ShouldBe("2024-10-21");
        // Any well-formed version, not only those the SDK knows.
        updated.ApiVersion.ShouldBe("2025-04-01-preview");
    }

    [Fact]
    public async Task Should_Reject_Unknown_Model_Type()
    {
        var exception = await Should.ThrowAsync<BusinessException>(() =>
            WithUnitOfWorkAsync(() => ModelService.CreateAsync(NewModel("gpt-5-mini", type: "Vision"))));
        exception.Code.ShouldBe(AIErrorCodes.SetupModelTypeInvalid);
    }

    [Fact]
    public async Task Should_Block_Type_Change_When_Model_Is_Used()
    {
        var model = await WithUnitOfWorkAsync(() => ModelService.CreateAsync(NewModel("gpt-5-mini")));
        await WithUnitOfWorkAsync(() => OperationService.UpdateAsync(AIPromptTypes.FormMapping, NewOperation(model.Id)));

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            WithUnitOfWorkAsync(() => ModelService.UpdateAsync(model.Id,
                NewModel("gpt-5-mini", type: AISetupConsts.EmbeddingModelType))));
        exception.Code.ShouldBe(AIErrorCodes.SetupModelInUse);
    }

    [Theory]
    [InlineData(null, "secret", "2024-10-21", AIModelConnectionTester.EndpointNotConfigured)]
    [InlineData("not a uri", "secret", "2024-10-21", AIModelConnectionTester.EndpointNotConfigured)]
    [InlineData("https://example.test", "", "2024-10-21", AIModelConnectionTester.ApiKeyNotConfigured)]
    [InlineData("https://example.test", "secret", null, AIModelConnectionTester.ApiVersionNotConfigured)]
    [InlineData("https://example.test", "secret", "latest", AIModelConnectionTester.ApiVersionNotConfigured)]
    public async Task Should_Report_Missing_Configuration_Without_Calling_Azure(
        string? endpoint,
        string apiKey,
        string? apiVersion,
        string expectedErrorCode)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>
            {
                ["Azure:OpenAI:Endpoint"] = endpoint,
                ["Azure:OpenAI:ApiKey"] = apiKey
            })
            .Build();
        var chatClientFactory = Substitute.For<OpenAIChatClientFactory>();
        var transport = new OpenAITransportService(
            chatClientFactory,
            Substitute.For<Microsoft.Extensions.Logging.ILogger<OpenAITransportService>>());
        var tester = new AIModelConnectionTester(transport, chatClientFactory, configuration);

        var result = await tester.TestAsync(new AIModel(Guid.NewGuid(), "gpt-5-mini", AISetupConsts.OpenAIProvider)
        {
            ApiVersion = apiVersion ?? string.Empty
        });

        result.Success.ShouldBeFalse();
        result.ErrorCode.ShouldBe(expectedErrorCode);
        chatClientFactory.ReceivedCalls().ShouldBeEmpty();
    }

    private static CreateUpdateAIModelDto NewModel(
        string name,
        bool isActive = true,
        string type = AISetupConsts.ChatModelType)
    {
        return new CreateUpdateAIModelDto
        {
            Name = name,
            Provider = AISetupConsts.OpenAIProvider,
            Type = type,
            ApiVersion = "2024-10-21",
            IsActive = isActive
        };
    }

    private static UpdateAIOperationDto NewOperation(
        Guid modelId,
        string executionMode = nameof(AIExecutionMode.Sequential),
        bool isActive = true)
    {
        return new UpdateAIOperationDto
        {
            AIModelId = modelId,
            ExecutionMode = executionMode,
            IsActive = isActive
        };
    }
}
