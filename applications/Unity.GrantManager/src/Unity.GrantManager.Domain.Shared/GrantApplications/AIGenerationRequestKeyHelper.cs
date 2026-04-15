using System;

namespace Unity.GrantManager.GrantApplications;

public static class AIGenerationRequestKeyHelper
{
    public const string PipelineOperationType = "pipeline";

    public static string BuildRequestKey(Guid? tenantId, Guid applicationId, string operationType, string? promptVersion = null, Guid? attachmentId = null)
    {
        var normalizedPromptVersion = string.IsNullOrWhiteSpace(promptVersion) ? "default" : promptVersion.Trim();
        var normalizedAttachmentId = attachmentId?.ToString("D") ?? "none";
        var normalizedTenantId = tenantId?.ToString("D") ?? "host";

        return string.Join(
            ':',
            normalizedTenantId,
            applicationId.ToString("D"),
            normalizedAttachmentId,
            operationType.Trim().ToLowerInvariant(),
            normalizedPromptVersion.ToLowerInvariant());
    }
}
