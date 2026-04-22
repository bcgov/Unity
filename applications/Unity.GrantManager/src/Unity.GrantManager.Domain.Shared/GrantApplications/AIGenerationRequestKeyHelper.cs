using System;

namespace Unity.GrantManager.GrantApplications;

public static class AIGenerationRequestKeyHelper
{
    public const string AttachmentSummaryOperationType = "attachment-summary";
    public const string ApplicationAnalysisOperationType = "application-analysis";
    public const string ApplicationScoringOperationType = "application-scoring";
    public static string BuildRequestKey(Guid? tenantId, Guid applicationId, string operationType)
    {
        var normalizedTenantId = tenantId?.ToString("D") ?? "host";

        return string.Join(
            ':',
            normalizedTenantId,
            applicationId.ToString("D"),
            operationType.Trim().ToLowerInvariant());
    }
}
