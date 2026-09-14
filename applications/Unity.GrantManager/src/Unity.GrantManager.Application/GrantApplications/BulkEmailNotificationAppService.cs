using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Notifications.Email;
using Unity.Modules.Shared.Utils;
using Unity.Notifications.Emails;
using Unity.Notifications.Permissions;
using Unity.Notifications.Templates;
using Volo.Abp;
using Volo.Abp.Users;

namespace Unity.GrantManager.GrantApplications
{
    // The right-panel editor reuses EmailsWidget, whose edit fieldset/Save/attachment handling all require
    // Notifications.Email.Send (a sibling permission, not a parent of SendBulk) — so both are required here
    // too, not just SendBulk, or a user granted SendBulk alone could reach this API into a state the UI can't
    // actually support. Two stacked [Authorize] attributes compose as AND per standard ASP.NET Core semantics.
    [Authorize(NotificationsPermissions.Email.SendBulk)]
    [Authorize(NotificationsPermissions.Email.Send)]
    public class BulkEmailNotificationAppService(
        IApplicationRepository applicationRepository,
        IEmailLogsRepository emailLogsRepository,
        IEmailLogAttachmentRepository emailLogAttachmentRepository,
        IEmailAppService emailAppService,
        IExternalUserLookupServiceProvider externalUserLookupServiceProvider,
        ITemplateService templateService,
        IConfiguration configuration) : GrantManagerAppService, IBulkEmailNotificationAppService
    {
        /// <summary>
        /// Get applications for bulk email with added draft validation information
        /// </summary>
        /// <param name="applicationGuids"></param>
        /// <returns></returns>
        public async Task<List<BulkEmailNotificationDto>> GetApplicationsForBulkEmail(Guid[] applicationGuids)
        {
            var applications = await applicationRepository.GetListByIdsAsync(applicationGuids);
            var draftEmails = await emailLogsRepository.GetByApplicationIdsAndStatusAsync([.. applicationGuids], EmailStatus.Draft);
            var draftsByApplication = draftEmails.GroupBy(e => e.ApplicationId).ToDictionary(g => g.Key, g => g.ToList());
            var draftAuthorsById = await GetDraftAuthorsAsync(draftEmails);

            var applicationsForEmail = new List<BulkEmailNotificationDto>();
            foreach (var application in applications)
            {
                draftsByApplication.TryGetValue(application.Id, out var drafts);
                applicationsForEmail.Add(MapBulkEmailNotification(application, drafts ?? [], draftAuthorsById));
            }

            return applicationsForEmail;
        }

        /// <summary>
        /// Re-validate a single application's draft state (e.g. after an in-panel Save) without re-fetching the whole batch
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        public async Task<BulkEmailNotificationDto> RevalidateApplicationForBulkEmail(Guid applicationId)
        {
            var applications = await applicationRepository.GetListByIdsAsync([applicationId]);
            var application = applications.Single();
            var drafts = await emailLogsRepository.GetByApplicationIdsAndStatusAsync([applicationId], EmailStatus.Draft);
            var draftAuthorsById = await GetDraftAuthorsAsync(drafts);

            return MapBulkEmailNotification(application, drafts, draftAuthorsById);
        }

        public async Task<List<ComposeEmailApplicationDto>> GetApplicationsForComposeEmail(Guid[] applicationGuids)
        {
            var applications = await applicationRepository.GetListByIdsAsync(applicationGuids);
            var applicationsById = applications.ToDictionary(application => application.Id);

            return applicationGuids
                .Distinct()
                .Where(applicationsById.ContainsKey)
                .Select(applicationId => MapComposeEmailApplication(applicationsById[applicationId]))
                .ToList();
        }

        private async Task<Dictionary<Guid, IUserData>> GetDraftAuthorsAsync(List<EmailLog> drafts)
        {
            var creatorIds = drafts
                .Where(d => d.CreatorId.HasValue)
                .Select(d => d.CreatorId!.Value)
                .Distinct();

            var draftAuthorsById = new Dictionary<Guid, IUserData>();
            foreach (var creatorId in creatorIds)
            {
                var userInfo = await externalUserLookupServiceProvider.FindByIdAsync(creatorId);
                if (userInfo != null)
                {
                    draftAuthorsById[creatorId] = userInfo;
                }
            }

            return draftAuthorsById;
        }

        /// <summary>
        /// Send bulk email notifications for the given batch of draft emails
        /// </summary>
        /// <param name="batchApplicationsToEmail"></param>
        /// <returns></returns>
        public async Task<BulkEmailNotificationResultDto> SendBulkEmailNotifications(List<BulkEmailNotificationDto> batchApplicationsToEmail)
        {
            var bulkEmailResult = new BulkEmailNotificationResultDto();

            // Fail the whole batch up front if notifications are disabled, rather than reporting false successes
            // for emails that SendAsync would silently drop (it always returns true after publishing the event).
            if (!await FeatureChecker.IsEnabledAsync("Unity.Notifications"))
            {
                foreach (var applicationToEmail in batchApplicationsToEmail)
                {
                    bulkEmailResult.Failures.Add(new KeyValuePair<string, string>(applicationToEmail.ReferenceNo, "Email notifications are currently disabled."));
                }
                return bulkEmailResult;
            }

            // We send individually here so that a failure on one application does not block the rest of the batch
            foreach (var applicationToEmail in batchApplicationsToEmail)
            {
                try
                {
                    if (!applicationToEmail.EmailId.HasValue)
                    {
                        throw new UserFriendlyException("No draft email was found for this application.");
                    }

                    // Re-fetch the draft fresh (defense-in-depth: it may have changed since the modal opened)
                    var draft = await emailLogsRepository.GetAsync(applicationToEmail.EmailId.Value);
                    if (draft.Status != EmailStatus.Draft)
                    {
                        throw new UserFriendlyException("This email is no longer a draft.");
                    }

                    // The posted ApplicationId is client-controlled (hidden form field) — never trust it for
                    // authorization-relevant writes. Confirm it still matches the draft's real owning application
                    // and use the draft's own ApplicationId, not the posted one, when sending.
                    if (draft.ApplicationId != applicationToEmail.ApplicationId)
                    {
                        throw new UserFriendlyException("This draft no longer matches the selected application.");
                    }

                    // Re-check the "exactly one draft" invariant fresh at send time: this endpoint can be reached
                    // directly (bypassing the modal's GetApplicationsForBulkEmail check), and another draft may
                    // have been created for this application after the modal was loaded.
                    var currentDrafts = await emailLogsRepository.GetByApplicationIdsAndStatusAsync([draft.ApplicationId], EmailStatus.Draft);
                    if (currentDrafts.Count != 1)
                    {
                        throw new UserFriendlyException("Multiple draft emails found for this application. Please retain only one draft before proceeding.");
                    }

                    // A non-blank ToAddress can still parse to zero recipients (e.g. ";" or ",")  — the same check
                    // the send pipeline itself uses. Catch that here instead of reporting a false success: SendAsync
                    // always returns true, but the handler silently drops emails with no parseable recipients.
                    if (draft.ToAddress.ParseEmailList() is not { Count: > 0 })
                    {
                        throw new UserFriendlyException("Draft email is missing a To address. Please update the draft before proceeding.");
                    }

                    // Neither the per-upload size gate nor the modal's own UI check can catch every path an
                    // attachment can arrive by (e.g. copying a template's attachments onto a draft applies no
                    // size check at all), and this endpoint can be reached directly regardless of what the UI
                    // showed. FileSize is recorded on upload/copy, not derived from S3, so this is a cheap
                    // in-database check, not a storage round-trip.
                    var attachments = await emailLogAttachmentRepository.GetByEmailLogIdAsync(draft.Id);
                    var totalAttachmentMb = attachments.Sum(a => a.FileSize) * 0.000001;
                    // Same TryParse-with-fallback as AttachmentController's own total-size check: a missing,
                    // empty, malformed, or non-positive config value falls back to 25 rather than throwing —
                    // double.Parse would otherwise fail every row in the batch on a bad config value alone.
                    if (!double.TryParse(configuration["S3:EmailAttachmentsTotalMaxFileSize"], out double maxAttachmentMb) || maxAttachmentMb <= 0)
                    {
                        maxAttachmentMb = 25;
                    }
                    if (totalAttachmentMb > maxAttachmentMb)
                    {
                        throw new UserFriendlyException($"The total size of all attachments ({totalAttachmentMb:F2} MB) exceeds the maximum allowed {maxAttachmentMb} MB. Please remove one or more attachments before proceeding.");
                    }

                    await emailAppService.SendAsync(new CreateEmailDto
                    {
                        EmailId = draft.Id,
                        ApplicationId = draft.ApplicationId,
                        EmailTo = draft.ToAddress,
                        EmailFrom = draft.FromAddress,
                        EmailSubject = draft.Subject,
                        EmailBody = draft.Body,
                        EmailCC = draft.CC,
                        EmailBCC = draft.BCC,
                        EmailTemplateName = draft.TemplateName,
                        SendOnDateTime = draft.SendOnDateTime
                    });

                    bulkEmailResult.Successes.Add(applicationToEmail.ReferenceNo);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error sending bulk email notification for application with ID: {ApplicationId} and ReferenceNo: {ReferenceNo}",
                        applicationToEmail.ApplicationId,
                        applicationToEmail.ReferenceNo);

                    bulkEmailResult.Failures.Add(new KeyValuePair<string, string>(applicationToEmail.ReferenceNo, ex.Message));
                }
            }

            return bulkEmailResult;
        }

