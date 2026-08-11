namespace Unity.AI.Operations;

public sealed record AttachmentSummaryDiagnosticResult(
    string Summary,
    string ModelOutput,
    Unity.AI.Runtime.AIOperationOutcome Outcome,
    Unity.AI.Runtime.AIFailureCategory FailureCategory,
    string EffectivePromptVersion,
    string ProviderName,
    string ProfileName,
    string? Model,
    int? HttpStatusCode,
    string? FinishReason,
    int AttemptCount,
    long DurationMs,
    int PromptTokensTotal,
    int CompletionTokensTotal,
    int TotalTokensTotal,
    int ReasoningTokensTotal)
{
    /// <summary>
    /// SHA-256 of the unrendered system prompt, user template, and prompt metadata.
    /// This deliberately excludes attachment text and other request data.
    /// </summary>
    public string PromptTemplateSha256 { get; init; } = "";
}
