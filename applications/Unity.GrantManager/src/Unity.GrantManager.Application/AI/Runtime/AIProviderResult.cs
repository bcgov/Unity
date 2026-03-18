namespace Unity.GrantManager.AI
{
    internal sealed record AIProviderResult(
        string Content,
        string RawResponse = "",
        string? Model = null,
        string? FinishReason = null,
        int? PromptTokens = null,
        int? CompletionTokens = null,
        int? TotalTokens = null,
        int? ReasoningTokens = null)
    {
        public static AIProviderResult Empty { get; } = new(string.Empty);

        public string CaptureOutput => string.IsNullOrWhiteSpace(RawResponse) ? Content : RawResponse;
    }
}
