using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.Modules.Shared;
using Unity.Notifications.Permissions;
using Unity.Notifications.Settings;
using Unity.Notifications.Templates;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Features;
using Volo.Abp.Settings;

namespace Unity.Notifications.Emails;

/// <summary>Shared checks for interactive email entry points; background delivery does not use this service.</summary>
public class EmailComposerAccessChecker(
    IEmailLogsRepository emailLogsRepository,
    ITemplatesRepository templatesRepository,
    IApplicantRepository applicantRepository,
    IApplicationRepository applicationRepository,
    IPermissionChecker permissionChecker,
    IFeatureChecker featureChecker,
    ISettingProvider settingProvider) : ITransientDependency
{
    public async Task CheckOwnerAsync(Guid applicationId, Guid applicantId, string permission)
    {
        EmailOwnership.EnsureSingleOwner(applicationId, applicantId);
        await CheckPermissionsAsync(applicantId, permission);
        if (applicantId != Guid.Empty)
        {
            await applicantRepository.GetAsync(applicantId);
        }
        else
        {
            await applicationRepository.GetAsync(applicationId);
        }
    }

    public async Task CheckPermissionsAsync(Guid applicantId, string permission)
    {
        if (!await featureChecker.IsEnabledAsync("Unity.Notifications")
            || !await permissionChecker.IsGrantedAsync(NotificationsPermissions.Email.Default)
            || !await permissionChecker.IsGrantedAsync(permission)
            || (applicantId != Guid.Empty && !await permissionChecker.IsGrantedAsync(UnitySelector.ApplicantManagement.Applicant.Default)))
        {
            throw new AbpAuthorizationException();
        }
    }

    public async Task<EmailLog> CheckEmailAsync(Guid emailId, string permission, bool requireDraft = false)
    {
        var email = await emailLogsRepository.GetAsync(emailId);
        await CheckOwnerAsync(email.ApplicationId, email.ApplicantId, permission);
        if (requireDraft)
        {
            EmailOwnership.EnsureDraft(email);
        }
        return email;
    }

    public async Task CheckEmailReadAsync(Guid emailId)
    {
        var email = await emailLogsRepository.GetAsync(emailId);
        // The global Notifications list has its own read permission, including system emails with no owner.
        if (await permissionChecker.IsGrantedAsync(NotificationsPermissions.NotificationList.View))
        {
            return;
        }
        await CheckOwnerAsync(email.ApplicationId, email.ApplicantId, NotificationsPermissions.Email.Default);
    }

    public async Task<EmailTemplate> CheckTemplateAsync(Guid templateId, Guid applicantId)
    {
        var template = await templatesRepository.GetByIdAsync(templateId)
            ?? throw new EntityNotFoundException(typeof(EmailTemplate), templateId);
        EmailOwnership.EnsureTemplateType(template, applicantId);
        return template;
    }

    public async Task CheckScheduleAsync(Guid applicantId)
    {
        await CheckPermissionsAsync(applicantId, NotificationsPermissions.Email.Schedule);
        if (!string.Equals(await settingProvider.GetOrNullAsync(NotificationsSettings.Mailing.EnableEmailDelay),
            "true", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException("Notifications:EmailSchedulingDisabled");
        }
    }
}
