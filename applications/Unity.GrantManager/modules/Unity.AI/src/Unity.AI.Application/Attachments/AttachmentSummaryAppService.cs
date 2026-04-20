using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Operations;
using Unity.AI.Permissions;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Features;

namespace Unity.GrantManager.Attachments;

[Authorize(AIPermissions.Analysis.GenerateAttachmentSummaries)]
[ExposeServices(typeof(AttachmentSummaryAppService), typeof(IAttachmentSummaryAppService))]
public class AttachmentSummaryAppService(
    IAttachmentSummaryService attachmentSummaryService,
    IFeatureChecker featureChecker) : AIAppService, IAttachmentSummaryAppService
{
    public async Task<AttachmentSummaryResultDto> GenerateAttachmentSummaryAsync(System.Guid attachmentId, string? promptVersion = null)
    {
        if (!await featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries"))
        {
            throw new UserFriendlyException("AI attachment summaries are not enabled.");
        }

        await attachmentSummaryService.GenerateAndSaveAsync(attachmentId, promptVersion);
        return new AttachmentSummaryResultDto { Completed = true };
    }

    public async Task<List<AttachmentSummaryResultDto>> GenerateAttachmentSummariesAsync(List<System.Guid> attachmentIds, string? promptVersion = null)
    {
        if (!await featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries"))
        {
            throw new UserFriendlyException("AI attachment summaries are not enabled.");
        }

        if (attachmentIds.Count == 0)
        {
            return [];
        }

        var results = new List<AttachmentSummaryResultDto>();
        foreach (var attachmentId in attachmentIds)
        {
            await attachmentSummaryService.GenerateAndSaveAsync(attachmentId, promptVersion);
            results.Add(new AttachmentSummaryResultDto { Completed = true });
        }

        return results;
    }
}
