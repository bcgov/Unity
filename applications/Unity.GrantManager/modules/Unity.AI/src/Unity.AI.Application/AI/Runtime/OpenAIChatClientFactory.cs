using Azure.AI.OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Runtime;

public class OpenAIChatClientFactory(OpenAIConfigurationResolver configurationResolver) : ITransientDependency
{
    private readonly OpenAIConfigurationResolver _configurationResolver = configurationResolver;

    public ChatClient Create(string? operationName = null)
    {
        return new AzureOpenAIClient(
            _configurationResolver.ResolveEndpoint(operationName),
            new ApiKeyCredential(_configurationResolver.ResolveApiKey(operationName)),
            new AzureOpenAIClientOptions())
            .GetChatClient(_configurationResolver.ResolveDeploymentName(operationName));
    }
}
