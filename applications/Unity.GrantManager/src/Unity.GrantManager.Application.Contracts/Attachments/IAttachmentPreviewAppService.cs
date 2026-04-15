using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Attachments;

public interface IAttachmentPreviewAppService : IApplicationService
{
    Task<BlobDto> GetOrCreatePreviewPdfAsync(AttachmentType attachmentType, Guid ownerId, string fileName);
    Task<BlobDto> GetOrCreateChefsPreviewPdfAsync(Guid formSubmissionId, Guid chefsFileId, string fileName, byte[] originalContent);
}
