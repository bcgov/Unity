using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.AI.Automation;

public interface IApplicationAIGenerationQueue
{
    Task QueueAttachmentSummariesAsync(IReadOnlyList<Guid> attachmentIds, Guid? tenantId, string? promptVersion = null);
    Task QueueApplicationAnalysisAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null);
    Task QueueApplicationScoringAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null);
    Task QueueApplicationPipelineAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null);
}
