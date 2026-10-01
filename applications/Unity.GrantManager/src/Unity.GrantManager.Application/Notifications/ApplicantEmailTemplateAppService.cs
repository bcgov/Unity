using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Events;
using Unity.GrantManager.Notifications.Email;
using Unity.Notifications.EmailGroups;
using Unity.Notifications.Emails;
using Unity.Notifications.Permissions;
using Volo.Abp.Application.Services;
using Volo.Abp.Identity.Integration;

namespace Unity.GrantManager.Notifications;

[Authorize(NotificationsPermissions.Email.Send)]
public class ApplicantEmailTemplateAppService(
    IApplicantRepository applicantRepository,
    IApplicantContactQueryService contactQueryService,
    EmailComposerAccessChecker emailAccessChecker,
    ScheduledNotificationHelper notificationHelper,
    IEmailGroupsAppService emailGroupsAppService,
    IEmailGroupUsersAppService emailGroupUsersAppService,
    IIdentityUserIntegrationService identityUserIntegrationService)
    : ApplicationService, IApplicantEmailTemplateAppService
{
    public virtual async Task<ApplicantEmailTemplatePreviewDto> GetPreviewAsync(Guid applicantId, Guid templateId)
    {
        await emailAccessChecker.CheckOwnerAsync(Guid.Empty, applicantId, NotificationsPermissions.Email.Send);
        var template = await emailAccessChecker.CheckTemplateAsync(templateId, applicantId);
        var applicant = await applicantRepository.GetAsync(applicantId);
        var values = ScheduledNotificationHelper.BuildApplicantTokenValues(applicant);
        var emailTo = string.Empty;
        if (string.Equals(template.RecipientCategory, "Internal", StringComparison.OrdinalIgnoreCase))
        {
            emailTo = await notificationHelper.GetInternalRecipientEmailAddressesAsync(
                new ScheduledNotification { RecipientCategory = template.RecipientCategory, RecipientIdentifier = template.RecipientIdentifier },
                emailGroupsAppService, emailGroupUsersAppService, identityUserIntegrationService);
        }
        else if (string.Equals(template.RecipientCategory, "External", StringComparison.OrdinalIgnoreCase))
        {
            var contacts = await contactQueryService.GetByApplicantIdAsync(applicantId);
            emailTo = ApplicantEmailRecipientResolver.Resolve(template.RecipientIdentifier, contacts.Contacts);
        }

        return new ApplicantEmailTemplatePreviewDto
        {
            Id = template.Id,
            Name = template.Name,
            Subject = ScheduledNotificationHelper.RenderTemplate(template.Subject, values),
            // Applicant data is text, not template HTML. Encode substitutions without encoding the template markup.
            Body = ScheduledNotificationHelper.RenderTemplate(template.BodyHTML,
                values.ToDictionary(x => x.Key, x => WebUtility.HtmlEncode(x.Value), StringComparer.OrdinalIgnoreCase)),
            SendFrom = template.SendFrom,
            EmailTo = emailTo
        };
    }
}
