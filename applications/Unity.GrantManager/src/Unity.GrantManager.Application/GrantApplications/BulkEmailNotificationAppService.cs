using Microsoft.AspNetCore.Authorization;
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
using Volo.Abp;

namespace Unity.GrantManager.GrantApplications
{
    [Authorize(NotificationsPermissions.Email.SendBulk)]
    public class BulkEmailNotificationAppService(
        IApplicationRepository applicationRepository,
        IEmailLogsRepository emailLogsRepository,
        IEmailAppService emailAppService) : GrantManagerAppService, IBulkEmailNotificationAppService
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

            var applicationsForEmail = new List<BulkEmailNotificationDto>();
            foreach (var application in applications)
            {
                draftsByApplication.TryGetValue(application.Id, out var drafts);
                applicationsForEmail.Add(MapBulkEmailNotification(application, drafts ?? []));
            }

            return applicationsForEmail;
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

        /// <summary>
        /// Map the application to a BulkEmailNotificationDto with validation messages based on its draft emails
        /// </summary>
        /// <param name="application"></param>
        /// <param name="drafts"></param>
        /// <returns></returns>
        private static BulkEmailNotificationDto MapBulkEmailNotification(Application application, List<EmailLog> drafts)
        {
            var validationMessages = new List<string>();
            Guid? emailId = null;
            string? emailSubject = null;

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
                ValidationMessages = validationMessages,
                IsValid = validationMessages.Count == 0
            };
        }
    }
}
