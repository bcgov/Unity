using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.AI.Automation;

public interface IApplicationAIGenerationQueue
{
    Task QueueAttachmentSummaryAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null, List<Guid>? attachmentIds = null);
    Task QueueApplicationAnalysisAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null);
    Task QueueApplicationScoringAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null);
    Task QueueFormMappingAsync(Guid applicationId, Guid? tenantId, Guid applicationFormVersionId, string? promptVersion = null);
    Task QueueFormWorksheetAsync(Guid applicationId, Guid? tenantId, Guid applicationFormVersionId, string? promptVersion = null);
    Task QueueFormScoresheetAsync(Guid applicationId, Guid? tenantId, Guid applicationFormVersionId, string? promptVersion = null);
    Task QueueAllAIStagesAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null);
}
