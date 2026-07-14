using System;

namespace Unity.GrantManager.GrantApplications;

public static class AIGenerationRequestKeyHelper
{
    public const string AttachmentSummaryOperationType = "attachment-summary";
    public const string ApplicationAnalysisOperationType = "application-analysis";
    public const string ApplicationScoringOperationType = "application-scoring";
    public const string PipelineOperationType = "pipeline";
    public const string FormMappingOperationType = "form-mapping";

    public static string BuildRequestKey(Guid? tenantId, Guid applicationId, string operationType)
    {
        var normalizedTenantId = tenantId?.ToString("D") ?? "host";

        return string.Join(
            ':',
            normalizedTenantId,
            applicationId.ToString("D"),
            operationType.Trim().ToLowerInvariant());
    }

    /// <summary>
    /// Maps an operation type key (e.g. "application-analysis") to the canonical operation name
    /// stored in the AIOperations table (e.g. "ApplicationAnalysis").
    /// </summary>
    public static string? ResolveOperationName(string operationType)
    {
        return operationType switch
        {
            ApplicationAnalysisOperationType => "ApplicationAnalysis",
            AttachmentSummaryOperationType => "AttachmentSummary",
            ApplicationScoringOperationType => "ApplicationScoring",
            FormMappingOperationType => "FormMapping",
            PipelineOperationType => "Default",
            _ => null
        };
    }
}
