namespace Unity.AI.Runtime.Execution;

public sealed record AIRetryMetrics(
    int AttemptCount,
    long DurationMs,
    int PromptTokensTotal,
    int CompletionTokensTotal,
    int TotalTokensTotal,
    int ReasoningTokensTotal);
