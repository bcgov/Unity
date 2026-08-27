using System;
using System.Threading.Tasks;

namespace Unity.Notifications.Emails;

public interface IEmailLogAttachmentUploadService
{
    Task<EmailLogAttachmentDto> UploadAsync(Guid? emailLogId, Guid? templateId, Guid? tenantId, string fileName, byte[] content, string contentType);
    Task<long> GetTotalFileSizeByEmailLogIdAsync(Guid? emailLogId, Guid? templateId);
}