        public async Task<BulkEmailNotificationResultDto> SendComposedBulkEmails(ComposeBulkEmailRequestDto request)
        {
            var result = new BulkEmailNotificationResultDto();
            var emails = request?.Emails ?? [];

            if (emails.Count == 0)
            {
                return result;
            }

            if (emails.Count > BatchApprovalConsts.MaxBatchCount)
            {
                throw new UserFriendlyException($"A maximum of {BatchApprovalConsts.MaxBatchCount} applications can be emailed at once.");
            }

            var applications = await applicationRepository.GetListByIdsAsync([.. emails.Select(email => email.ApplicationId).Distinct()]);
            var applicationsById = applications.ToDictionary(application => application.Id);
            var processedApplicationIds = new HashSet<Guid>();
            Dictionary<Guid, EmailTemplate>? templatesById = null;

            if (!await FeatureChecker.IsEnabledAsync("Unity.Notifications"))
            {
                foreach (var email in emails)
                {
                    var referenceNo = applicationsById.TryGetValue(email.ApplicationId, out var application)
                        ? application.ReferenceNo
                        : email.ApplicationId.ToString();
                    result.Failures.Add(new KeyValuePair<string, string>(referenceNo, "Email notifications are currently disabled."));
                }

                return result;
            }

            foreach (var email in emails)
            {
                var referenceNo = applicationsById.TryGetValue(email.ApplicationId, out var application)
                    ? application.ReferenceNo
                    : email.ApplicationId.ToString();

                try
                {
                    if (application == null)
                    {
                        throw new UserFriendlyException("The selected application could not be found.");
                    }

                    if (!processedApplicationIds.Add(application.Id))
                    {
                        throw new UserFriendlyException("The selected application occurs more than once in this batch.");
                    }

                    ComposedEmailValidator.ValidateFields(email);

                    var templateName = string.Empty;
                    if (email.TemplateId.HasValue)
                    {
                        templatesById ??= (await templateService.GetTemplatesByTenant())
                            .ToDictionary(template => template.Id);
                        if (!templatesById.TryGetValue(email.TemplateId.Value, out var template))
                        {
                            throw new UserFriendlyException("The selected email template no longer exists.");
                        }

                        await ValidateTemplateAttachmentSizeAsync(template.Id);
                        templateName = template.Name;
                    }

                    await emailAppService.SendAsync(new CreateEmailDto
                    {
                        ApplicationId = application.Id,
                        EmailTo = email.EmailTo,
                        EmailCC = email.EmailCC,
                        EmailBCC = email.EmailBCC,
                        EmailFrom = email.EmailFrom,
                        EmailSubject = email.EmailSubject,
                        EmailBody = email.EmailBody,
                        TemplateId = email.TemplateId,
                        EmailTemplateName = templateName
                    });

                    result.Successes.Add(referenceNo);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex,
                        "Error sending composed bulk email for application with ID {ApplicationId} and ReferenceNo {ReferenceNo}",
                        email.ApplicationId,
                        referenceNo);
                    result.Failures.Add(new KeyValuePair<string, string>(referenceNo, ex.Message));
                }
            }

