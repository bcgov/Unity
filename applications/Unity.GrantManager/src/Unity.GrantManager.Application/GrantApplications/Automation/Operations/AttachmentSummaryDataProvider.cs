using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI.Localization;
using Unity.AI.Operations;
using Unity.GrantManager.Applications;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Uow;
using Volo.Abp;

namespace Unity.GrantManager.GrantApplications.Automation;

public class AttachmentSummaryDataProvider(
    IApplicationChefsFileAttachmentRepository applicationChefsFileAttachmentRepository,
    IUnitOfWorkManager unitOfWorkManager,
    IStringLocalizer<AIResource> localizer) : IAttachmentSummaryDataProvider, ITransientDependency
{
    public async Task<AttachmentSummarySource?> GetAttachmentAsync(Guid attachmentId)
    {
        using var uow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
        var attachment = await applicationChefsFileAttachmentRepository.FindAsync(attachmentId);
        await uow.CompleteAsync();

        return attachment == null
            ? null
            : new AttachmentSummarySource(
                attachment.Id,
                attachment.FileName,
                attachment.ChefsSubmissionId,
                attachment.ChefsFileId);
    }

    public async Task UpdateAttachmentSummaryAsync(Guid attachmentId, string summary)
    {
        using var uow = unitOfWorkManager.Begin(requiresNew: true);
        var attachment = await applicationChefsFileAttachmentRepository.FindAsync(attachmentId)
            ?? throw new UserFriendlyException(localizer[AILocalizationKeys.AttachmentNotFound]);
        attachment.AISummary = summary;
        await applicationChefsFileAttachmentRepository.UpdateAsync(attachment);
        await uow.CompleteAsync();
    }

    public async Task<List<Guid>> GetApplicationAttachmentIdsAsync(Guid applicationId)
    {
        using var uow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
        var ids = (await applicationChefsFileAttachmentRepository.GetListAsync(a => a.ApplicationId == applicationId))
            .Select(a => a.Id)
            .ToList();
        await uow.CompleteAsync();
        return ids;
    }
}
