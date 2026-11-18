using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Unity.AspNetCore.Mvc.UI.Theme.UX2.Renderers;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Assessments;
using Unity.GrantManager.Notifications;
using Unity.GrantManager.Notifications.Email;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;
using Unity.Notifications.EmailAddresses;
using Unity.Notifications.EmailNotifications;
using Unity.Notifications.Emails;
using Unity.Notifications.Events;
using Unity.Notifications.Features;
using Unity.Notifications.Integrations.Ches;
using Unity.Notifications.Integrations.RabbitMQ;
using Unity.Notifications.Integrations.RabbitMQ.QueueMessages;
using Unity.Notifications.Settings;
using Unity.Notifications.Templates;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Features;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.SettingManagement;
using Volo.Abp.Settings;
using Volo.Abp.Timing;
using Volo.Abp.Users;
using Xunit;

namespace Unity.Notifications;

public class IndividualScheduledEmailServiceTests
{
    private readonly IFeatureChecker _features = Substitute.For<IFeatureChecker>();
    private readonly ISettingManager _settings = Substitute.For<ISettingManager>();
    private readonly IEmailLogsRepository _logs = Substitute.For<IEmailLogsRepository>();
    private readonly ILocalEventBus _events = Substitute.For<ILocalEventBus>();
    private readonly EmailComposerAccessChecker _access;
    private readonly EmailNotificationService _service;
    private readonly EmailAppService _sender;

