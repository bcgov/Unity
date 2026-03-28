using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI.Permissions;
using Unity.GrantManager.AI.BackgroundJobs;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Features;

namespace Unity.GrantManager.Attachments;

[Authorize(AIPermissions.AttachmentSummary.AttachmentSummaryDefault)]
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(AttachmentSummaryAppService), typeof(IAttachmentSummaryAppService))]
public class AttachmentSummaryAppService(
    IBackgroundJobManager backgroundJobManager,
    IFeatureChecker featureChecker) : ApplicationService, IAttachmentSummaryAppService
{
    private const string SummaryGenerationQueuedMessage = "AI summary generation queued.";

    public async Task<string> GenerateAttachmentSummaryAsync(Guid attachmentId, string? promptVersion = null)
    {
        if (!await featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries"))
        {
            throw new UserFriendlyException("AI attachment summaries are not enabled.");
        }

        await backgroundJobManager.EnqueueAsync(new GenerateAttachmentSummaryBackgroundJobArgs
        {
            AttachmentIds = [attachmentId],
            PromptVersion = promptVersion,
            TenantId = CurrentTenant.Id
        });

        return SummaryGenerationQueuedMessage;
    }

    public async Task<List<string>> GenerateAttachmentSummariesAsync(List<Guid> attachmentIds, string? promptVersion = null)
    {
        if (!await featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries"))
        {
            throw new UserFriendlyException("AI attachment summaries are not enabled.");
        }

        if (attachmentIds.Count == 0)
        {
            return [];
        }

        await backgroundJobManager.EnqueueAsync(new GenerateAttachmentSummaryBackgroundJobArgs
        {
            AttachmentIds = attachmentIds,
            PromptVersion = promptVersion,
            TenantId = CurrentTenant.Id
        });

        return attachmentIds.Select(_ => SummaryGenerationQueuedMessage).ToList();
    }
}
