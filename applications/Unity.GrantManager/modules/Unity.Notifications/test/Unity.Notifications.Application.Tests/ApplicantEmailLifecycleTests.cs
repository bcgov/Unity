using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Notifications;
using Unity.GrantManager.Notifications.Email;
using Unity.Notifications.EmailAddresses;
using Unity.Notifications.EmailNotifications;
using Unity.Notifications.Emails;
using Unity.Notifications.Events;
using Unity.Notifications.Templates;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Features;
using Volo.Abp.Guids;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Settings;
using Xunit;

namespace Unity.Notifications;

public class ApplicantEmailLifecycleTests
{
    private readonly IEmailLogsRepository _logs = Substitute.For<IEmailLogsRepository>();
    private readonly EmailNotificationManager _manager;

    public ApplicantEmailLifecycleTests()
    {
        var addresses = Substitute.For<IEmailAddressConfigurationsRepository>();
        addresses.GetListAsync(Arg.Any<Expression<Func<EmailAddressConfiguration, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new List<EmailAddressConfiguration>());
        _logs.InsertAsync(Arg.Any<EmailLog>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(call => call.Arg<EmailLog>());
        _logs.UpdateAsync(Arg.Any<EmailLog>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(call => call.Arg<EmailLog>());
        var guidGenerator = Substitute.For<IGuidGenerator>();
        guidGenerator.Create().Returns(_ => Guid.NewGuid());
        var lazy = Substitute.For<IAbpLazyServiceProvider>();
        lazy.LazyGetRequiredService<IGuidGenerator>().Returns(guidGenerator);
        _manager = new EmailNotificationManager(_logs, null!, null!, null!, addresses) { LazyServiceProvider = lazy };
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Should_PreserveApplicantOwnershipThroughSaveAndSend(bool scheduled)
    {
        var applicantId = Guid.NewGuid();
        var draft = await _manager.CreateDraftEmailLogAsync(Guid.Empty, applicantId);
        draft.Id = Guid.NewGuid();
        _logs.FindAsync(draft.Id, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(draft);
        var templateId = Guid.NewGuid();
        EmailOwnership.SetTemplateId(draft, templateId);
        var content = new EmailMessageParams("person@example.test", "Saved applicant body", "Applicant subject", "sender@example.test", "Applicant template");

        var saved = await _manager.UpdateEmailLogAsync(draft.Id, content, Guid.Empty, EmailStatus.Draft, applicantId);
        saved.ShouldNotBeNull();
        saved.ApplicantId.ShouldBe(applicantId);
        saved.ApplicationId.ShouldBe(Guid.Empty);
        saved.Status.ShouldBe(EmailStatus.Draft);

        var sendOn = scheduled ? DateTime.UtcNow.AddDays(1) : (DateTime?)null;
        var sent = await _manager.UpdateEmailLogAsync(draft.Id, content with { SendOnDateTime = sendOn }, Guid.Empty, EmailStatus.Initialized, applicantId);
        sent.ShouldNotBeNull();
        sent.ApplicantId.ShouldBe(applicantId);
        sent.ApplicationId.ShouldBe(Guid.Empty);
        sent.Body.ShouldBe("Saved applicant body");
        sent.Status.ShouldBe(scheduled ? EmailStatus.Scheduled : EmailStatus.Initialized);
        sent.EmailType.ShouldBe(scheduled ? EmailType.Delayed : EmailType.Manual);
        EmailOwnership.GetTemplateId(sent).ShouldBe(templateId);
    }

    [Fact]
    public async Task Should_CreateApplicantLogWithoutAnyApplication()
    {
        var applicantId = Guid.NewGuid();
        var log = await _manager.CreateEmailLogAsync(
            new EmailMessageParams("person@example.test", "Body", "Subject", "sender@example.test", null),
            Guid.Empty, EmailStatus.Draft, applicantId: applicantId);
        log.ShouldNotBeNull();
        log.ApplicantId.ShouldBe(applicantId);
        log.ApplicationId.ShouldBe(Guid.Empty);
        log.Status.ShouldBe(EmailStatus.Draft);
    }

    [Fact]
    public async Task Should_IncludeApplicantOwnershipInManualNotificationEvents()
    {
        var applicantId = Guid.NewGuid();
        var tenants = Substitute.For<ICurrentTenant>();
        tenants.Id.Returns(Guid.NewGuid());
        var lazy = Substitute.For<IAbpLazyServiceProvider>();
        lazy.LazyGetRequiredService<ICurrentTenant>().Returns(tenants);
        var features = Substitute.For<IFeatureChecker>();
        features.IsEnabledAsync("Unity.Notifications").Returns(true);
        var permissions = Substitute.For<IPermissionChecker>();
        permissions.IsGrantedAsync(Arg.Any<string>()).Returns(true);
        var access = new EmailComposerAccessChecker(_logs, Substitute.For<ITemplatesRepository>(), Substitute.For<IApplicantRepository>(),
            Substitute.For<IApplicationRepository>(), permissions, features, Substitute.For<ISettingProvider>());
        var events = Substitute.For<ILocalEventBus>();
        var service = new EmailAppService(events, Substitute.For<IEmailNotificationService>(), null!, access, _manager)
        {
            LazyServiceProvider = lazy
        };
        var input = new CreateEmailDto
        {
            ApplicantId = applicantId, EmailTo = "person@example.test", EmailFrom = "sender@example.test",
            EmailSubject = "Subject", EmailBody = "Body"
        };

        await service.SaveDraftAsync(input);
        await service.SendAsync(input);

        await events.Received().PublishAsync(Arg.Is<EmailNotificationEvent>(e =>
            e.Action == EmailAction.SaveDraft && e.ApplicantId == applicantId && e.ApplicationId == Guid.Empty), Arg.Any<bool>());
        await events.Received().PublishAsync(Arg.Is<EmailNotificationEvent>(e =>
            e.Action == EmailAction.SendCustom && e.ApplicantId == applicantId && e.ApplicationId == Guid.Empty && e.TenantId == tenants.Id), Arg.Any<bool>());
    }
}
