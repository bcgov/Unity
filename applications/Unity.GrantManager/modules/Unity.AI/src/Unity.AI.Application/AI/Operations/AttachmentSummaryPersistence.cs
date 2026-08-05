using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI.Operations;
using Unity.GrantManager.Applications;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Uow;

namespace Unity.AI.Operations;

public class AttachmentSummaryPersistence(
    IApplicationChefsFileAttachmentRepository applicationChefsFileAttachmentRepository,
    IUnitOfWorkManager unitOfWorkManager) : IAttachmentSummaryPersistence, ITransientDependency
{
    public async Task<AttachmentSummarySource> LoadAsync(Guid attachmentId)
    {
        using var uow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
        var attachment = await applicationChefsFileAttachmentRepository.GetAsync(attachmentId);
        var source = new AttachmentSummarySource(
            attachment.Id,
            attachment.FileName,
            attachment.ChefsSubmissionId,
            attachment.ChefsFileId);
        await uow.CompleteAsync();
        return source;
    }

    public async Task SaveSummaryAsync(Guid attachmentId, string summary)
    {
        using var uow = unitOfWorkManager.Begin(requiresNew: true);
        var attachment = await applicationChefsFileAttachmentRepository.GetAsync(attachmentId);
        attachment.AISummary = summary;
        await applicationChefsFileAttachmentRepository.UpdateAsync(attachment);
        await uow.CompleteAsync();
    }

    public async Task<List<Guid>> LoadApplicationAttachmentIdsAsync(Guid applicationId)
    {
        using var uow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
        var ids = (await applicationChefsFileAttachmentRepository.GetListAsync(a => a.ApplicationId == applicationId))
            .Select(a => a.Id)
            .ToList();
        await uow.CompleteAsync();
        return ids;
    }
}
