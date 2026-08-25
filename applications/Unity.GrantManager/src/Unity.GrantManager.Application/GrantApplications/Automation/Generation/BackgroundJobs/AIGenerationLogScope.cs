using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

internal static class AIGenerationLogScope
{
    public static IDisposable? Begin(
        ILogger logger,
        string operationType,
        Guid applicationId,
        Guid? tenantId,
        string? promptVersion,
        Guid? requestedByUserId)
    {
        return logger.BeginScope(new Dictionary<string, object?>
        {
            ["AIOperationType"] = operationType,
            ["AIApplicationId"] = applicationId,
            ["AITenantId"] = tenantId,
            ["AIPromptVersion"] = promptVersion,
            ["AIRequestedByUserId"] = requestedByUserId
        });
    }
}
