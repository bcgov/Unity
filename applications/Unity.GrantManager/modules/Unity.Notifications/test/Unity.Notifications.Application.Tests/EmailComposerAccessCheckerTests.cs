using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Unity.GrantManager.Applications;
using Unity.Modules.Shared;
using Unity.Notifications.Emails;
using Unity.Notifications.Permissions;
using Unity.Notifications.Templates;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Features;
using Volo.Abp.Settings;
using Volo.Abp.Users;
using Xunit;

namespace Unity.Notifications;

public class EmailComposerAccessCheckerTests
{
    private readonly IEmailLogsRepository _logs = Substitute.For<IEmailLogsRepository>();
    private readonly ITemplatesRepository _templates = Substitute.For<ITemplatesRepository>();
    private readonly IApplicantRepository _applicants = Substitute.For<IApplicantRepository>();
    private readonly IApplicationRepository _applications = Substitute.For<IApplicationRepository>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly IFeatureChecker _features = Substitute.For<IFeatureChecker>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly EmailComposerAccessChecker _checker;

    public EmailComposerAccessCheckerTests()
    {
        _features.IsEnabledAsync("Unity.Notifications").Returns(true);
        _permissions.IsGrantedAsync(Arg.Any<string>()).Returns(true);
        _currentUser.Id.Returns(_userId);
        _checker = new EmailComposerAccessChecker(_logs, _templates, _applicants,
            _applications, _permissions, _features, Substitute.For<ISettingProvider>(), _currentUser);
    }

    [Theory]
    [InlineData(NotificationsPermissions.Email.Default)]
    [InlineData(NotificationsPermissions.Email.Applicant.Default)]
    [InlineData(NotificationsPermissions.Email.Applicant.Send)]
    [InlineData(UnitySelector.ApplicantManagement.Applicant.Default)]
    public async Task Should_RequireEmailAndApplicantPermissions(string deniedPermission)
    {
        _permissions.IsGrantedAsync(deniedPermission).Returns(false);
        await Should.ThrowAsync<AbpAuthorizationException>(() => _checker.CheckOwnerAsync(Guid.Empty, Guid.NewGuid(), EmailOperation.Send));
    }

    [Theory]
    [InlineData(NotificationsPermissions.Email.Application.Default)]
    [InlineData(NotificationsPermissions.Email.Application.Send)]
    public async Task Should_RequireApplicationScopedPermissions(string deniedPermission)
    {
        _permissions.IsGrantedAsync(deniedPermission).Returns(false);
        await Should.ThrowAsync<AbpAuthorizationException>(() => _checker.CheckOwnerAsync(Guid.NewGuid(), Guid.Empty, EmailOperation.Send));
    }

    [Fact]
    public async Task Should_NotLetOneOwnerTypeAuthorizeTheOther()
    {
        _permissions.IsGrantedAsync(Arg.Is<string>(p => p != null && p.StartsWith("Notifications.Email.Applicant"))).Returns(false);
        await Should.ThrowAsync<AbpAuthorizationException>(() => _checker.CheckOwnerAsync(Guid.Empty, Guid.NewGuid(), EmailOperation.View));
        await _checker.CheckOwnerAsync(Guid.NewGuid(), Guid.Empty, EmailOperation.Send);
    }

    [Fact]
    public async Task Should_RejectLegacyGenericSendAlone()
    {
        _permissions.IsGrantedAsync(Arg.Any<string>()).Returns(false);
        _permissions.IsGrantedAsync(NotificationsPermissions.Email.Default).Returns(true);
        _permissions.IsGrantedAsync(NotificationsPermissions.Email.Send.Default).Returns(true);
        await Should.ThrowAsync<AbpAuthorizationException>(() => _checker.CheckOwnerAsync(Guid.NewGuid(), Guid.Empty, EmailOperation.Send));
    }

    [Fact]
    public async Task Should_RejectDisabledEmailFeature()
    {
        _features.IsEnabledAsync("Unity.Notifications").Returns(false);
        await Should.ThrowAsync<AbpAuthorizationException>(() => _checker.CheckOwnerAsync(Guid.Empty, Guid.NewGuid(), EmailOperation.Send));
    }

