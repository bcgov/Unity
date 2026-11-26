using Azure.AI.OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using System.ClientModel;
using System.ClientModel.Primitives;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Runtime.Execution;

public class OpenAIChatClientFactory : ITransientDependency
{
    public ChatClient Create(OpenAIOperationSettings settings)
    {
        return CreateClient(settings).GetChatClient(settings.DeploymentName);
    }

    public EmbeddingClient CreateEmbeddingClient(OpenAIOperationSettings settings)
    {
        return CreateClient(settings).GetEmbeddingClient(settings.DeploymentName);
    }

    private static AzureOpenAIClient CreateClient(OpenAIOperationSettings settings)
    {
        // The model's API version from AI Setup is sent on every request by the policy below,
        // so any version Azure publishes works, not only the ones this SDK knows.
        if (!AzureOpenAIApiVersions.IsValid(settings.ApiVersion))
        {
            throw new System.InvalidOperationException(
                $"Azure OpenAI API version '{settings.ApiVersion}' is not valid. Set it on the model in AI Setup.");
        }

        var options = new AzureOpenAIClientOptions();
        options.AddPolicy(new AzureOpenAIApiVersionPolicy(settings.ApiVersion.Trim()), PipelinePosition.PerCall);

        return new AzureOpenAIClient(
            settings.Endpoint,
            new ApiKeyCredential(settings.ApiKey),
            options);
    }
}
