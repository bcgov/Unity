using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Notifications
{
    public interface IScheduledNotificationTrackingRepository : IRepository<ScheduledNotificationTracking, Guid>
    {
    }
}
