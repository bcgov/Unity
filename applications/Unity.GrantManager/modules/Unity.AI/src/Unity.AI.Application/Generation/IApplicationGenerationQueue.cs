using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.AI.Generation;

public interface IApplicationGenerationQueue
{
    Task QueueApplicationAttachmentSummaryAsync(Guid applicationId, Guid? tenantId, List<Guid> attachmentIds, string? promptVersion = null);

    Task QueueApplicationAnalysisAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null);

    Task QueueApplicationScoringAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null);

    Task QueueFormMappingAsync(Guid applicationId, Guid? tenantId, Guid applicationFormVersionId, string? promptVersion = null);

    Task QueueApplicationIntakeAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null);
}
