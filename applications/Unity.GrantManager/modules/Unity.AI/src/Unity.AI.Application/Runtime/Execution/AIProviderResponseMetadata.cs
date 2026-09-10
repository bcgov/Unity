namespace Unity.AI.Runtime.Execution
{
    public sealed record AIProviderResponseMetadata(
        string? Model,
        string? FinishReason,
        int? PromptTokens,
        int? CompletionTokens,
        int? TotalTokens,
        int? ReasoningTokens);
}
