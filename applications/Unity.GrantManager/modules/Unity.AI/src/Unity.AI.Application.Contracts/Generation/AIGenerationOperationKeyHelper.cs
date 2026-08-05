namespace Unity.AI.Generation;

public static class AIGenerationOperationKeyHelper
{
    public const string AttachmentSummaryOperationType = "attachment-summary";
    public const string ApplicationAnalysisOperationType = "application-analysis";
    public const string ApplicationScoringOperationType = "application-scoring";
    public const string FormMappingOperationType = "form-mapping";
    public const string FormWorksheetOperationType = "form-worksheet";
    public const string FormScoresheetOperationType = "form-scoresheet";

    public static string? ResolveOperationName(string operationType)
    {
        return operationType switch
        {
            ApplicationAnalysisOperationType => "ApplicationAnalysis",
            AttachmentSummaryOperationType => "AttachmentSummary",
            ApplicationScoringOperationType => "ApplicationScoring",
            FormMappingOperationType => "FormMapping",
            FormWorksheetOperationType => "FormWorksheet",
            FormScoresheetOperationType => "FormScoresheet",
            _ => null
        };
    }
}
