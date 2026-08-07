namespace Unity.AI.Generation;

public static class AIGenerationOperationKeyHelper
{
    public const string AttachmentSummaryOperationType = AIGenerationOperations.AttachmentSummary;
    public const string ApplicationAnalysisOperationType = AIGenerationOperations.ApplicationAnalysis;
    public const string ApplicationScoringOperationType = AIGenerationOperations.ApplicationScoring;
    public const string FormMappingOperationType = AIGenerationOperations.FormMapping;
    public const string FormWorksheetOperationType = AIGenerationOperations.FormWorksheet;
    public const string FormScoresheetOperationType = AIGenerationOperations.FormScoresheet;

    public static string? ResolveOperationName(string operationType)
        => AIGenerationOperations.TryGet(operationType, out var definition)
            ? definition!.OperationName
            : null;
}
