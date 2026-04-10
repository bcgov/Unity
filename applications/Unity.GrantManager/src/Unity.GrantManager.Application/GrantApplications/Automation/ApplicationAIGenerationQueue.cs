using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.AI.Automation;
using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.GrantApplications.Automation;

public class ApplicationAIGenerationQueue(IBackgroundJobManager backgroundJobManager)
    : IApplicationAIGenerationQueue, ITransientDependency
{
    public async Task QueueAttachmentSummariesAsync(IReadOnlyList<Guid> attachmentIds, Guid? tenantId, string? promptVersion = null)
    {
        await backgroundJobManager.EnqueueAsync(new GenerateAttachmentSummaryBackgroundJobArgs
        {
            AttachmentIds = [.. attachmentIds],
            PromptVersion = promptVersion,
            TenantId = tenantId
        });
    }

    public async Task QueueApplicationAnalysisAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null)
    {
        await backgroundJobManager.EnqueueAsync(new GenerateApplicationAnalysisBackgroundJobArgs
        {
            ApplicationId = applicationId,
            PromptVersion = promptVersion,
            TenantId = tenantId
        });
    }

    public async Task QueueApplicationScoringAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null)
    {
        await backgroundJobManager.EnqueueAsync(new GenerateApplicationScoringBackgroundJobArgs
        {
            ApplicationId = applicationId,
            PromptVersion = promptVersion,
            TenantId = tenantId
        });
    }

    public async Task QueueApplicationPipelineAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null)
    {
        await backgroundJobManager.EnqueueAsync(new RunApplicationAIPipelineJobArgs
        {
            ApplicationId = applicationId,
            PromptVersion = promptVersion,
            TenantId = tenantId
        });
    }
}
