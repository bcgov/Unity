using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Notifications
{
    public interface IApplicationScheduledNotificationTrackingRepository : IRepository<ApplicationScheduledNotificationTracking, Guid>
    {
    }
}
