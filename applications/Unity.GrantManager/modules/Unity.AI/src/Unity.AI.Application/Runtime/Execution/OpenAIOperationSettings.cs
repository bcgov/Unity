using System;

namespace Unity.AI.Runtime.Execution;

public sealed record OpenAIOperationSettings(
    string ProviderName,
    string ProfileName,
    string ApiKey,
    Uri Endpoint,
    string DeploymentName,
    bool MaxOutputTokenCountSupported,
    double? Temperature,
    int CompletionTokens,
    string PromptVersion);
