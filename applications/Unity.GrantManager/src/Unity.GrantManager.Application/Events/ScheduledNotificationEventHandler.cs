using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Notifications;
using Unity.Notifications.EmailGroups;
using Unity.Notifications.Emails;
using Unity.Notifications.Events;
using Unity.Notifications.Settings;
using Unity.Notifications.Templates;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Features;
using Volo.Abp.Identity.Integration;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Settings;

namespace Unity.GrantManager.Events
{
    internal class ScheduledNotificationEventHandler(
        IRepository<ScheduledNotification, Guid> scheduledNotificationRepository,
        IApplicationRepository applicationRepository,
        IApplicantAgentRepository applicantAgentRepository,
        ILocalEventBus localEventBus,
        ITemplateService templateService,
        IEmailGroupsAppService emailGroupsAppService,
        IEmailGroupUsersAppService emailGroupUsersAppService,
        IIdentityUserIntegrationService identityUserIntegrationService,
        IFeatureChecker featureChecker,
        ISettingProvider settingProvider,
        ICurrentTenant currentTenant,
        ScheduledNotificationHelper scheduledNotificationHelper,
        ILogger<ScheduledNotificationEventHandler> logger)
        : ILocalEventHandler<ApplicationChangedEvent>, ITransientDependency
    {
        public async Task HandleEventAsync(ApplicationChangedEvent eventData)
        {
            if (!await featureChecker.IsEnabledAsync("Unity.Notifications"))
            {
                return;
            }

            try
            {
                // includeDetails: true to load Applicant navigation property for token substitution
                var application = await applicationRepository.GetAsync(eventData.ApplicationId, includeDetails: true);
                if (application == null)
                {
                    logger.LogWarning("ScheduledNotificationEventHandler: Application {ApplicationId} not found.", eventData.ApplicationId);
                    return;
                }

                // Find all active event-based scheduled notifications for this form that match the new application status
                var notifications = (await scheduledNotificationRepository.GetListAsync(
                    n => n.FormId == application.ApplicationFormId
                      && n.TriggerType == "Event"
                      && n.IsActive
                      && n.ApplicationStatusId == application.ApplicationStatusId))
                    .ToList();

                if (notifications.Count == 0)
                {
                    return;
                }

                var defaultFromAddress = await settingProvider.GetOrNullAsync(NotificationsSettings.Mailing.DefaultFromAddress);
                string emailFrom = defaultFromAddress ?? "NoReply@gov.bc.ca";

                // Fetch applicant agent once for the whole batch - used for contact tokens and recipient resolution
                var applicantAgent = await applicantAgentRepository.FirstOrDefaultAsync(a => a.ApplicationId == application.Id);

                foreach (var notification in notifications)
                {
                    await ProcessNotificationAsync(notification, application, applicantAgent, emailFrom);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ScheduledNotificationEventHandler: Error processing event for application {ApplicationId}.", eventData.ApplicationId);
            }
        }

        private async Task ProcessNotificationAsync(
            ScheduledNotification notification,
            Application application,
            ApplicantAgent? applicantAgent,
            string emailFrom)
        {
            if (string.IsNullOrWhiteSpace(notification.RecipientCategory) ||
                string.IsNullOrWhiteSpace(notification.RecipientIdentifier))
            {
                logger.LogWarning(
                    "ScheduledNotificationEventHandler: Scheduled notification {NotificationId} has no recipient category or identifier, skipping.",
                    notification.Id);
                return;
            }

            var template = await templateService.GetTemplateById(notification.EmailTemplateId);
            if (template == null)
            {
                logger.LogWarning(
                    "ScheduledNotificationEventHandler: Email template {TemplateId} not found for scheduled notification {NotificationId}, skipping.",
                    notification.EmailTemplateId, notification.Id);
                return;
            }

            var tokenValues = ScheduledNotificationHelper.BuildTokenValues(application, applicantAgent);
            string subject = ScheduledNotificationHelper.RenderTemplate(template.Subject, tokenValues);
            string body = ScheduledNotificationHelper.RenderTemplate(
                string.IsNullOrWhiteSpace(template.BodyHTML) ? template.BodyText : template.BodyHTML,
                tokenValues);

            // Build email event with common properties
            var emailEvent = new EmailNotificationEvent
            {
                Action = EmailAction.SendEventDriven,
                TenantId = currentTenant.Id,
                ApplicationId = application.Id,
                ScheduledNotificationId = notification.Id,
                TemplateId = template.Id,
                EmailTemplateName = template.Name,
                Subject = subject,
                Body = body,
                EmailFrom = emailFrom,
                RetryAttempts = 0
            };

            // Publish based on recipient category
            if (string.Equals(notification.RecipientCategory, "Internal", StringComparison.OrdinalIgnoreCase))
            {
                await scheduledNotificationHelper.PublishToEmailGroupAsync(
                    emailGroupsAppService, emailGroupUsersAppService, identityUserIntegrationService,
                    localEventBus, notification, emailEvent);
            }
            else if (string.Equals(notification.RecipientCategory, "External", StringComparison.OrdinalIgnoreCase))
            {
                await scheduledNotificationHelper.PublishToExternalRecipientAsync(
                    localEventBus, notification, application, applicantAgent, emailEvent);
            }
            else
            {
                logger.LogWarning(
                    "ScheduledNotificationEventHandler: Unknown RecipientCategory '{Category}' on notification {NotificationId}, skipping.",
                    notification.RecipientCategory, notification.Id);
            }
        }
    }
}
