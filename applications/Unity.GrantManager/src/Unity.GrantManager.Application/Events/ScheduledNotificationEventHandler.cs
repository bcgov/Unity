using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Notifications;
using Unity.Notifications.EmailGroups;
using Unity.Notifications.Emails;
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
        IRepository<EmailLog, Guid> emailLogRepository,
        IRepository<ScheduledNotificationTracking, Guid> trackingRepository,
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


            if (string.Equals(notification.RecipientCategory, "Internal", StringComparison.OrdinalIgnoreCase))
            {
                await ScheduledNotificationHelper.PublishToEmailGroupAsync(
                    emailGroupsAppService, emailGroupUsersAppService, identityUserIntegrationService,
                    currentTenant, localEventBus, notification, application.Id, template, subject, body, emailFrom, logger);
            }
            else if (string.Equals(notification.RecipientCategory, "External", StringComparison.OrdinalIgnoreCase))
            {
                await ScheduledNotificationHelper.PublishToExternalRecipientAsync(
                    currentTenant, localEventBus, notification, application, applicantAgent, template, subject, body, emailFrom, logger);
            }
            else
            {
                logger.LogWarning(
                    "ScheduledNotificationEventHandler: Unknown RecipientCategory '{Category}' on notification {NotificationId}, skipping.",
                    notification.RecipientCategory, notification.Id);
            }
        }

        private async Task CreateDraftEmailAsync(
            ScheduledNotification notification,
            Application application,
            EmailTemplate template,
            string subject,
            string body,
            string emailFrom,
            string toAddress = "")
        {
            try
            {
                var draftEmail = new EmailLog
                {
                    TenantId = currentTenant.Id,
                    ScheduledNotificationId = notification.Id,
                    ApplicationId = application.Id,
                    ApplicantId = application.ApplicantId,
                    FromAddress = emailFrom,
                    ToAddress = toAddress, // Populated if recipient is available
                    Subject = subject,
                    Body = body,
                    BodyType = "HTML",
                    Priority = "Normal",
                    TemplateName = template.Name,
                    Tag = "ScheduledNotificationEventHandler", // Identifies source as event handler
                    Status = EmailStatus.Draft,
                    RetryAttempts = 0
                };

                await emailLogRepository.InsertAsync(draftEmail);
                
                // Create tracking record to mark that this notification has been processed for this application
                if (!string.IsNullOrEmpty(notification.DateField))
                {
                    var tracking = new ScheduledNotificationTracking(
                        Guid.NewGuid(),
                        application.Id,
                        notification.Id,
                        notification.DateField,
                        DateTime.UtcNow);
                    await trackingRepository.InsertAsync(tracking);
                }
                
                logger.LogInformation(
                    "ScheduledNotificationEventHandler: Draft email created for scheduled notification {NotificationId} with subject '{Subject}'.",
                    notification.Id, subject);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "ScheduledNotificationEventHandler: Error creating draft email for scheduled notification {NotificationId}.",
                    notification.Id);
            }
        }
    }
}
