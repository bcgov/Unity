using System;
using System.Threading.Tasks;
using Unity.GrantManager.Attachments;

namespace Unity.GrantManager.Intakes;

public interface IChefsAttachmentDownloadService
{
    Task<BlobDto> DownloadAsync(Guid? formSubmissionId, Guid? chefsFileAttachmentId, string name);
}
