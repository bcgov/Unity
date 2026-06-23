using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Notifications;
using Unity.Notifications.Emails;
using Unity.Notifications.EmailGroups;
using Unity.Notifications.Settings;
using Unity.Notifications.Templates;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Features;
using Volo.Abp.Identity.Integration;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Settings;
using Volo.Abp.TenantManagement;
using Unity.Notifications.Events;
using Unity.Modules.Shared.Utils;
using Unity.GrantManager.Settings;
using Volo.Abp.SettingManagement;

namespace Unity.GrantManager.Events
{
    /// <summary>
    /// Quartz background job that runs nightly to process date-based scheduled notifications.
    /// Checks if application trigger dates (DueDate, NotificationDate, ContractNotificationDate) have passed
    /// and sends emails accordingly.
    /// </summary>
    [DisallowConcurrentExecution]
    public class DateBasedScheduledNotificationJob : QuartzBackgroundWorkerBase, ITransientDependency
    {
        private readonly IRepository<ScheduledNotification, Guid> _scheduledNotificationRepository;
        private readonly IApplicationRepository _applicationRepository;
        private readonly IRepository<ScheduledNotificationTracking, Guid> _trackingRepository;
        private readonly IRepository<EmailLog, Guid> _emailLogRepository;
        private readonly IApplicantAgentRepository _applicantAgentRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly ILocalEventBus _localEventBus;
        private readonly ITemplateService _templateService;
        private readonly IEmailGroupsAppService _emailGroupsAppService;
        private readonly IEmailGroupUsersAppService _emailGroupUsersAppService;
        private readonly IIdentityUserIntegrationService _identityUserIntegrationService;
        private readonly IFeatureChecker _featureChecker;
        private readonly ISettingProvider _settingProvider;
        private readonly ICurrentTenant _currentTenant;
        private readonly ScheduledNotificationHelper _scheduledNotificationHelper;
        private readonly ILogger<DateBasedScheduledNotificationJob> _logger;

        public DateBasedScheduledNotificationJob(
            IRepository<ScheduledNotification, Guid> scheduledNotificationRepository,
            IApplicationRepository applicationRepository,
            IRepository<ScheduledNotificationTracking, Guid> trackingRepository,
            IRepository<EmailLog, Guid> emailLogRepository,
            IApplicantAgentRepository applicantAgentRepository,
            ITenantRepository tenantRepository,
            ILocalEventBus localEventBus,
            ITemplateService templateService,
            IEmailGroupsAppService emailGroupsAppService,
            IEmailGroupUsersAppService emailGroupUsersAppService,
            IIdentityUserIntegrationService identityUserIntegrationService,
            IFeatureChecker featureChecker,
            ISettingProvider settingProvider,
            ICurrentTenant currentTenant,
            ScheduledNotificationHelper scheduledNotificationHelper,
            SettingManager settingManager,
            ILogger<DateBasedScheduledNotificationJob> logger)
        {
            _scheduledNotificationRepository = scheduledNotificationRepository;
            _applicationRepository = applicationRepository;
            _trackingRepository = trackingRepository;
            _emailLogRepository = emailLogRepository;
            _applicantAgentRepository = applicantAgentRepository;
            _tenantRepository = tenantRepository;
            _localEventBus = localEventBus;
            _templateService = templateService;
            _emailGroupsAppService = emailGroupsAppService;
            _emailGroupUsersAppService = emailGroupUsersAppService;
            _identityUserIntegrationService = identityUserIntegrationService;
            _featureChecker = featureChecker;
            _settingProvider = settingProvider;
            _currentTenant = currentTenant;
            _scheduledNotificationHelper = scheduledNotificationHelper;
            _logger = logger;

            // 2 AM PST = 10 AM UTC
            const string defaultCronExpression = "0 0 2 * * ?";
            string cronExpression = defaultCronExpression;

            try
            {
                var settingsValue = SettingDefinitions
                    .GetSettingsValue(settingManager,
                        SettingsConstants.BackgroundJobs.DateBasedNotificationSchedule_Expression);

                if (!settingsValue.IsNullOrEmpty())
                {
                    if (CronExpression.IsValidExpression(settingsValue))
                    {
                        cronExpression = settingsValue;
                    }
                    else
                    {
                        _logger.LogWarning("Invalid cron expression '{CronExpression}' for date-based notifications, reverting to default '{DefaultCronExpression}'",
                            settingsValue, defaultCronExpression);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error reading cron setting for date-based notifications, reverting to default '{CronExpression}'", defaultCronExpression);
            }

            if (!cronExpression.IsNullOrEmpty())
            {
                JobDetail = JobBuilder
                    .Create<DateBasedScheduledNotificationJob>()
                    .WithIdentity(nameof(DateBasedScheduledNotificationJob))
                    .Build();

                Trigger = TriggerBuilder
                    .Create()
                    .WithIdentity(nameof(DateBasedScheduledNotificationJob))
                    .WithSchedule(CronScheduleBuilder.CronSchedule(cronExpression)
                        .WithMisfireHandlingInstructionIgnoreMisfires())
                    .Build();
                }
        }

        public override async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("DateBasedScheduledNotificationJob: Starting execution.");

                // Get all active tenants
                var allTenants = await _tenantRepository.GetListAsync();

                if (allTenants.Count == 0)
                {
                    _logger.LogInformation("DateBasedScheduledNotificationJob: No tenants found.");
                    return;
                }

                _logger.LogInformation("DateBasedScheduledNotificationJob: Processing notifications for {TenantCount} tenant(s).", allTenants.Count);

                // Process each tenant separately
                foreach (var tenant in allTenants)
                {
                    using (_currentTenant.Change(tenant.Id))
                    {
                        _logger.LogDebug("DateBasedScheduledNotificationJob: Processing notifications for tenant '{TenantName}' ({TenantId}).", tenant.Name, tenant.Id);
                        await ProcessTenantNotificationsAsync();
                    }
                }

                _logger.LogInformation("DateBasedScheduledNotificationJob: Completed execution.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DateBasedScheduledNotificationJob: Error during execution.");
            }
        }

