using System;
using System.Threading.Tasks;

namespace Unity.AI.Automation;

public interface IApplicationAIGenerationQueue
{
<<<<<<< HEAD
=======
    Task QueueAttachmentSummariesAsync(Guid applicationId, IReadOnlyList<Guid> attachmentIds, Guid? tenantId, string? promptVersion = null);
    Task QueueApplicationAnalysisAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null);
    Task QueueApplicationScoringAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null);
>>>>>>> 64123200c (AB#32451 consolidate AI queue entrypoints)
    Task QueueApplicationPipelineAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null);
}
