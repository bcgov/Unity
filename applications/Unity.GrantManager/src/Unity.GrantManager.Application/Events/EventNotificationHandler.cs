using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Notifications;
using Unity.Notifications.EmailGroups;
using Unity.Notifications.Emails;
using Unity.Notifications.Events;
using Unity.Notifications.Settings;
using Unity.Notifications.Templates;
using Unity.Payments.Events;
using Unity.Payments.Enums;
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
    internal class EventNotificationHandler(
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
        ILogger<EventNotificationHandler> logger)
        : ILocalEventHandler<ApplicationChangedEvent>, ILocalEventHandler<PaymentStatusChangedEvent>, ITransientDependency
    {
        public async Task HandleEventAsync(ApplicationChangedEvent eventData)
        {
            if (!await featureChecker.IsEnabledAsync("Unity.Notifications"))
            {
                return;
            }

            try
            {
                var application = await applicationRepository.GetAsync(eventData.ApplicationId, includeDetails: true);
                if (application == null)
                {
                    logger.LogWarning("EventNotificationHandler: Application {ApplicationId} not found.", eventData.ApplicationId);
                    return;
                }

                var notifications = (await scheduledNotificationRepository.GetListAsync(
                    ApplicationEventNotificationFilter(application.ApplicationFormId, application.ApplicationStatusId)))
                    .ToList();

                if (notifications.Count == 0)
                {
                    return;
                }

                var emailFrom = await GetDefaultFromAddressAsync();
                var applicantAgent = await applicantAgentRepository.FirstOrDefaultAsync(a => a.ApplicationId == application.Id);

                foreach (var notification in notifications)
                {
                    await ProcessNotificationAsync(notification, application, applicantAgent, emailFrom);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "EventNotificationHandler: Error processing event for application {ApplicationId}.", eventData.ApplicationId);
            }
        }

        internal static Expression<Func<ScheduledNotification, bool>> ApplicationEventNotificationFilter(
            Guid formId,
            Guid applicationStatusId)
        {
            return n => n.FormId == formId
                     && n.TriggerType == "Event"
                     && n.IsActive
                     && (n.Module == null || n.Module == "Application")
                     && n.ApplicationStatusId == applicationStatusId;
        }

        internal static Expression<Func<ScheduledNotification, bool>> PaymentEventNotificationFilter(
            Guid formId,
            PaymentRequestStatus paymentStatus,
            string? casPaymentStatus)
        {
            return n => n.FormId == formId
                     && n.TriggerType == "Event"
                     && n.IsActive
                     && n.Module == "Payment"
                     && (n.EventType == paymentStatus.ToString() ||
                         (!string.IsNullOrWhiteSpace(casPaymentStatus) && n.EventType == casPaymentStatus));
        }

        public async Task HandleEventAsync(PaymentStatusChangedEvent eventData)
        {
            if (!await featureChecker.IsEnabledAsync("Unity.Notifications"))
            {
                return;
            }

            try
            {
                var application = await applicationRepository.GetAsync(eventData.ApplicationId, includeDetails: true);
                if (application == null)
                {
                    logger.LogWarning(
                        "EventNotificationHandler: Application {ApplicationId} not found for payment {PaymentRequestId}.",
                        eventData.ApplicationId,
                        eventData.PaymentRequestId);
                    return;
                }

                var notifications = (await scheduledNotificationRepository.GetListAsync(
                    PaymentEventNotificationFilter(
                        application.ApplicationFormId,
                        eventData.Status,
                        eventData.CasPaymentStatus)))
                    .ToList();

                if (notifications.Count == 0)
                {
                    return;
                }

                var emailFrom = await GetDefaultFromAddressAsync();
                var applicantAgent = await applicantAgentRepository.FirstOrDefaultAsync(a => a.ApplicationId == application.Id);

                foreach (var notification in notifications)
                {
                    await ProcessNotificationAsync(notification, application, applicantAgent, emailFrom);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "EventNotificationHandler: Error processing payment event for payment {PaymentRequestId}.",
                    eventData.PaymentRequestId);
            }
        }

        private async Task<string> GetDefaultFromAddressAsync()
        {
            return await settingProvider.GetOrNullAsync(NotificationsSettings.Mailing.DefaultFromAddress)
                ?? "NoReply@gov.bc.ca";
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
                    "EventNotificationHandler: Scheduled notification {NotificationId} has no recipient category or identifier, skipping.",
                    notification.Id);
                return;
            }

            var template = await templateService.GetTemplateById(notification.EmailTemplateId);
            if (template == null)
            {
                logger.LogWarning(
                    "EventNotificationHandler: Email template {TemplateId} not found for scheduled notification {NotificationId}, skipping.",
                    notification.EmailTemplateId,
                    notification.Id);
                return;
            }

            var tokenValues = ScheduledNotificationHelper.BuildTokenValues(application, applicantAgent);
            var subject = ScheduledNotificationHelper.RenderTemplate(template.Subject, tokenValues);
            var body = ScheduledNotificationHelper.RenderTemplate(
                string.IsNullOrWhiteSpace(template.BodyHTML) ? template.BodyText : template.BodyHTML,
                tokenValues);

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

            if (string.Equals(notification.RecipientCategory, "Internal", StringComparison.OrdinalIgnoreCase))
            {
                await scheduledNotificationHelper.PublishToEmailGroupAsync(
                    emailGroupsAppService,
                    emailGroupUsersAppService,
                    identityUserIntegrationService,
                    localEventBus,
                    notification,
                    emailEvent);
            }
            else if (string.Equals(notification.RecipientCategory, "External", StringComparison.OrdinalIgnoreCase))
            {
                await scheduledNotificationHelper.PublishToExternalRecipientAsync(
                    localEventBus,
                    notification,
                    application,
                    applicantAgent,
                    emailEvent);
            }
            else
            {
                logger.LogWarning(
                    "EventNotificationHandler: Unknown RecipientCategory '{Category}' on notification {NotificationId}, skipping.",
                    notification.RecipientCategory,
                    notification.Id);
            }
        }
    }
}