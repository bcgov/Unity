using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Features;
using Unity.AI.Localization;
using Unity.AI.Operations;
using Unity.AI.Permissions;
using Unity.AI.Settings;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Attachments;

[Authorize(AIPermissions.Analysis.GenerateAttachmentSummaries)]
[ExposeServices(typeof(AttachmentSummaryAppService), typeof(IAttachmentSummaryAppService))]
public class AttachmentSummaryAppService(
    IAttachmentSummaryService attachmentSummaryService,
    AIFeatureGuard featureGuard) : AIAppService, IAttachmentSummaryAppService
{
    public virtual async Task<AttachmentSummaryResultDto> GenerateAttachmentSummaryAsync(System.Guid attachmentId, string? promptVersion = null)
    {
        await featureGuard.EnsureEnabledAsync(
            AIFeatures.AttachmentSummaries,
            AILocalizationKeys.AttachmentSummariesDisabled);

        await attachmentSummaryService.GenerateAndSaveAsync(attachmentId, promptVersion);
        return new AttachmentSummaryResultDto { Completed = true };
    }

    public virtual async Task<List<AttachmentSummaryResultDto>> GenerateAttachmentSummariesAsync(List<System.Guid> attachmentIds, string? promptVersion = null)
    {
        await featureGuard.EnsureEnabledAsync(
            AIFeatures.AttachmentSummaries,
            AILocalizationKeys.AttachmentSummariesDisabled);

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

    // Internal-only: no HTTP endpoint, no auth check — safe for background job callers
    [AllowAnonymous]
    [RemoteService(IsEnabled = false)]
    public virtual async Task<List<AttachmentSummaryResultDto>> GenerateAttachmentSummariesForPipelineAsync(List<System.Guid> attachmentIds, string? promptVersion = null)
    {
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
