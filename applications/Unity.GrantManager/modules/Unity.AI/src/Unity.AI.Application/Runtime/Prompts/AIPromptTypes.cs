namespace Unity.AI.Runtime.Prompts;

public static class AIPromptTypes
{
    public const string AttachmentSummary = "AttachmentSummary";
    public const string ApplicationAnalysis = "ApplicationAnalysis";
    public const string ApplicationScoring = "ApplicationScoring";
    public const string FormMapping = "FormMapping";
    public const string FormWorksheet = "FormWorksheet";
    public const string FormScoresheet = "FormScoresheet";

    /// <summary>The built-in AI operations, in the order Unity AI Setup lists them.</summary>
    public static readonly string[] All =
    [
        ApplicationAnalysis,
        AttachmentSummary,
        ApplicationScoring,
        FormMapping,
        FormWorksheet,
        FormScoresheet
    ];
}
