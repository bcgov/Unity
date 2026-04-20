using System;
using System.Threading.Tasks;

namespace Unity.AI.Automation;

public interface IApplicationAIGenerationQueue
{
    Task QueueApplicationPipelineAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null);
}
