namespace Unity.AI.Runtime
{
    internal sealed record AIProviderResponseMetadata(
        string? Model,
        string? FinishReason,
        int? PromptTokens,
        int? CompletionTokens,
        int? TotalTokens,
        int? ReasoningTokens);
}