    public IndividualScheduledEmailServiceTests()
    {
        _features.IsEnabledAsync("Unity.Notifications").Returns(true);
        _features.IsEnabledAsync(NotificationsFeatureConsts.IndividualScheduledNotification).Returns(false);
        var permissions = Substitute.For<IPermissionChecker>();
        permissions.IsGrantedAsync(Arg.Any<string>()).Returns(true);
        var tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns(Guid.NewGuid());
        tenant.IsAvailable.Returns(true);
        var settings = Substitute.For<ISettingProvider>();
        settings.GetOrNullAsync(NotificationsSettings.Mailing.EnableEmailDelay).Returns("true");
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(Guid.NewGuid());
        var lazy = Substitute.For<IAbpLazyServiceProvider>();
        lazy.LazyGetRequiredService<ICurrentTenant>().Returns(tenant);
        lazy.LazyGetRequiredService<ICurrentUser>().Returns(currentUser);
        lazy.LazyGetRequiredService<IFeatureChecker>().Returns(_features);
        lazy.LazyGetRequiredService<ISettingProvider>().Returns(settings);
        var clock = Substitute.For<IClock>();
        clock.Now.Returns(DateTime.UtcNow);
        lazy.LazyGetRequiredService<IClock>().Returns(clock);
        var guids = Substitute.For<IGuidGenerator>();
        guids.Create().Returns(_ => Guid.NewGuid());
        lazy.LazyGetRequiredService<IGuidGenerator>().Returns(guids);
        lazy.LazyGetService<ILogger>(Arg.Any<Func<IServiceProvider, ILogger>>()).Returns(NullLogger.Instance);

        var configuration = new ConfigurationBuilder().Build();
        var attachments = new EmailAttachmentService(configuration, Substitute.For<IEmailLogAttachmentRepository>(),
            currentUser, NullLogger<EmailAttachmentService>.Instance, Substitute.For<IAmazonS3>());
        var queue = new EmailQueueService(Substitute.For<IQueueProducer<EmailMessages>>(), NullLogger<EmailQueueService>.Instance)
        {
            LazyServiceProvider = lazy
        };
        var addresses = Substitute.For<IEmailAddressConfigurationsRepository>();
        var manager = new EmailNotificationManager(_logs, Substitute.For<IChesClientService>(), queue, attachments, addresses)
        {
            LazyServiceProvider = lazy
        };
        var comments = new CommentNotificationService(manager, _features, configuration, Substitute.For<IMarkdownRenderer>(),
            Substitute.For<IIdentityUserRepository>(), addresses, _settings, Substitute.For<IAssessmentRepository>())
        {
            LazyServiceProvider = lazy
        };
        var applicants = Substitute.For<IApplicantRepository>();
        applicants.GetAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(new Applicant());
        var applications = Substitute.For<IApplicationRepository>();
        applications.GetAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(new Application());
        _access = new EmailComposerAccessChecker(_logs, Substitute.For<ITemplatesRepository>(),
            applicants, applications, permissions, _features, settings, currentUser);
        _service = new EmailNotificationService(Substitute.For<INotificationsAppService>(), manager,
            Substitute.For<IExternalUserLookupServiceProvider>(), _settings, comments, _access, _logs, _features)
        {
            LazyServiceProvider = lazy
        };
        _sender = new EmailAppService(_events, _service, attachments, _access, manager)
        {
            LazyServiceProvider = lazy
        };
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Should_PreserveHiddenSchedulingSetting_WhileSavingOtherSettings(bool submittedValue)
    {
        await _service.UpdateSettings(new NotificationsSettingsDto
        {
            MaximumRetryAttempts = "5",
            EnableEmailDelay = submittedValue
        });

        var writes = _settings.ReceivedCalls().Where(call => call.GetMethodInfo().Name == nameof(ISettingManager.SetAsync))
            .Select(call => call.GetArguments()).ToList();
        writes.ShouldContain(args => Equals(args[0], NotificationsSettings.Mailing.EmailMaxRetryAttempts)
            && Equals(args[1], "5"));
        writes.ShouldNotContain(args => Equals(args[0], NotificationsSettings.Mailing.EnableEmailDelay));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Should_UpdateSchedulingSetting_WhenFeatureIsEnabled(bool submittedValue)
    {
        _features.IsEnabledAsync(NotificationsFeatureConsts.IndividualScheduledNotification).Returns(true);

        await _service.UpdateSettings(new NotificationsSettingsDto { EnableEmailDelay = submittedValue });

        var writes = _settings.ReceivedCalls().Where(call => call.GetMethodInfo().Name == nameof(ISettingManager.SetAsync))
            .Select(call => call.GetArguments()).ToList();
        writes.ShouldContain(args => Equals(args[0], NotificationsSettings.Mailing.EnableEmailDelay)
            && Equals(args[1], submittedValue ? "true" : "false"));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Should_RejectBothCancellationEndpoints_WhenFeatureIsDisabled(bool isApplicant, bool useDeleteEndpoint)
    {
        var email = new EmailLog
        {
            Id = Guid.NewGuid(),
            ApplicationId = isApplicant ? Guid.Empty : Guid.NewGuid(),
            ApplicantId = isApplicant ? Guid.NewGuid() : Guid.Empty,
            Status = EmailStatus.Scheduled,
            SendOnDateTime = DateTime.UtcNow.AddDays(1)
        };
        _logs.GetAsync(email.Id, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(email);

        await Should.ThrowAsync<AbpAuthorizationException>(() => useDeleteEndpoint
            ? _service.DeleteEmail(email.Id)
            : _service.CancelEmail(email.Id));

        email.Status.ShouldBe(EmailStatus.Scheduled);
        _logs.ReceivedCalls().ShouldNotContain(call => call.GetMethodInfo().Name == "UpdateAsync"
            || call.GetMethodInfo().Name == "DeleteAsync");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Should_RejectDirectScheduledSend_WhenFeatureIsDisabled(bool isApplicant)
    {
        var request = CreateRequest(isApplicant);
        request.SendOnDateTime = DateTime.UtcNow.AddDays(1);

        await Should.ThrowAsync<AbpAuthorizationException>(() => _sender.SendAsync(request));

        _events.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Should_RejectDatedDrafts_RegardlessOfSchedulingFeature(bool isApplicant, bool featureEnabled)
    {
        _features.IsEnabledAsync(NotificationsFeatureConsts.IndividualScheduledNotification).Returns(featureEnabled);
        var request = CreateRequest(isApplicant);
        request.SendOnDateTime = DateTime.UtcNow.AddDays(1);

        var exception = await Should.ThrowAsync<BusinessException>(() => _sender.SaveDraftAsync(request));

        exception.Code.ShouldBe(NotificationsErrorCodes.EmailDraftCannotBeScheduled);
        _events.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Should_AllowImmediateSend_WhenIndividualSchedulingIsDisabled(bool isApplicant)
    {
        var result = await _sender.SendAsync(CreateRequest(isApplicant));

        result.ShouldBeTrue();
        var publishedEvents = _events.ReceivedCalls().SelectMany(call => call.GetArguments())
            .OfType<EmailNotificationEvent>().ToList();
        publishedEvents.ShouldHaveSingleItem().SendOnDateTime.ShouldBeNull();
    }

    private static CreateEmailDto CreateRequest(bool isApplicant)
    {
        return new CreateEmailDto
        {
            ApplicationId = isApplicant ? Guid.Empty : Guid.NewGuid(),
            ApplicantId = isApplicant ? Guid.NewGuid() : Guid.Empty,
            EmailTo = "recipient@example.test",
            EmailFrom = "sender@example.test",
            EmailSubject = "Individual email",
            EmailBody = "Message"
        };
    }
}
