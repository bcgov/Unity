using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.Notifications.Emails;

public interface IEmailLogAttachmentRepository : IBasicRepository<EmailLogAttachment, Guid>
{
    Task<List<EmailLogAttachment>> GetByEmailLogIdAsync(Guid emailLogId);
}
