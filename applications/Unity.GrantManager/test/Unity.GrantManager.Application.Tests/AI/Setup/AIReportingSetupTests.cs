using Shouldly;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Operations;
using Unity.AI.Runtime.Execution;
using Unity.AI.Setup;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.AI.Setup;

/// <summary>AI Reporting's chat and embedding model rows in AI Setup (AB#35030).</summary>
public class AIReportingSetupTests(ITestOutputHelper outputHelper) : GrantManagerApplicationTestBase(outputHelper)
{
    private IAIModelAppService ModelService => GetRequiredService<IAIModelAppService>();
    private IAIOperationAppService OperationService => GetRequiredService<IAIOperationAppService>();
    private AIReportingModelResolver Resolver => GetRequiredService<AIReportingModelResolver>();

    [Fact]
    public async Task Should_List_Both_AI_Reporting_Rows_Unconfigured_And_Not_With_Operations()
    {
        var reporting = await WithUnitOfWorkAsync(() => OperationService.GetAIReportingAsync());
        var operations = await WithUnitOfWorkAsync(() => OperationService.GetListAsync());

        reporting.Items.Select(item => item.Name).ShouldBe(AISetupConsts.AIReportingOperations);
        reporting.Items.ShouldAllBe(item => !item.IsConfigured && item.IsBuiltIn);
        reporting.Items.Single(item => item.Name == AISetupConsts.AIReportingEmbeddingOperation)
            .RequiredModelType.ShouldBe(AISetupConsts.EmbeddingModelType);
        operations.Items.ShouldNotContain(item => AISetupConsts.AIReportingOperations.Contains(item.Name));
    }

    [Fact]
    public async Task Should_Set_Both_Models_And_Send_Them_To_The_Token()
    {
        var chat = await WithUnitOfWorkAsync(() => ModelService.CreateAsync(NewModel("gpt-5-mini", AISetupConsts.ChatModelType)));
        var embeddingInput = NewModel("text-embedding-3-large", AISetupConsts.EmbeddingModelType);
        embeddingInput.ApiVersion = "2024-06-01";
        var embedding = await WithUnitOfWorkAsync(() => ModelService.CreateAsync(embeddingInput));

        await WithUnitOfWorkAsync(() => OperationService.UpdateAsync(AISetupConsts.AIReportingChatOperation, NewOperation(chat.Id)));
        await WithUnitOfWorkAsync(() => OperationService.UpdateAsync(AISetupConsts.AIReportingEmbeddingOperation, NewOperation(embedding.Id)));

        var models = await WithUnitOfWorkAsync(() => Resolver.ResolveAsync());
        models.ChatDeployment.ShouldBe("gpt-5-mini");
        models.ChatApiVersion.ShouldBe("2024-10-21");
        models.EmbeddingDeployment.ShouldBe("text-embedding-3-large");
        models.EmbeddingApiVersion.ShouldBe("2024-06-01");
        models.IsComplete.ShouldBeTrue();

        var list = await WithUnitOfWorkAsync(() => ModelService.GetListAsync());
        list.Items.Single(model => model.Id == chat.Id).UsedByOperations.ShouldContain(AISetupConsts.AIReportingChatOperation);
        list.Items.Single(model => model.Id == embedding.Id).UsedByOperations.ShouldContain(AISetupConsts.AIReportingEmbeddingOperation);
    }

    [Fact]
    public async Task Should_Resolve_Nothing_When_AI_Reporting_Has_No_Models()
    {
        var models = await WithUnitOfWorkAsync(() => Resolver.ResolveAsync());

        models.ChatDeployment.ShouldBeNull();
        models.EmbeddingDeployment.ShouldBeNull();
        models.IsComplete.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Reject_Chat_Model_For_Embedding_Row()
    {
        var chat = await WithUnitOfWorkAsync(() => ModelService.CreateAsync(NewModel("gpt-5-mini", AISetupConsts.ChatModelType)));

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            WithUnitOfWorkAsync(() => OperationService.UpdateAsync(AISetupConsts.AIReportingEmbeddingOperation, NewOperation(chat.Id))));
        exception.Code.ShouldBe(AIErrorCodes.SetupModelNotEmbedding);
    }

    [Fact]
    public async Task Should_Reject_Embedding_Model_For_AI_Reporting_Chat_Row()
    {
        var embedding = await WithUnitOfWorkAsync(() => ModelService.CreateAsync(
            NewModel("text-embedding-3-large", AISetupConsts.EmbeddingModelType)));

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            WithUnitOfWorkAsync(() => OperationService.UpdateAsync(AISetupConsts.AIReportingChatOperation, NewOperation(embedding.Id))));
        exception.Code.ShouldBe(AIErrorCodes.SetupModelNotChat);
    }

    [Fact]
    public async Task Should_Block_Deleting_The_Embedding_Model_AI_Reporting_Uses()
    {
        var embedding = await WithUnitOfWorkAsync(() => ModelService.CreateAsync(
            NewModel("text-embedding-3-large", AISetupConsts.EmbeddingModelType)));
        await WithUnitOfWorkAsync(() => OperationService.UpdateAsync(AISetupConsts.AIReportingEmbeddingOperation, NewOperation(embedding.Id)));

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            WithUnitOfWorkAsync(() => ModelService.DeleteAsync(embedding.Id)));
        exception.Code.ShouldBe(AIErrorCodes.SetupModelInUse);
    }

    private static CreateUpdateAIModelDto NewModel(string name, string type)
    {
        return new CreateUpdateAIModelDto
        {
            Name = name,
            Provider = AISetupConsts.OpenAIProvider,
            Type = type,
            ApiVersion = "2024-10-21",
            IsActive = true
        };
    }

    private static UpdateAIOperationDto NewOperation(System.Guid modelId)
    {
        return new UpdateAIOperationDto
        {
            AIModelId = modelId,
            ExecutionMode = nameof(AIExecutionMode.Sequential),
            IsActive = true
        };
    }
}
