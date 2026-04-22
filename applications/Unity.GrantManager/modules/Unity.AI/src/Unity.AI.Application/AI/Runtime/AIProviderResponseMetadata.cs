namespace Unity.AI.Runtime
{
    public sealed record AIProviderResponseMetadata(
        string? Model,
        string? FinishReason,
        int? PromptTokens,
        int? CompletionTokens,
        int? TotalTokens,
        int? ReasoningTokens);
}
