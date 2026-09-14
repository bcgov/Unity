using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantsPortal.Notifications;
using Unity.Notifications.EmailAddresses;
using Unity.Notifications.EmailNotifications;
using Unity.Notifications.Emails;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Features;
using Volo.Abp.Guids;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Timing;
using Volo.Abp.Uow;
using Xunit;

namespace Unity.GrantManager.GrantsPortal;

public class ApplicantUpdateNotificationServiceTests
{
    private readonly IApplicantRepository _applicants = Substitute.For<IApplicantRepository>();
    private readonly IEmailAddressConfigurationsRepository _addresses = Substitute.For<IEmailAddressConfigurationsRepository>();
    private readonly IEmailLogsRepository _logs = Substitute.For<IEmailLogsRepository>();
    private readonly IEmailNotificationManager _emailManager = Substitute.For<IEmailNotificationManager>();
    private readonly IFeatureChecker _features = Substitute.For<IFeatureChecker>();
    private readonly ICurrentTenant _tenant = Substitute.For<ICurrentTenant>();
    private readonly IUnitOfWorkManager _uowManager = Substitute.For<IUnitOfWorkManager>();
    private readonly IUnitOfWork _portalUow = Substitute.For<IUnitOfWork>();
    private readonly IUnitOfWork _notificationUow = Substitute.For<IUnitOfWork>();
    private readonly IUnitOfWork _queueUow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
    private readonly List<EmailAddressConfiguration> _configuredAddresses = [];
    private readonly ApplicantUpdateNotificationService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private Func<Task>? _afterCommit;
    private EmailLog? _savedEmail;

    private static ApplicantUpdateDetails Details => new("Contact", [new("Contact name", "Jane Doe")]);

    public ApplicantUpdateNotificationServiceTests()
    {
        _tenant.Id.Returns(_tenantId);
        _features.IsEnabledAsync("Unity.Notifications").Returns(true);
        _configuration["App:SelfUrl"].Returns("https://unity.example.test/");
        _configuredAddresses.Add(new EmailAddressConfiguration(Guid.NewGuid(), "default@example.test", "Sender", "", true));
        _addresses.GetListAsync(Arg.Any<Expression<Func<EmailAddressConfiguration, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(call => _configuredAddresses.Where(call.ArgAt<Expression<Func<EmailAddressConfiguration, bool>>>(0).Compile()).ToList());
        _uowManager.Current.Returns(_portalUow);
        _uowManager.Begin(Arg.Any<AbpUnitOfWorkOptions>(), Arg.Any<bool>()).Returns(_notificationUow, _queueUow);
        _portalUow.When(uow => uow.OnCompleted(Arg.Any<Func<Task>>())).Do(call => _afterCommit = call.Arg<Func<Task>>());
        _logs.InsertAsync(Arg.Any<EmailLog>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _savedEmail = call.ArgAt<EmailLog>(0);
            return _savedEmail;
        });
        var guidGenerator = Substitute.For<IGuidGenerator>();
        guidGenerator.Create().Returns(_ => Guid.NewGuid());
        _clock.Now.Returns(new DateTime(2025, 7, 1, 12, 34, 56, DateTimeKind.Utc));
        _service = new ApplicantUpdateNotificationService(_applicants, _addresses, _logs, _emailManager,
            _features, _tenant, _uowManager, guidGenerator, _clock, _configuration,
            NullLogger<ApplicantUpdateNotificationService>.Instance);
    }

    private static Applicant Applicant()
    {
        var applicant = new Applicant { ApplicantName = "Applicant Name", UnityApplicantId = "APP-123", OrgName = "Organization Name" };
        EntityHelper.TrySetId(applicant, Guid.NewGuid);
        return applicant;
    }

    private async Task CompletePortalUpdateAsync()
    {
        _afterCommit.ShouldNotBeNull();
        await _afterCommit();
    }

