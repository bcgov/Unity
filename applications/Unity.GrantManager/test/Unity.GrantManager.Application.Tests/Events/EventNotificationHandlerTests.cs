using System;
using System.Collections.Generic;
using System.Linq;
using Shouldly;
using Unity.GrantManager.Notifications;
using Unity.Payments.Enums;
using Xunit;

namespace Unity.GrantManager.Events;

public class EventNotificationHandlerTests
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
            .Where(EventNotificationHandler.ApplicationEventNotificationFilter(formId, statusId))
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
            .Where(EventNotificationHandler.ApplicationEventNotificationFilter(formId, statusId))
            .ShouldBeEmpty();
    }

    [Theory]
    [InlineData(PaymentRequestStatus.L1Pending)]
    [InlineData(PaymentRequestStatus.L1Declined)]
    [InlineData(PaymentRequestStatus.L2Pending)]
    [InlineData(PaymentRequestStatus.L2Declined)]
    [InlineData(PaymentRequestStatus.L3Pending)]
    [InlineData(PaymentRequestStatus.L3Declined)]
    [InlineData(PaymentRequestStatus.Submitted)]
    [InlineData(PaymentRequestStatus.Validated)]
    [InlineData(PaymentRequestStatus.NotValidated)]
    [InlineData(PaymentRequestStatus.Paid)]
    [InlineData(PaymentRequestStatus.Failed)]
    [InlineData(PaymentRequestStatus.FSB)]
    [InlineData(PaymentRequestStatus.HistoricalPayment)]
    [InlineData(PaymentRequestStatus.Cancelled)]
    public void PaymentEventNotificationFilter_SelectsMatchingStatus(PaymentRequestStatus paymentStatus)
    {
        var formId = Guid.NewGuid();
        var notifications = new List<ScheduledNotification>
        {
            CreatePaymentNotification(formId, paymentStatus, isActive: true),
            CreatePaymentNotification(formId, paymentStatus == PaymentRequestStatus.Paid
                ? PaymentRequestStatus.Failed
                : PaymentRequestStatus.Paid, isActive: true),
            CreatePaymentNotification(formId, paymentStatus, isActive: false),
            CreateNotification(formId, Guid.NewGuid(), isActive: true, module: "Application"),
            CreatePaymentNotification(Guid.NewGuid(), paymentStatus, isActive: true)
        };

        var matches = notifications
            .AsQueryable()
            .Where(EventNotificationHandler.PaymentEventNotificationFilter(formId, paymentStatus, null))
            .ToList();

        matches.Count.ShouldBe(1);
        matches[0].EventType.ShouldBe(paymentStatus.ToString());
    }

    [Fact]
    public void PaymentEventNotificationFilter_RequiresPaymentEventModule()
    {
        var formId = Guid.NewGuid();
        var notification = CreatePaymentNotification(formId, PaymentRequestStatus.Paid, isActive: true);
        notification.Module = null;

        new[] { notification }
            .AsQueryable()
            .Where(EventNotificationHandler.PaymentEventNotificationFilter(formId, PaymentRequestStatus.Paid, null))
            .ShouldBeEmpty();
    }

    [Fact]
    public void PaymentEventNotificationFilter_SelectsMatchingCasPaymentStatus()
    {
        var formId = Guid.NewGuid();
        var notification = new ScheduledNotification
        {
            FormId = formId,
            TriggerType = "Event",
            IsActive = true,
            Module = "Payment",
            EventType = "Validated"
        };

        new[] { notification }
            .AsQueryable()
            .Where(EventNotificationHandler.PaymentEventNotificationFilter(
                formId,
                PaymentRequestStatus.Submitted,
                "Validated"))
            .ShouldHaveSingleItem();
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

    private static ScheduledNotification CreatePaymentNotification(
        Guid formId,
        PaymentRequestStatus paymentStatus,
        bool isActive)
    {
        return new ScheduledNotification
        {
            FormId = formId,
            TriggerType = "Event",
            IsActive = isActive,
            Module = "Payment",
            EventType = paymentStatus.ToString()
        };
    }
}