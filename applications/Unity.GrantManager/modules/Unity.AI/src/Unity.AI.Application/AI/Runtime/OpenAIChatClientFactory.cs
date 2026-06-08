using Azure.AI.OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Runtime;

public class OpenAIChatClientFactory : ITransientDependency
{
    public ChatClient Create(OpenAIOperationSettings settings)
    {
        return new AzureOpenAIClient(
            settings.Endpoint,
            new ApiKeyCredential(settings.ApiKey),
            new AzureOpenAIClientOptions())
            .GetChatClient(settings.DeploymentName);
    }
}
