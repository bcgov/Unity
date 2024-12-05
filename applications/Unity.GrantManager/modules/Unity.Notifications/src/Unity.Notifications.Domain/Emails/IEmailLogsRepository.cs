using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.Notifications.Emails
{
    public interface IEmailLogsRepository : IBasicRepository<EmailLog, Guid>
    {
        Task<EmailLog?> GetByIdAsync(Guid id, bool includeDetails = false);
        Task<List<EmailLog>> GetByApplicationIdAsync(Guid applicationId);
    }
}
