using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Automation;
using Unity.AI.Permissions;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Features;

namespace Unity.GrantManager.Attachments;

[Authorize(AIPermissions.Analysis.GenerateAttachmentSummaries)]
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(AttachmentSummaryAppService), typeof(IAttachmentSummaryAppService))]
public class AttachmentSummaryAppService(
    IApplicationAIGenerationQueue aiGenerationQueue,
    IFeatureChecker featureChecker) : AIAppService, IAttachmentSummaryAppService
{
    private const string SummaryGenerationQueuedMessage = "AI summary generation queued.";

    public async Task<string> GenerateAttachmentSummaryAsync(Guid attachmentId, string? promptVersion = null)
    {
        if (!await featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries"))
        {
            throw new UserFriendlyException("AI attachment summaries are not enabled.");
        }

        await aiGenerationQueue.QueueAttachmentSummariesAsync([attachmentId], CurrentTenant.Id, promptVersion);

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

        await aiGenerationQueue.QueueAttachmentSummariesAsync(attachmentIds, CurrentTenant.Id, promptVersion);

        return attachmentIds.Select(_ => SummaryGenerationQueuedMessage).ToList();
    }
}
