using System;
using System.Collections.Generic;
using System.Linq;
using Shouldly;
using Unity.GrantManager.Notifications;
using Xunit;

namespace Unity.GrantManager.Events;

public class ScheduledNotificationEventHandlerTests
{
    [Fact]
    public void ApplicationEventNotificationFilter_SelectsOnlyMatchingActiveApplicationPlans()
    {
        var formId = Guid.NewGuid();
        var statusId = Guid.NewGuid();
        var notifications = new List<ScheduledNotification>
        {
            CreateNotification(formId, statusId, isActive: true, module: "Application"),
            CreateNotification(formId, statusId, isActive: true, module: null),
            CreateNotification(formId, Guid.NewGuid(), isActive: true, module: "Application"),
            CreateNotification(formId, statusId, isActive: false, module: "Application"),
            CreateNotification(formId, statusId, isActive: true, module: "Payment"),
            CreateNotification(Guid.NewGuid(), statusId, isActive: true, module: "Application")
        };

        var matches = notifications
            .AsQueryable()
            .Where(ScheduledNotificationEventHandler.ApplicationEventNotificationFilter(formId, statusId))
            .ToList();

        matches.Count.ShouldBe(2);
    }

    [Fact]
    public void ApplicationEventNotificationFilter_RequiresEventTrigger()
    {
        var formId = Guid.NewGuid();
        var statusId = Guid.NewGuid();
        var dateNotification = CreateNotification(formId, statusId, isActive: true, module: null);
        dateNotification.TriggerType = "Date";

        new[] { dateNotification }
            .AsQueryable()
            .Where(ScheduledNotificationEventHandler.ApplicationEventNotificationFilter(formId, statusId))
            .ShouldBeEmpty();
    }

    private static ScheduledNotification CreateNotification(
        Guid formId,
        Guid statusId,
        bool isActive,
        string? module)
    {
        return new ScheduledNotification
        {
            FormId = formId,
            TriggerType = "Event",
            IsActive = isActive,
            Module = module,
            ApplicationStatusId = statusId
        };
    }
}