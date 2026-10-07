using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shouldly;
using Unity.GrantManager.Web.Controllers;
using Unity.Notifications.EmailNotifications;
using Unity.Notifications.Emails;
using Unity.Notifications.Permissions;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Users;
using Xunit;

namespace Unity.GrantManager.Components;

public class NotificationsControllerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly IEmailNotificationService _emails = Substitute.For<IEmailNotificationService>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly ICurrentTenant _tenant = Substitute.For<ICurrentTenant>();
    private readonly NotificationsController _controller;

    public NotificationsControllerTests()
    {
        _tenant.Id.Returns(_tenantId);
        _user.Id.Returns(Guid.NewGuid());
        _controller = new NotificationsController(_user, _emails, _tenant);
    }

    private EmailLog Email(Guid applicationId) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = _tenantId,
        ApplicationId = applicationId,
        ApplicantId = Guid.NewGuid(),
        Subject = "APP-123 - Applicant - Update",
        FromAddress = "from@example.test",
        ToAddress = "to@example.test",
        CC = "cc@example.test",
        BCC = "bcc@example.test",
        Status = EmailStatus.Sent,
        SentDateTime = new DateTime(2025, 7, 1, 12, 0, 0, DateTimeKind.Utc),
        Body = "<table><tr><td>Updated applicant</td></tr></table>",
        BodyType = "html"
    };

    [Theory]
    [InlineData("html")]
    [InlineData("text")]
    public async Task ShouldOpenApplicantEmailWithoutAnApplication(string bodyType)
    {
        var email = Email(Guid.Empty);
        email.BodyType = bodyType;
        _emails.GetEmailLogById(email.Id).Returns(email);

        var result = (await _controller.EmailModal(Guid.Empty, email.Id)).ShouldBeOfType<ViewResult>();

        result.ViewName.ShouldBe("ApplicantEmailModal");
        var model = result.Model.ShouldBeOfType<NotificationEmailModalViewModel>();
        model.ApplicationId.ShouldBe(Guid.Empty);
        model.ApplicantId.ShouldBe(email.ApplicantId);
        model.TenantId.ShouldBe(_tenantId);
        model.BodyType.ShouldBe(bodyType);
        model.SelectedEmail.ShouldNotBeNull();
        model.SelectedEmail.Id.ShouldBe(email.Id);
        model.SelectedEmail.Subject.ShouldBe(email.Subject);
        model.SelectedEmail.FromAddress.ShouldBe(email.FromAddress);
        model.SelectedEmail.ToAddress.ShouldBe(email.ToAddress);
        model.SelectedEmail.Cc.ShouldBe(email.CC);
        model.SelectedEmail.Bcc.ShouldBe(email.BCC);
        model.SelectedEmail.Status.ShouldBe(email.Status);
        model.SelectedEmail.SentDateTime.ShouldBe(email.SentDateTime);
        model.SelectedEmail.Body.ShouldBe(email.Body);
        await _emails.DidNotReceive().GetHistoryByApplicationId(Arg.Any<Guid>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ShouldKeepApplicationEmailWidgetAndResolveItsStoredApplication(bool supplyApplicationId)
    {
        var email = Email(Guid.NewGuid());
        var history = new EmailHistoryDto { Id = email.Id, Body = email.Body, Subject = email.Subject };
        _emails.GetEmailLogById(email.Id).Returns(email);
        _emails.GetHistoryByApplicationId(email.ApplicationId).Returns(new List<EmailHistoryDto> { history });

        var result = (await _controller.EmailModal(supplyApplicationId ? email.ApplicationId : Guid.Empty, email.Id))
            .ShouldBeOfType<ViewResult>();

        result.ViewName.ShouldBe("EmailModal");
        var model = result.Model.ShouldBeOfType<NotificationEmailModalViewModel>();
        model.ApplicationId.ShouldBe(email.ApplicationId);
        model.CurrentUserId.ShouldBe(_user.Id!.Value);
        model.SelectedEmail.ShouldBeSameAs(history);
        await _emails.Received(1).GetHistoryByApplicationId(email.ApplicationId);
    }

    [Fact]
    public async Task ShouldRejectEmptyEmailId()
    {
        (await _controller.EmailModal(Guid.Empty, Guid.Empty)).ShouldBeOfType<BadRequestResult>();
        await _emails.DidNotReceive().GetEmailLogById(Arg.Any<Guid>());
    }

    [Fact]
    public async Task ShouldReturnNotFoundForMissingEmail()
    {
        var id = Guid.NewGuid();
        _emails.GetEmailLogById(id).Returns((EmailLog?)null);
        (await _controller.EmailModal(Guid.Empty, id)).ShouldBeOfType<NotFoundResult>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ShouldRejectEmailFromAnotherTenant(bool applicantEmail)
    {
        var email = Email(applicantEmail ? Guid.Empty : Guid.NewGuid());
        email.TenantId = Guid.NewGuid();
        _emails.GetEmailLogById(email.Id).Returns(email);

        (await _controller.EmailModal(Guid.Empty, email.Id)).ShouldBeOfType<NotFoundResult>();
        await _emails.DidNotReceive().GetHistoryByApplicationId(Arg.Any<Guid>());
    }

    [Fact]
    public async Task ShouldRejectMismatchedApplicationId()
    {
        var email = Email(Guid.NewGuid());
        _emails.GetEmailLogById(email.Id).Returns(email);
        (await _controller.EmailModal(Guid.NewGuid(), email.Id)).ShouldBeOfType<NotFoundResult>();
        await _emails.DidNotReceive().GetHistoryByApplicationId(Arg.Any<Guid>());
    }

    [Fact]
    public async Task ShouldReturnNotFoundWhenEmailHasNoApplicantOrApplication()
    {
        var email = Email(Guid.Empty);
        email.ApplicantId = Guid.Empty;
        _emails.GetEmailLogById(email.Id).Returns(email);
        (await _controller.EmailModal(Guid.Empty, email.Id)).ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public void ShouldContinueRequiringNotificationListPermission()
    {
        var policy = typeof(NotificationsController).GetCustomAttributes<AuthorizeAttribute>()
            .Select(attribute => attribute.Policy);
        policy.ShouldContain(NotificationsPermissions.NotificationList.View);
    }
}
