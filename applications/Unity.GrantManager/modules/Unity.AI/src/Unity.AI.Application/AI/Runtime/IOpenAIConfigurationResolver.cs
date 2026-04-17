namespace Unity.AI.Runtime;

public interface IOpenAIConfigurationResolver
{
    string ResolveProviderName(string? operationName = null);

    string ResolveApiKey(string? operationName = null);

    string ResolveMaxTokensParameterNameForOperation(string? operationName = null);

    double? ResolveConfiguredTemperature(string? operationName = null);

    int ResolveCompletionTokens(string operationName, int defaultValue);

    string ResolveApiUrl(string? operationName = null);
}
