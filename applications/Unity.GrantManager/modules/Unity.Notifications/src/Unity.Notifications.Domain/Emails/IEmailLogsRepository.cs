using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.Notifications.Emails
{
    public interface IEmailLogsRepository : IRepository<EmailLog, Guid>
    {
        Task<EmailLog?> GetByIdAsync(Guid id, bool includeDetails = false);
        Task<List<EmailLog>> GetByApplicationIdAsync(Guid applicationId);
        Task<List<EmailLog>> GetByApplicationIdsAndStatusAsync(List<Guid> applicationIds, string status);

        // Bypasses ABP's automatic audit-stamping so system/background saves don't overwrite the last user edit.
        // Keyed by concurrencyStamp so a newer user edit committed in between wins instead of being silently overwritten.
        Task RestoreAuditStampsAsync(Guid id, Guid? modifierId, DateTime? modificationTime, string concurrencyStamp);
    }
}