        private async Task ProcessTenantNotificationsAsync()
        {
            try
            {
                // Check if Notifications feature is enabled for this tenant
                if (!await _featureChecker.IsEnabledAsync("Unity.Notifications"))
                {
                    _logger.LogInformation("DateBasedScheduledNotificationJob: Notifications feature is not enabled for current tenant, skipping.");
                    return;
                }

                // Get all active date-based notifications for this tenant (now in correct tenant context)
                var notifications = (await _scheduledNotificationRepository.GetListAsync(
                    n => n.IsActive
                      && !string.IsNullOrEmpty(n.DateField)))
                    .ToList();

                if (notifications.Count == 0)
                {
                    _logger.LogDebug("DateBasedScheduledNotificationJob: No active date-based notifications found for current tenant.");
                    return;
                }

                _logger.LogInformation("DateBasedScheduledNotificationJob: Processing {Count} date-based notifications for current tenant.", notifications.Count);

                // Extract unique FormIds from date-based notifications only
                // (notifications are already filtered to include only those with DateField set, meaning TriggerType is "Date")
                var formIds = notifications
                    .Where(n => !string.IsNullOrEmpty(n.DateField)) // Explicit check for Date trigger type
                    .Select(n => n.FormId)
                    .Distinct()
                    .ToList();

                if (formIds.Count == 0)
                {
                    _logger.LogDebug("DateBasedScheduledNotificationJob: No forms with date-based notifications found for current tenant.");
                    return;
                }

                var today = DateTime.UtcNow.Date;

                // OPTIMIZATION: Single query to get all applications for all forms with past dates
                // Only queries applications where FormId exists in ScheduledNotifications with Date trigger type
                var allApplications = (await _applicationRepository.GetListAsync(
                    a => formIds.Contains(a.ApplicationFormId)
                      && ((a.DueDate != null && a.DueDate <= today) ||
                          (a.ProjectStartDate != null && a.ProjectStartDate <= today) ||
                          (a.ProjectEndDate != null && a.ProjectEndDate <= today) ||
                          (a.NotificationDate != null && a.NotificationDate <= today) ||
                          (a.ContractExecutionDate != null && a.ContractExecutionDate <= today))))
                    .ToList();

                if (allApplications.Count == 0)
                {
                    _logger.LogDebug("DateBasedScheduledNotificationJob: No applications with past dates found across all forms.");
                    return;
                }

                // OPTIMIZATION: Batch query all tracking records for all notifications
                var notificationIds = notifications.Select(n => n.Id).ToList();
                var allTracking = (await _trackingRepository.GetListAsync(
                    t => notificationIds.Contains(t.ScheduledNotificationId)))
                    .ToList();

                // OPTIMIZATION: Batch load all needed templates upfront instead of querying per notification (N+1 problem)
                var uniqueTemplateIds = notifications.Select(n => n.EmailTemplateId).Distinct().ToHashSet();
                var templatesDict = (await _templateService.GetTemplatesByTenent())
                    .Where(t => uniqueTemplateIds.Contains(t.Id))
                    .ToDictionary(t => t.Id);

                // OPTIMIZATION: Batch load all ApplicantAgents instead of querying per application (N+1 problem)
                // This reduces 4000+ queries to 1 single batch query
                var applicationIds = allApplications.Select(a => a.Id).ToList();
                var applicantAgentsDict = (await _applicantAgentRepository.GetListAsync(
                    a => a.ApplicationId != null && applicationIds.Contains(a.ApplicationId.Value)))
                    .ToDictionary(a => a.ApplicationId!.Value, a => a);

                var defaultFromAddress = await _settingProvider.GetOrNullAsync(NotificationsSettings.Mailing.DefaultFromAddress);
                string emailFrom = defaultFromAddress ?? "NoReply@gov.bc.ca";
                
                // OPTIMIZATION: Collect all tracking records to batch insert at end
                var trackingRecordsToInsert = new List<ScheduledNotificationTracking>();

                // Group by notification and process
                foreach (var notification in notifications)
                {
                    // Get template from pre-loaded dictionary instead of querying per notification
                    if (!templatesDict.TryGetValue(notification.EmailTemplateId, out var template))
                    {
                        _logger.LogWarning(
                            "DateBasedScheduledNotificationJob: Email template {TemplateId} not found for notification {NotificationId}, skipping.",
                            notification.EmailTemplateId, notification.Id);
                        continue;
                    }

                    // Get applications for this form
                    var applicationsForForm = allApplications
                        .Where(a => a.ApplicationFormId == notification.FormId)
                        .ToList();

                    if (applicationsForForm.Count == 0)
                    {
                        _logger.LogDebug(
                            "DateBasedScheduledNotificationJob: No applications with past dates found for form {FormId}, skipping notification {NotificationId}.",
                            notification.FormId, notification.Id);
                        continue;
                    }

                    // Get already-notified app IDs for this specific notification
                    var notifiedAppIds = allTracking
                        .Where(t => t.ScheduledNotificationId == notification.Id
                                 && t.DateField == notification.DateField)
                        .Select(t => t.ApplicationId)
                        .ToHashSet();

                    // Filter out already-notified applications
                    var applicationsToProcess = applicationsForForm
                        .Where(a => !notifiedAppIds.Contains(a.Id))
                        .ToList();

                    if (applicationsToProcess.Count == 0)
                    {
                        _logger.LogDebug(
                            "DateBasedScheduledNotificationJob: All applications have already been notified for notification {NotificationId}, skipping.",
                            notification.Id);
                        continue;
                    }

                    _logger.LogInformation(
                        "DateBasedScheduledNotificationJob: Processing {Count} applications with past dates for notification {NotificationId}.",
                        applicationsToProcess.Count, notification.Id);

                    // Process applications for this notification - pass cached agents and batch list
                    foreach (var application in applicationsToProcess)
                    {
                        var tracking = await ProcessApplicationForNotificationAsync(
                            notification, application, template, emailFrom, applicantAgentsDict);
                        if (tracking != null)
                        {
                            trackingRecordsToInsert.Add(tracking);
                        }
                    }
                }

                // OPTIMIZATION: Batch insert all tracking records at once instead of individual inserts
                if (trackingRecordsToInsert.Count > 0)
                {
                    _logger.LogInformation(
                        "DateBasedScheduledNotificationJob: Batch inserting {Count} tracking records for current tenant.",
                        trackingRecordsToInsert.Count);
                    await _trackingRepository.InsertManyAsync(trackingRecordsToInsert, autoSave: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DateBasedScheduledNotificationJob: Error processing notifications for current tenant.");
            }
        }

        private async Task<ScheduledNotificationTracking?> ProcessApplicationForNotificationAsync(
            ScheduledNotification notification,
            Application application,
            EmailTemplate template,
            string emailFrom,
            Dictionary<Guid, ApplicantAgent> applicantAgentsDict)
      {
            try
            {
                // Build token values and render template
                // OPTIMIZATION: Use pre-loaded applicant agent from cache instead of querying per application
                applicantAgentsDict.TryGetValue(application.Id, out var applicantAgent);
                var tokenValues = ScheduledNotificationHelper.BuildTokenValues(application, applicantAgent);
                string subject = ScheduledNotificationHelper.RenderTemplate(template.Subject, tokenValues);
                string body = ScheduledNotificationHelper.RenderTemplate(
                    string.IsNullOrWhiteSpace(template.BodyHTML) ? template.BodyText : template.BodyHTML,
                    tokenValues);


                // Build email event with common properties
                var emailEvent = new EmailNotificationEvent
                {
                    TenantId = _currentTenant.Id,
                    ApplicationId = application.Id,
                    ScheduledNotificationId = notification.Id,
                    TemplateId = template.Id,
                    EmailTemplateName = template.Name,
                    Subject = subject,
                    Body = body,
                    EmailFrom = emailFrom,
                    Action = EmailAction.SendDateDriven, // Default action, may be overridden in helper methods
                    RetryAttempts = 0
                };

                // Publish based on recipient category
                if (string.Equals(notification.RecipientCategory, "Internal", StringComparison.OrdinalIgnoreCase))
                {
                    await _scheduledNotificationHelper.PublishToEmailGroupAsync(
                        _emailGroupsAppService, _emailGroupUsersAppService, _identityUserIntegrationService,
                        _localEventBus, notification, emailEvent);
                }
                else if (string.Equals(notification.RecipientCategory, "External", StringComparison.OrdinalIgnoreCase))
                {
                    await _scheduledNotificationHelper.PublishToExternalRecipientAsync(
                        _localEventBus, notification, application, applicantAgent, emailEvent);
                }
                else
                {
                    _logger.LogWarning(
                        "DateBasedScheduledNotificationJob: Unknown recipient category '{Category}' for notification {NotificationId}, skipping.",
                        notification.RecipientCategory, notification.Id);
                    return null;
                }

                // Create tracking record to prevent duplicate notifications
                return CreateTrackingRecord(notification, application, isDraft: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "DateBasedScheduledNotificationJob: Error processing application {ApplicationId} for notification {NotificationId}.",
                    application.Id, notification.Id);
                return null;
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
                    TenantId = _currentTenant.Id,
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
                    Tag = "DateBasedScheduledNotificationJob", // Identifies source as background job
                    Status = EmailStatus.Draft,
                    EmailType = EmailType.DateBased, // Distinguish from event-based emails created by event handler
                    RetryAttempts = 0
                };

                await _emailLogRepository.InsertAsync(draftEmail);
                _logger.LogInformation(
                    "DateBasedScheduledNotificationJob: Draft email created for scheduled notification {NotificationId} with subject '{Subject}'.",
                    notification.Id, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "DateBasedScheduledNotificationJob: Error creating draft email for scheduled notification {NotificationId}.",
                    notification.Id);
            }
        }

        private ScheduledNotificationTracking? CreateTrackingRecord(
            ScheduledNotification notification,
            Application application,
            bool isDraft = false)
        {
            if (string.IsNullOrEmpty(notification.DateField))
            {
                if (!isDraft)
                {
                    _logger.LogWarning(
                        "DateBasedScheduledNotificationJob: DateField is null or empty for notification {NotificationId}, cannot create tracking record.",
                        notification.Id);
                }
                return null;
            }

            var tracking = new ScheduledNotificationTracking(
                Guid.NewGuid(),
                application.Id,
                notification.Id,
                notification.DateField,
                DateTime.UtcNow);

            if (!isDraft)
            {
                _logger.LogInformation(
                    "DateBasedScheduledNotificationJob: Notification {NotificationId} ready to be tracked for application {ApplicationId}.",
                    notification.Id, application.Id);
            }

            return tracking;
        }
    }
}