    [Fact]
    public async Task Should_RejectApplicantOutsideTheCurrentTenantRepository()
    {
        var foreignId = Guid.NewGuid();
        _applicants.GetAsync(foreignId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Applicant>(new EntityNotFoundException(typeof(Applicant), foreignId)));
        await Should.ThrowAsync<EntityNotFoundException>(() => _checker.CheckOwnerAsync(Guid.Empty, foreignId, EmailOperation.Send));
    }

    [Fact]
    public async Task Should_RejectApplicationTemplateForApplicant()
    {
        var template = new EmailTemplate(Guid.NewGuid(), "Application", "", "Subject", "", "Body", "sender@example.test");
        _templates.GetByIdAsync(template.Id).Returns(template);
        await Should.ThrowAsync<BusinessException>(() => _checker.CheckTemplateAsync(template.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task Should_RejectChangingAttachmentsOnASentEmail()
    {
        var email = new EmailLog { Id = Guid.NewGuid(), ApplicantId = Guid.NewGuid(), Status = EmailStatus.Sent };
        _logs.GetAsync(email.Id, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(email);
        await Should.ThrowAsync<BusinessException>(() => _checker.CheckEmailAsync(email.Id, EmailOperation.Send, requireDraft: true));
    }

    [Fact]
    public async Task Should_LetCreateEditOnlyTheUsersOwnDrafts()
    {
        _permissions.IsGrantedAsync(NotificationsPermissions.Email.Application.Edit).Returns(false);
        var ownDraft = new EmailLog { Id = Guid.NewGuid(), ApplicationId = Guid.NewGuid(), Status = EmailStatus.Draft, CreatorId = _userId };
        var otherDraft = new EmailLog { Id = Guid.NewGuid(), ApplicationId = Guid.NewGuid(), Status = EmailStatus.Draft, CreatorId = Guid.NewGuid() };

        (await _checker.CanEditDraftAsync(ownDraft)).ShouldBeTrue();
        (await _checker.CanEditDraftAsync(otherDraft)).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_LetEditChangeAnyonesDraft()
    {
        _permissions.IsGrantedAsync(NotificationsPermissions.Email.Application.Create).Returns(false);
        var otherDraft = new EmailLog { Id = Guid.NewGuid(), ApplicationId = Guid.NewGuid(), Status = EmailStatus.Draft, CreatorId = Guid.NewGuid() };

        (await _checker.CanEditDraftAsync(otherDraft)).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_RequireSendBeforeSchedule()
    {
        _permissions.IsGrantedAsync(NotificationsPermissions.Email.Application.Send).Returns(false);
        await Should.ThrowAsync<AbpAuthorizationException>(() => _checker.CheckPermissionsAsync(Guid.Empty, EmailOperation.Schedule));
    }

    [Fact]
    public async Task Should_ReportNoCapabilitiesWithoutOwnerTypeView()
    {
        _permissions.IsGrantedAsync(NotificationsPermissions.Email.Applicant.Default).Returns(false);

        var capabilities = await _checker.GetCapabilitiesAsync(EmailOwnerTypes.Applicant);

        capabilities.CanView.ShouldBeFalse();
        capabilities.CanSend.ShouldBeFalse();
        capabilities.CanCreate.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_ReportCapabilitiesPerOwnerType()
    {
        _permissions.IsGrantedAsync(NotificationsPermissions.Email.Application.Send).Returns(false);
        _permissions.IsGrantedAsync(NotificationsPermissions.Email.Applicant.Edit).Returns(false);

        var application = await _checker.GetCapabilitiesAsync(EmailOwnerTypes.Application);
        var applicant = await _checker.GetCapabilitiesAsync(EmailOwnerTypes.Applicant);

        application.CanEdit.ShouldBeTrue();
        application.CanSend.ShouldBeFalse();
        application.CanSchedule.ShouldBeFalse();
        applicant.CanEdit.ShouldBeFalse();
        applicant.CanSend.ShouldBeTrue();
    }
}