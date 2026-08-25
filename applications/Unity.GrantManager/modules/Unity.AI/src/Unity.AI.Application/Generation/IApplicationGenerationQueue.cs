using System;
using System.Threading.Tasks;

namespace Unity.AI.Generation;

public interface IApplicationGenerationQueue
{
    Task QueueAsync(string operationType, AIGenerationSubmissionDto request, Guid? tenantId);

    Task QueueApplicationIntakeAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null);
}
