using System;
using System.Threading.Tasks;

namespace Unity.AI.Automation;

public interface IApplicationAIGenerationQueue
{
    Task QueueAttachmentSummaryAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null);
    Task QueueApplicationAnalysisAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null);
    Task QueueApplicationScoringAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null);
    Task QueueAllAIStagesAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null);
}
