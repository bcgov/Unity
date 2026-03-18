namespace Unity.GrantManager.AI
{
    internal sealed record AIProviderResponse(
        string Content,
        string RawResponse = "",
        string? Model = null,
        string? FinishReason = null,
        int? PromptTokens = null,
        int? CompletionTokens = null,
        int? TotalTokens = null,
        int? ReasoningTokens = null)
    {
        public static AIProviderResponse Empty { get; } = new(string.Empty);

        public string CaptureOutput => string.IsNullOrWhiteSpace(RawResponse) ? Content : RawResponse;
    }
}
