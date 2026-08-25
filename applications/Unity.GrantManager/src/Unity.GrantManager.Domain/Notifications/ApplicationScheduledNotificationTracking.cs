using System;
using System.Diagnostics.CodeAnalysis;
using Volo.Abp.Domain.Entities;

namespace Unity.GrantManager.Notifications
{
    /// <summary>
    /// Tracks which scheduled notifications have been sent for which applications.
    /// Prevents duplicate notifications from being sent for the same application and notification combination.
    /// </summary>
    public class ScheduledNotificationTracking : Entity<Guid>
    {
        public Guid ApplicationId { get; set; }
        public Guid ScheduledNotificationId { get; set; }
        public required string DateField { get; set; } // "DueDate", "NotificationDate", "ContractNotificationDate"
        public DateTime NotificationSentDate { get; set; }
        public DateTime CreationTime { get; set; }
        public Guid? CreatorId { get; set; }

        protected ScheduledNotificationTracking() { }

        [SetsRequiredMembers]
        public ScheduledNotificationTracking(
            Guid id,
            Guid applicationId,
            Guid scheduledNotificationId,
            string dateField,
            DateTime notificationSentDate) : base(id)
        {
            ApplicationId = applicationId;
            ScheduledNotificationId = scheduledNotificationId;
            DateField = dateField;
            NotificationSentDate = notificationSentDate;
            CreationTime = DateTime.UtcNow;
        }
    }
}
