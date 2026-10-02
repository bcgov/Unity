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
using Xunit;

namespace Unity.Notifications;

public class EmailComposerAccessCheckerTests
{
    private readonly IEmailLogsRepository _logs = Substitute.For<IEmailLogsRepository>();
    private readonly ITemplatesRepository _templates = Substitute.For<ITemplatesRepository>();
    private readonly IApplicantRepository _applicants = Substitute.For<IApplicantRepository>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly IFeatureChecker _features = Substitute.For<IFeatureChecker>();
    private readonly EmailComposerAccessChecker _checker;

    public EmailComposerAccessCheckerTests()
    {
        _features.IsEnabledAsync("Unity.Notifications").Returns(true);
        _permissions.IsGrantedAsync(Arg.Any<string>()).Returns(true);
        _checker = new EmailComposerAccessChecker(_logs, _templates, _applicants,
            Substitute.For<IApplicationRepository>(), _permissions, _features, Substitute.For<ISettingProvider>());
    }

    [Theory]
    [InlineData(NotificationsPermissions.Email.Default)]
    [InlineData(NotificationsPermissions.Email.Send)]
    [InlineData(UnitySelector.ApplicantManagement.Applicant.Default)]
    public async Task Should_RequireEmailAndApplicantPermissions(string deniedPermission)
    {
        _permissions.IsGrantedAsync(deniedPermission).Returns(false);
        await Should.ThrowAsync<AbpAuthorizationException>(() => _checker.CheckOwnerAsync(Guid.Empty, Guid.NewGuid(), NotificationsPermissions.Email.Send));
    }

    [Fact]
    public async Task Should_RejectDisabledEmailFeature()
    {
        _features.IsEnabledAsync("Unity.Notifications").Returns(false);
        await Should.ThrowAsync<AbpAuthorizationException>(() => _checker.CheckOwnerAsync(Guid.Empty, Guid.NewGuid(), NotificationsPermissions.Email.Send));
    }

    [Fact]
    public async Task Should_RejectApplicantOutsideTheCurrentTenantRepository()
    {
        var foreignId = Guid.NewGuid();
        _applicants.GetAsync(foreignId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Applicant>(new EntityNotFoundException(typeof(Applicant), foreignId)));
        await Should.ThrowAsync<EntityNotFoundException>(() => _checker.CheckOwnerAsync(Guid.Empty, foreignId, NotificationsPermissions.Email.Send));
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
        await Should.ThrowAsync<BusinessException>(() => _checker.CheckEmailAsync(email.Id, NotificationsPermissions.Email.Send, requireDraft: true));
    }
}
