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
using Volo.Abp.Users;

namespace Unity.Notifications.Emails;

/// <summary>Shared checks for interactive email entry points; background delivery does not use this service.</summary>
public class EmailComposerAccessChecker(
    IEmailLogsRepository emailLogsRepository,
    ITemplatesRepository templatesRepository,
    IApplicantRepository applicantRepository,
    IApplicationRepository applicationRepository,
    IPermissionChecker permissionChecker,
    IFeatureChecker featureChecker,
    ISettingProvider settingProvider,
    ICurrentUser currentUser) : ITransientDependency
{
    public async Task CheckOwnerAsync(Guid applicationId, Guid applicantId, EmailOperation operation)
    {
        await CheckOwnerAnyAsync(applicationId, applicantId, operation);
    }

    public async Task CheckOwnerAnyAsync(Guid applicationId, Guid applicantId, params EmailOperation[] operations)
    {
        EmailOwnership.EnsureSingleOwner(applicationId, applicantId);
        var ownerType = EmailOwnerTypes.FromIds(applicantId);
        var allowed = false;
        foreach (var operation in operations)
        {
            if (await IsAllowedAsync(ownerType, operation))
            {
                allowed = true;
                break;
            }
        }

        if (!allowed)
        {
            throw new AbpAuthorizationException();
        }

        await EnsureOwnerExistsAsync(applicationId, applicantId);
    }

    public async Task CheckPermissionsAsync(Guid applicantId, EmailOperation operation)
    {
        if (!await IsAllowedAsync(EmailOwnerTypes.FromIds(applicantId), operation))
        {
            throw new AbpAuthorizationException();
        }
    }

    public async Task<EmailLog> CheckEmailAsync(Guid emailId, EmailOperation operation, bool requireDraft = false)
    {
        var email = await emailLogsRepository.GetAsync(emailId);
        await CheckOwnerAsync(email.ApplicationId, email.ApplicantId, operation);
        if (requireDraft)
        {
            EmailOwnership.EnsureDraft(email);
        }
        return email;
    }

    /// <summary>Edit rights over a draft: Edit on the owner type, or Create when the user started the draft.</summary>
    public async Task<EmailLog> CheckDraftEditAsync(Guid emailId)
    {
        var email = await emailLogsRepository.GetAsync(emailId);
        if (!await CanEditDraftAsync(email))
        {
            throw new AbpAuthorizationException();
        }
        EmailOwnership.EnsureDraft(email);
        await EnsureOwnerExistsAsync(email.ApplicationId, email.ApplicantId);
        return email;
    }

    public async Task<bool> CanEditDraftAsync(EmailLog email)
    {
        var ownerType = EmailOwnerTypes.FromIds(email.ApplicantId);
        return await IsAllowedAsync(ownerType, EmailOperation.Edit)
            || (IsCreator(email) && await IsAllowedAsync(ownerType, EmailOperation.Create));
    }

    public async Task CheckDraftDeleteAsync(EmailLog email)
    {
        var ownerType = EmailOwnerTypes.FromIds(email.ApplicantId);
        if (!await IsAllowedAsync(ownerType, EmailOperation.DeleteDraft) || !await CanEditDraftAsync(email))
        {
            throw new AbpAuthorizationException();
        }
        await EnsureOwnerExistsAsync(email.ApplicationId, email.ApplicantId);
    }

    public async Task CheckEmailReadAsync(Guid emailId)
    {
        var email = await emailLogsRepository.GetAsync(emailId);
        // The global Notifications list has its own read permission, including system emails with no owner.
        if (await permissionChecker.IsGrantedAsync(NotificationsPermissions.NotificationList.View))
        {
            return;
        }
        await CheckOwnerAsync(email.ApplicationId, email.ApplicantId, EmailOperation.View);
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
        await CheckPermissionsAsync(applicantId, EmailOperation.Schedule);
        if (!string.Equals(await settingProvider.GetOrNullAsync(NotificationsSettings.Mailing.EnableEmailDelay),
            "true", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException("Notifications:EmailSchedulingDisabled");
        }
    }

    public async Task<EmailCapabilitiesDto> GetCapabilitiesAsync(string ownerType)
    {
        var canView = await IsAllowedAsync(ownerType, EmailOperation.View);
        if (!canView)
        {
            return new EmailCapabilitiesDto();
        }

        var canCreate = await IsAllowedAsync(ownerType, EmailOperation.Create);
        var canEdit = await IsAllowedAsync(ownerType, EmailOperation.Edit);
        return new EmailCapabilitiesDto
        {
            CanView = true,
            CanCreate = canCreate,
            CanEdit = canEdit,
            CanSend = await IsAllowedAsync(ownerType, EmailOperation.Send),
            CanSchedule = await IsAllowedAsync(ownerType, EmailOperation.Schedule),
            CanDeleteDraft = (canCreate || canEdit) && await IsAllowedAsync(ownerType, EmailOperation.DeleteDraft),
            CanCancelScheduled = await IsAllowedAsync(ownerType, EmailOperation.CancelScheduled)
        };
    }

    private bool IsCreator(EmailLog email)
    {
        return currentUser.Id.HasValue && email.CreatorId == currentUser.Id;
    }

    // Every operation needs the module gate and the owner-type View; Schedule is additive to Send.
    private async Task<bool> IsAllowedAsync(string ownerType, EmailOperation operation)
    {
        if (!await featureChecker.IsEnabledAsync("Unity.Notifications")
            || !await permissionChecker.IsGrantedAsync(NotificationsPermissions.Email.Default)
            || !await permissionChecker.IsGrantedAsync(EmailPermissionMap.Get(ownerType, EmailOperation.View))
            || (ownerType == EmailOwnerTypes.Applicant && !await permissionChecker.IsGrantedAsync(UnitySelector.ApplicantManagement.Applicant.Default)))
        {
            return false;
        }

        if (operation == EmailOperation.View)
        {
            return true;
        }

        if (operation == EmailOperation.Schedule && !await permissionChecker.IsGrantedAsync(EmailPermissionMap.Get(ownerType, EmailOperation.Send)))
        {
            return false;
        }

        return await permissionChecker.IsGrantedAsync(EmailPermissionMap.Get(ownerType, operation));
    }

    private async Task EnsureOwnerExistsAsync(Guid applicationId, Guid applicantId)
    {
        if (applicantId != Guid.Empty)
        {
            await applicantRepository.GetAsync(applicantId);
        }
        else
        {
            await applicationRepository.GetAsync(applicationId);
        }
    }
}