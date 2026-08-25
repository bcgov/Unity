using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.GrantManager.GrantApplications
{
    public interface IBulkEmailNotificationAppService
    {
        Task<BulkEmailNotificationResultDto> SendBulkEmailNotifications(List<BulkEmailNotificationDto> batchApplicationsToEmail);
        Task<List<BulkEmailNotificationDto>> GetApplicationsForBulkEmail(Guid[] applicationGuids);
        Task<BulkEmailNotificationDto> RevalidateApplicationForBulkEmail(Guid applicationId);
    }
}
