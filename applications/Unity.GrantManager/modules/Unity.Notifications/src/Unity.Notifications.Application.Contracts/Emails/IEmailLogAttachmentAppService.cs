using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Notifications.Emails;

public interface IEmailLogAttachmentAppService : IApplicationService
{
    Task<List<EmailLogAttachmentDto>> GetListByEmailLogIdAsync(Guid emailLogId);
    Task DeleteAsync(Guid id);
}