    [Fact]
    public async Task ShouldPerformNoNotificationIoUnlessPortalCommitCompletes()
    {
        await _service.QueueAsync(Guid.NewGuid(), Details);

        _afterCommit.ShouldNotBeNull();
        _savedEmail.ShouldBeNull();
        await _features.DidNotReceive().IsEnabledAsync(Arg.Any<string>());
        await _addresses.DidNotReceive().GetListAsync(Arg.Any<Expression<Func<EmailAddressConfiguration, bool>>>(),
            Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await _applicants.DidNotReceive().GetAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await _logs.DidNotReceive().InsertAsync(Arg.Any<EmailLog>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await _emailManager.DidNotReceive().QueueEmailAsync(Arg.Any<EmailLog>());
        _uowManager.DidNotReceive().Begin(Arg.Any<AbpUnitOfWorkOptions>(), Arg.Any<bool>());
        // On rollback the portal never invokes this completion callback.
    }

    [Fact]
    public async Task ShouldPreserveSnapshotAndPersistInSeparateTransactionBeforePublishing()
    {
        var applicant = Applicant();
        var fields = new List<ApplicantUpdateField> { new("Contact name", "Jane Doe") };
        await _service.QueueAsync(applicant, new("Contact", fields));

        _savedEmail.ShouldBeNull();
        applicant.ApplicantName = "Later change";
        fields[0] = new("Contact name", "Later contact");
        _clock.Now.Returns(new DateTime(2025, 7, 2, 12, 34, 56, DateTimeKind.Utc));
        await CompletePortalUpdateAsync();

        _savedEmail.ShouldNotBeNull();
        _savedEmail.TenantId.ShouldBe(_tenantId);
        _savedEmail.ApplicantId.ShouldBe(applicant.Id);
        _savedEmail.ApplicationId.ShouldBe(Guid.Empty);
        _savedEmail.ToAddress.ShouldBe("default@example.test");
        _savedEmail.FromAddress.ShouldBe("default@example.test");
        _savedEmail.Subject.ShouldBe("APP-123 - Applicant Name - Update");
        _savedEmail.BodyType.ShouldBe("html");
        _savedEmail.Status.ShouldBe(EmailStatus.Initialized);
        _savedEmail.EmailType.ShouldBe(EmailType.EventBased);
        _savedEmail.Body.ShouldContain($"ApplicantId={applicant.Id}&amp;TenantId={_tenantId}");
        _savedEmail.Body.ShouldContain(">APP-123</a>");
        _savedEmail.Body.ShouldContain("2025-07-01 5:34 AM (PDT)");
        _savedEmail.Body.ShouldContain("Jane Doe");
        _savedEmail.Body.ShouldNotContain("Later");
        _uowManager.Received(1).Begin(Arg.Is<AbpUnitOfWorkOptions>(options => options.IsTransactional == true), true);
        _uowManager.Received(1).Begin(Arg.Is<AbpUnitOfWorkOptions>(options => options.IsTransactional == false), true);
        Received.InOrder(() =>
        {
            _notificationUow.CompleteAsync();
            _emailManager.QueueEmailAsync(_savedEmail);
        });
    }

    [Theory]
    [InlineData(false, true, "default@example.test")]
    [InlineData(true, false, "default@example.test")]
    [InlineData(true, true, " ")]
    public async Task ShouldSkipWithoutUsableActiveDefault(bool active, bool isDefault, string address)
    {
        _configuredAddresses[0].IsActive = active;
        _configuredAddresses[0].IsDefault = isDefault;
        _configuredAddresses[0].EmailAddress = address;

        await _service.QueueAsync(Applicant(), Details);
        await CompletePortalUpdateAsync();

        _savedEmail.ShouldBeNull();
        await _emailManager.DidNotReceive().QueueEmailAsync(Arg.Any<EmailLog>());
    }

    [Fact]
    public async Task ShouldSkipMissingDefaultBeforeLoadingApplicant()
    {
        _configuredAddresses.Clear();
        await _service.QueueAsync(Guid.NewGuid(), Details);
        await CompletePortalUpdateAsync();
        _savedEmail.ShouldBeNull();
        await _applicants.DidNotReceive().GetAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldUseWidgetDefaultEvenWhenItsTypeIsNotSender()
    {
        _configuredAddresses[0].EmailType = "Other";
        _configuredAddresses[0].EmailAddress = " default@example.test ";
        await _service.QueueAsync(Applicant(), Details);
        await CompletePortalUpdateAsync();
        _savedEmail.ShouldNotBeNull();
        _savedEmail.ToAddress.ShouldBe("default@example.test");
        _savedEmail.FromAddress.ShouldBe("default@example.test");
    }

    [Fact]
    public async Task ShouldSkipWhenFeatureDisabled()
    {
        _features.IsEnabledAsync("Unity.Notifications").Returns(false);
        await _service.QueueAsync(Applicant(), Details);
        await CompletePortalUpdateAsync();
        _savedEmail.ShouldBeNull();
        await _emailManager.DidNotReceive().QueueEmailAsync(Arg.Any<EmailLog>());
    }

    [Fact]
    public async Task ShouldNotRegisterNotificationWhenNoFieldsChanged()
    {
        await _service.QueueAsync(Applicant(), new("Applicant Information", [], true));
        _afterCommit.ShouldBeNull();
        _savedEmail.ShouldBeNull();
    }

    [Fact]
    public async Task ShouldRestoreCapturedTenantForApplicantLookupAfterCommit()
    {
        var applicant = Applicant();
        var otherTenant = Guid.NewGuid();
        _tenant.Id.Returns(otherTenant);
        _applicants.GetAsync(applicant.Id, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(applicant);
        await _service.QueueAsync(applicant.Id, Details);
        _tenant.Id.Returns(_tenantId);

        await CompletePortalUpdateAsync();

        _tenant.Received().Change(otherTenant);
        await _applicants.Received(1).GetAsync(applicant.Id, Arg.Any<bool>(), Arg.Any<CancellationToken>());
        _savedEmail.ShouldNotBeNull();
        _savedEmail.TenantId.ShouldBe(otherTenant);
        _savedEmail.Body.ShouldContain($"TenantId={otherTenant}");
        _savedEmail.Body.ShouldNotContain(_tenantId.ToString());
    }

    [Fact]
    public async Task ShouldKeepCommittedLogRecoverableWhenPublishingFails()
    {
        _emailManager.QueueEmailAsync(Arg.Any<EmailLog>()).Returns(Task.FromException(new InvalidOperationException("Queue unavailable")));
        await _service.QueueAsync(Applicant(), Details);

        await CompletePortalUpdateAsync(); // Must not propagate into the portal acknowledgment.

        _savedEmail.ShouldNotBeNull();
        _savedEmail.Status.ShouldBe(EmailStatus.Initialized);
        _savedEmail.RetryAttempts.ShouldBe(0);
        await _notificationUow.Received(1).CompleteAsync();
    }

    [Theory]
    [InlineData("feature")]
    [InlineData("addresses")]
    [InlineData("applicant")]
    [InlineData("rendering")]
    [InlineData("insert")]
    [InlineData("commit")]
    public async Task ShouldContainNotificationFailuresAfterPortalCommit(string failure)
    {
        var applicant = Applicant();
        var details = Details;
        _applicants.GetAsync(applicant.Id, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(applicant);
        var error = new InvalidOperationException("Notification dependency unavailable");
        switch (failure)
        {
            case "feature":
                _features.IsEnabledAsync("Unity.Notifications").Returns(Task.FromException<bool>(error));
                break;
            case "addresses":
                _addresses.GetListAsync(Arg.Any<Expression<Func<EmailAddressConfiguration, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                    .Returns(Task.FromException<List<EmailAddressConfiguration>>(error));
                break;
            case "applicant":
                _applicants.GetAsync(applicant.Id, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<Applicant>(error));
                break;
            case "rendering":
                details = new("Contact", [null!]);
                break;
            case "insert":
                _logs.InsertAsync(Arg.Any<EmailLog>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                    .Returns(Task.FromException<EmailLog>(error));
                break;
            case "commit":
                _notificationUow.CompleteAsync().Returns(Task.FromException(error));
                break;
        }

        await _service.QueueAsync(applicant.Id, details);
        _savedEmail.ShouldBeNull();
        await CompletePortalUpdateAsync(); // No exception, including a failure at notification transaction commit.

        await _emailManager.DidNotReceive().QueueEmailAsync(Arg.Any<EmailLog>());
        _notificationUow.Received().Dispose();
        _uowManager.Received(1).Begin(Arg.Is<AbpUnitOfWorkOptions>(options => options.IsTransactional == true), true);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not-a-url")]
    [InlineData("/relative")]
    [InlineData("ftp://unity.example.test")]
    [InlineData("https://unity.example.test?query=1")]
    [InlineData("https://unity.example.test#fragment")]
    public async Task ShouldSkipMissingOrInvalidBaseUrlWithoutFailingPortalUpdate(string? baseUrl)
    {
        _configuration["App:SelfUrl"].Returns(baseUrl);
        await _service.QueueAsync(Applicant(), Details);
        await CompletePortalUpdateAsync();
        _savedEmail.ShouldBeNull();
        await _emailManager.DidNotReceive().QueueEmailAsync(Arg.Any<EmailLog>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ShouldSkipMissingSchedulingContextWithoutThrowing(bool missingTenant)
    {
        if (missingTenant)
        {
            _tenant.Id.Returns((Guid?)null);
        }
        else
        {
            _uowManager.Current.Returns((IUnitOfWork?)null);
        }
        await _service.QueueAsync(Applicant(), Details);
        _afterCommit.ShouldBeNull();
        _savedEmail.ShouldBeNull();
    }

    [Fact]
    public async Task ShouldFallbackForLegacyApplicantIdentity()
    {
        var applicant = Applicant();
        applicant.UnityApplicantId = null;
        applicant.ApplicantName = " ";
        await _service.QueueAsync(applicant, Details);
        await CompletePortalUpdateAsync();
        _savedEmail.ShouldNotBeNull();
        _savedEmail.Subject.ShouldBe($"{applicant.Id} - Organization Name - Update");
    }

    [Theory]
    [InlineData(null, "NoReply@gov.bc.ca")]
    [InlineData(" ", "NoReply@gov.bc.ca")]
    [InlineData(" sender@example.test ", "sender@example.test")]
    public void ShouldResolveSenderWithFallback(string? configured, string expected) =>
        ApplicantUpdateNotificationService.ResolveFromAddress(configured).ShouldBe(expected);
}