            return result;
        }

        private async Task ValidateTemplateAttachmentSizeAsync(Guid templateId)
        {
            var attachments = await emailLogAttachmentRepository.GetByTemplateIdAsync(templateId);
            ComposedEmailValidator.ValidateAttachmentSize(
                attachments.Sum(attachment => attachment.FileSize),
                GetMaxAttachmentMb());
        }

        private double GetMaxAttachmentMb()
        {
            return double.TryParse(configuration["S3:EmailAttachmentsTotalMaxFileSize"], out var maxAttachmentMb)
                && maxAttachmentMb > 0
                    ? maxAttachmentMb
                    : 25;
        }

        /// <summary>
        /// Map the application to a BulkEmailNotificationDto with validation messages based on its draft emails
        /// </summary>
        /// <param name="application"></param>
        /// <param name="drafts"></param>
        /// <returns></returns>
        private static BulkEmailNotificationDto MapBulkEmailNotification(Application application, List<EmailLog> drafts, Dictionary<Guid, IUserData> draftAuthorsById)
        {
            var validationMessages = new List<string>();
            Guid? emailId = null;
            string? emailSubject = null;
            string? createdByName = null;
            DateTime? lastModified = null;

            if (drafts.Count == 0)
            {
                validationMessages.Add("NO_DRAFT_FOUND");
            }
            else if (drafts.Count > 1)
            {
                validationMessages.Add("MULTIPLE_DRAFTS_FOUND");
            }
            else
            {
                var draft = drafts[0];
                emailId = draft.Id;
                emailSubject = draft.Subject;
                lastModified = draft.LastModificationTime ?? draft.CreationTime;

                if (draft.CreatorId.HasValue && draftAuthorsById.TryGetValue(draft.CreatorId.Value, out var author))
                {
                    createdByName = $"{author.Name} {author.Surname}".Trim();
                }

                if (string.IsNullOrWhiteSpace(draft.Subject))
                {
                    validationMessages.Add("MISSING_SUBJECT");
                }
                if (draft.ToAddress.ParseEmailList() is not { Count: > 0 })
                {
                    validationMessages.Add("MISSING_TO_ADDRESS");
                }
                if (string.IsNullOrWhiteSpace(draft.FromAddress))
                {
                    validationMessages.Add("MISSING_FROM_ADDRESS");
                }
                if (string.IsNullOrWhiteSpace(draft.Body))
                {
                    validationMessages.Add("MISSING_BODY");
                }
            }

            return new BulkEmailNotificationDto()
            {
                ApplicationId = application.Id,
                EmailId = emailId,
                EmailSubject = emailSubject,
                ReferenceNo = application.ReferenceNo,
                ApplicantName = application.Applicant?.ApplicantName ?? string.Empty,
                ApplicationStatus = application.ApplicationStatus.InternalStatus,
                FormName = application.ApplicationForm?.ApplicationFormName ?? string.Empty,
                ApprovedAmount = application.ApprovedAmount,
                DecisionDate = application.FinalDecisionDate,
                CreatedByName = createdByName,
                LastModified = lastModified,
                ValidationMessages = validationMessages,
                IsValid = validationMessages.Count == 0
            };
        }

        private static ComposeEmailApplicationDto MapComposeEmailApplication(Application application)
        {
            return new ComposeEmailApplicationDto
            {
                ApplicationId = application.Id,
                ReferenceNo = application.ReferenceNo,
                ApplicantName = application.Applicant?.ApplicantName ?? string.Empty,
                ApplicationStatus = application.ApplicationStatus.InternalStatus,
                FormName = application.ApplicationForm?.ApplicationFormName ?? string.Empty,
                ApplicantEmail = application.ApplicantAgent?.Email ?? string.Empty,
                ApprovedAmount = application.ApprovedAmount,
                DecisionDate = application.FinalDecisionDate
            };
        }
    }
}
