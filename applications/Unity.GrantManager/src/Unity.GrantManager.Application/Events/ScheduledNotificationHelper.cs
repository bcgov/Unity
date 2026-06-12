using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Notifications;
using Unity.Notifications.EmailGroups;
using Unity.Notifications.Emails;
using Unity.Notifications.Events;
using Unity.Notifications.Templates;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Identity.Integration;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Events
{
    /// <summary>
    /// Helper service for scheduled notification processing - shared logic between
    /// event-based (ScheduledNotificationEventHandler) and date-based (DateBasedScheduledNotificationJob) handlers.
    /// </summary>
    public partial class ScheduledNotificationHelper
    {
        /// <summary>
        /// Generated regex for template token replacement - compiled at compile-time.
        /// Matches {{token}} patterns where token is one or more word characters.
        /// </summary>
        [GeneratedRegex(@"\{\{(\w+)\}\}")]
        private static partial Regex TokenRegex();
        /// <summary>
        /// Builds the token-to-value dictionary from the application and applicant agent,
        /// matching the MapTo paths defined in the TemplateVariable seed data.
        /// </summary>
        public static Dictionary<string, string> BuildTokenValues(Application application, ApplicantAgent? applicantAgent)
        {
            Applicant? applicant = null;
            try { applicant = application.Applicant; } catch { /* navigation property may not be loaded */ }

            ApplicationStatus? applicationStatus = null;
            try { applicationStatus = application.ApplicationStatus; } catch { /* navigation property may not be loaded */ }

            ApplicationForm? applicationForm = null;
            try { applicationForm = application.ApplicationForm; } catch { /* navigation property may not be loaded */ }

            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["applicant_name"]              = applicant?.ApplicantName ?? string.Empty,
                ["applicant_id"]                = applicant?.UnityApplicantId ?? string.Empty,
                ["organization_name"]           = applicant?.OrgName ?? applicant?.NonRegisteredBusinessName ?? string.Empty,
                ["submission_number"]           = application.ReferenceNo,
                ["submission_date"]             = application.SubmissionDate.ToString("yyyy-MM-dd"),
                ["status"]                      = applicationStatus?.StatusCode.ToString() ?? string.Empty,
                ["approved_amount"]             = application.ApprovedAmount.ToString("N2"),
                ["requested_amount"]            = application.RequestedAmount.ToString("N2"),
                ["recommended_amount"]          = application.RecommendedAmount.ToString("N2"),
                ["approval_date"]               = application.FinalDecisionDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                ["decline_rationale"]           = application.DeclineRational ?? string.Empty,
                ["community"]                   = application.Community ?? string.Empty,
                ["project_name"]                = application.ProjectName,
                ["project_summary"]             = application.ProjectSummary ?? string.Empty,
                ["project_start_date"]          = application.ProjectStartDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                ["project_end_date"]            = application.ProjectEndDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                ["signing_authority_full_name"] = application.SigningAuthorityFullName ?? string.Empty,
                ["signing_authority_title"]     = application.SigningAuthorityTitle ?? string.Empty,
                ["contact_full_name"]           = applicantAgent?.Name ?? string.Empty,
                ["contact_title"]               = applicantAgent?.Title ?? string.Empty,
                ["category"]                    = applicationForm?.Category ?? string.Empty,
            };
        }

        /// <summary>
        /// Replaces all {{token}} occurrences in the template text with their resolved values.
        /// Unknown tokens are left as-is.
        /// </summary>
        public static string RenderTemplate(string template, Dictionary<string, string> tokenValues)
        {
            if (string.IsNullOrEmpty(template)) return template;

            return TokenRegex().Replace(template, match =>
            {
                var token = match.Groups[1].Value;
                return tokenValues.TryGetValue(token, out var value) ? value : match.Value;
            });
        }

        /// <summary>
        /// Detects unresolved template parameters (tokens that couldn't be replaced).
        /// Returns a list of missing parameter names found in the subject or body.
        /// </summary>
        public static List<string> GetMissingParameters(string subject, string body)
        {
            var missingParams = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var combinedText = $"{subject} {body}";
            
            var matches = TokenRegex().Matches(combinedText);
            foreach (Match match in matches)
            {
                var token = match.Groups[1].Value;
                missingParams.Add(token);
            }

            return missingParams.ToList();
        }

        /// <summary>
        /// Internal category: RecipientIdentifier is a comma-separated list of email group names.
        /// Resolve the groups, their users, and their identity email addresses.
        /// </summary>
        public static async Task PublishToEmailGroupAsync(
            IEmailGroupsAppService emailGroupsAppService,
            IEmailGroupUsersAppService emailGroupUsersAppService,
            IIdentityUserIntegrationService identityUserIntegrationService,
            ICurrentTenant currentTenant,
            ILocalEventBus localEventBus,
            ScheduledNotification notification,
            Guid applicationId,
            EmailTemplate template,
            string subject,
            string body,
            string emailFrom,
            ILogger logger)
        {
            var allGroups = await emailGroupsAppService.GetListAsync();
            
            // Split comma-separated recipient identifiers and process each
            var recipientIdentifiers = notification.RecipientIdentifier?
                .Split(',')
                .Select(r => r.Trim())
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .ToList() ?? new List<string>();

            if (recipientIdentifiers.Count == 0)
            {
                logger.LogWarning(
                    "ScheduledNotificationHelper: No recipient identifiers provided for notification {NotificationId}, skipping.",
                    notification.Id);
                return;
            }

            var emailAddresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var recipientId in recipientIdentifiers)
            {
                var group = allGroups.FirstOrDefault(g =>
                    string.Equals(g.Name, recipientId, StringComparison.OrdinalIgnoreCase));

                if (group == null)
                {
                    logger.LogWarning(
                        "ScheduledNotificationHelper: Email group '{GroupName}' not found for notification {NotificationId}, skipping group.",
                        recipientId, notification.Id);
                    continue;
                }

                var groupUsers = await emailGroupUsersAppService.GetEmailGroupUsersByGroupIdAsync(group.Id);
                if (groupUsers.Count == 0)
                {
                    logger.LogWarning(
                        "ScheduledNotificationHelper: Email group '{GroupName}' has no members for notification {NotificationId}, skipping group.",
                        recipientId, notification.Id);
                    continue;
                }

                foreach (var groupUser in groupUsers)
                {
                    var user = await identityUserIntegrationService.FindByIdAsync(groupUser.UserId);
                    if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                    {
                        emailAddresses.Add(user.Email);
                    }
                }
            }

            if (emailAddresses.Count == 0)
            {
                logger.LogWarning(
                    "ScheduledNotificationHelper: No resolvable email addresses in any groups for notification {NotificationId}, skipping.",
                    notification.Id);
                return;
            }

            await localEventBus.PublishAsync(new EmailNotificationEvent
            {
                Action = EmailAction.SendCustom,
                TenantId = currentTenant.Id,
                ApplicationId = applicationId,
                TemplateId = template.Id,
                EmailTemplateName = template.Name,
                Subject = subject,
                Body = body,
                EmailAddressList = emailAddresses.ToList(),
                EmailFrom = emailFrom,
                RetryAttempts = 0
            });

            logger.LogInformation(
                "ScheduledNotificationHelper: Published EmailNotificationEvent for application {ApplicationId}, template {TemplateName}, groups '{GroupNames}' ({Count} recipients).",
                applicationId, template.Name, notification.RecipientIdentifier, emailAddresses.Count);
        }

        /// <summary>
        /// External category: RecipientIdentifier is a comma-separated list of "ApplicationContact" and/or "SigningAuthority".
        /// Resolve the emails from the application record for each recipient type.
        /// </summary>
        public static async Task PublishToExternalRecipientAsync(
            ICurrentTenant currentTenant,
            ILocalEventBus localEventBus,
            ScheduledNotification notification,
            Application application,
            ApplicantAgent? applicantAgent,
            EmailTemplate template,
            string subject,
            string body,
            string emailFrom,
            ILogger logger)
        {
            // Split comma-separated recipient identifiers and collect unique email addresses
            var recipientIdentifiers = notification.RecipientIdentifier?
                .Split(',')
                .Select(r => r.Trim())
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .ToList() ?? new List<string>();

            if (recipientIdentifiers.Count == 0)
            {
                logger.LogWarning(
                    "ScheduledNotificationHelper: No recipient identifiers provided for notification {NotificationId}, skipping.",
                    notification.Id);
                return;
            }

            var emailAddresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var recipientId in recipientIdentifiers)
            {
                string? emailTo = null;

                if (string.Equals(recipientId, "ApplicationContact", StringComparison.OrdinalIgnoreCase))
                {
                    emailTo = applicantAgent?.Email;
                }
                else if (string.Equals(recipientId, "SigningAuthority", StringComparison.OrdinalIgnoreCase))
                {
                    emailTo = application.SigningAuthorityEmail;
                }
                else
                {
                    logger.LogWarning(
                        "ScheduledNotificationHelper: Unknown external RecipientIdentifier '{Identifier}' for notification {NotificationId}, skipping identifier.",
                        recipientId, notification.Id);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(emailTo))
                {
                    logger.LogWarning(
                        "ScheduledNotificationHelper: No email address found for '{Identifier}' on application {ApplicationId}, skipping identifier.",
                        recipientId, application.Id);
                    continue;
                }

                emailAddresses.Add(emailTo);
            }

            if (emailAddresses.Count == 0)
            {
                logger.LogWarning(
                    "ScheduledNotificationHelper: No resolvable email addresses for any recipients on application {ApplicationId}, skipping notification {NotificationId}.",
                    application.Id, notification.Id);
                return;
            }

            await localEventBus.PublishAsync(new EmailNotificationEvent
            {
                Action = EmailAction.SendCustom,
                TenantId = currentTenant.Id,
                ApplicationId = application.Id,
                TemplateId = template.Id,
                EmailTemplateName = template.Name,
                Subject = subject,
                Body = body,
                EmailAddressList = emailAddresses.ToList(),
                EmailFrom = emailFrom,
                RetryAttempts = 0
            });

            logger.LogInformation(
                "ScheduledNotificationHelper: Published EmailNotificationEvent for application {ApplicationId}, template {TemplateName}, recipients '{RecipientIdentifiers}' ({Count} email addresses).",
                application.Id, template.Name, notification.RecipientIdentifier, emailAddresses.Count);
        }           
    }
}
