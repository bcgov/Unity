using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Features;
using Unity.AI.Localization;
using Unity.AI.Permissions;

namespace Unity.AI.Generation;

public sealed record AIGenerationOperationDefinition(
    string OperationType,
    string OperationName,
    string FeatureName,
    string DisabledLocalizationKey,
    string GeneratePermission,
    string ViewPermission,
    bool RequiresFormVersion);

public static class AIGenerationOperations
{
    public const string AttachmentSummary = "attachment-summary";
    public const string ApplicationAnalysis = "application-analysis";
    public const string ApplicationScoring = "application-scoring";
    public const string FormMapping = "form-mapping";
    public const string FormWorksheet = "form-worksheet";
    public const string FormScoresheet = "form-scoresheet";

    private static readonly IReadOnlyDictionary<string, AIGenerationOperationDefinition> Definitions =
        new Dictionary<string, AIGenerationOperationDefinition>(StringComparer.Ordinal)
        {
            [AttachmentSummary] = new(
                AttachmentSummary,
                "AttachmentSummary",
                AIFeatures.AttachmentSummaries,
                AILocalizationKeys.AttachmentSummariesDisabled,
                AIPermissions.Analysis.GenerateAttachmentSummaries,
                AIPermissions.Analysis.ViewAttachmentSummary,
                false),
            [ApplicationAnalysis] = new(
                ApplicationAnalysis,
                "ApplicationAnalysis",
                AIFeatures.ApplicationAnalysis,
                AILocalizationKeys.ApplicationAnalysisDisabled,
                AIPermissions.Analysis.GenerateApplicationAnalysis,
                AIPermissions.Analysis.ViewApplicationAnalysis,
                false),
            [ApplicationScoring] = new(
                ApplicationScoring,
                "ApplicationScoring",
                AIFeatures.Scoring,
                AILocalizationKeys.ScoringDisabled,
                AIPermissions.Analysis.GenerateScoring,
                AIPermissions.Analysis.ViewScoringResult,
                false),
            [FormMapping] = new(
                FormMapping,
                "FormMapping",
                AIFeatures.FormMapping,
                AILocalizationKeys.FormMappingDisabled,
                AIPermissions.Analysis.GenerateFormMapping,
                AIPermissions.Analysis.ViewFormMapping,
                true),
            [FormWorksheet] = new(
                FormWorksheet,
                "FormWorksheet",
                AIFeatures.FormWorksheet,
                AILocalizationKeys.FormWorksheetDisabled,
                AIPermissions.Analysis.GenerateFormWorksheet,
                AIPermissions.Analysis.ViewFormWorksheet,
                true),
            [FormScoresheet] = new(
                FormScoresheet,
                "FormScoresheet",
                AIFeatures.FormScoresheet,
                AILocalizationKeys.FormScoresheetDisabled,
                AIPermissions.Analysis.GenerateFormScoresheet,
                AIPermissions.Analysis.ViewFormScoresheet,
                true)
        };

    public static IReadOnlyCollection<AIGenerationOperationDefinition> All => Definitions.Values.ToArray();

    public static AIGenerationOperationDefinition Get(string operationType)
    {
        if (!Definitions.TryGetValue(operationType, out var definition))
        {
            throw new ArgumentException($"Unsupported AI generation operation type: {operationType}", nameof(operationType));
        }

        return definition;
    }

    public static bool TryGet(string? operationType, out AIGenerationOperationDefinition? definition)
    {
        if (operationType is not null && Definitions.TryGetValue(operationType, out var resolved))
        {
            definition = resolved;
            return true;
        }

        definition = null;
        return false;
    }
}
