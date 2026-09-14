using System;
using Shouldly;
using Unity.GrantManager.Events;
using Xunit;
using GrantApplication = Unity.GrantManager.Applications.Application;

namespace Unity.GrantManager.Events;

public class DateBasedScheduledNotificationJobTests
{
    [Fact]
    public void MatchesStatusFilter_WhenNoStatusesAreSelected_ReturnsTrue()
    {
        var application = CreateApplication(Guid.NewGuid());

        DateBasedScheduledNotificationJob.MatchesStatusFilter(application, null).ShouldBeTrue();
        DateBasedScheduledNotificationJob.MatchesStatusFilter(application, string.Empty).ShouldBeTrue();
        DateBasedScheduledNotificationJob.MatchesStatusFilter(application, "  ").ShouldBeTrue();
    }

    [Fact]
    public void MatchesStatusFilter_WhenApplicationStatusIsSelected_ReturnsTrue()
    {
        var selectedStatusId = Guid.NewGuid();
        var application = CreateApplication(selectedStatusId);

        DateBasedScheduledNotificationJob.MatchesStatusFilter(
            application,
            $"{Guid.NewGuid()}, {selectedStatusId}").ShouldBeTrue();
    }

    [Fact]
    public void MatchesStatusFilter_WhenApplicationStatusIsNotSelected_ReturnsFalse()
    {
        var application = CreateApplication(Guid.NewGuid());

        DateBasedScheduledNotificationJob.MatchesStatusFilter(
            application,
            Guid.NewGuid().ToString()).ShouldBeFalse();
    }

    [Fact]
    public void MatchesStatusFilter_WhenFilterContainsOnlyInvalidValues_ReturnsTrue()
    {
        var application = CreateApplication(Guid.NewGuid());

        DateBasedScheduledNotificationJob.MatchesStatusFilter(application, "not-a-guid").ShouldBeTrue();
    }

    [Fact]
    public void MatchesDateField_OnlyChecksTheConfiguredDateField()
    {
        var today = new DateTime(2026, 9, 10);
        var application = CreateApplication(Guid.NewGuid());
        application.DueDate = today.AddDays(-1);
        application.ProjectEndDate = today.AddDays(30);

        DateBasedScheduledNotificationJob.MatchesDateField(application, "DueDate", today).ShouldBeTrue();
        DateBasedScheduledNotificationJob.MatchesDateField(application, "ProjectEndDate", today).ShouldBeFalse();
    }

    [Theory]
    [InlineData("NotificationDate")]
    [InlineData("DueDate")]
    [InlineData("ProjectStartDate")]
    [InlineData("ProjectEndDate")]
    [InlineData("ContractExecutionDate")]
    public void MatchesDateField_WhenConfiguredDateIsTodayOrPast_ReturnsTrue(string dateField)
    {
        var today = new DateTime(2026, 9, 10);
        var application = CreateApplication(Guid.NewGuid());

        SetDateField(application, dateField, today.AddDays(-1));
        DateBasedScheduledNotificationJob.MatchesDateField(application, dateField, today).ShouldBeTrue();

        SetDateField(application, dateField, today);
        DateBasedScheduledNotificationJob.MatchesDateField(application, dateField, today).ShouldBeTrue();
    }

    [Theory]
    [InlineData("NotificationDate")]
    [InlineData("DueDate")]
    [InlineData("ProjectStartDate")]
    [InlineData("ProjectEndDate")]
    [InlineData("ContractExecutionDate")]
    public void MatchesDateField_WhenConfiguredDateIsFutureOrMissing_ReturnsFalse(string dateField)
    {
        var today = new DateTime(2026, 9, 10);
        var application = CreateApplication(Guid.NewGuid());

        SetDateField(application, dateField, today.AddDays(1));
        DateBasedScheduledNotificationJob.MatchesDateField(application, dateField, today).ShouldBeFalse();

        SetDateField(application, dateField, null);
        DateBasedScheduledNotificationJob.MatchesDateField(application, dateField, today).ShouldBeFalse();
    }

    [Fact]
    public void MatchesDateField_WhenDateFieldIsUnknown_ReturnsFalse()
    {
        var application = CreateApplication(Guid.NewGuid());
        application.DueDate = new DateTime(2026, 9, 1);

        DateBasedScheduledNotificationJob.MatchesDateField(
            application,
            "UnknownDateField",
            new DateTime(2026, 9, 10)).ShouldBeFalse();
    }

    private static GrantApplication CreateApplication(Guid statusId)
    {
        return new GrantApplication
        {
            ApplicationStatusId = statusId
        };
    }

    private static void SetDateField(GrantApplication application, string dateField, DateTime? value)
    {
        switch (dateField)
        {
            case "NotificationDate":
                application.NotificationDate = value;
                break;
            case "DueDate":
                application.DueDate = value;
                break;
            case "ProjectStartDate":
                application.ProjectStartDate = value;
                break;
            case "ProjectEndDate":
                application.ProjectEndDate = value;
                break;
            case "ContractExecutionDate":
                application.ContractExecutionDate = value;
                break;
        }
    }
}