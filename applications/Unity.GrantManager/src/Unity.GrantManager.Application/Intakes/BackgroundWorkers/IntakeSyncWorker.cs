using Microsoft.Extensions.Logging;
using Quartz;
using System.Net.Http;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Settings;
using Unity.Modules.Shared.Utils;
using Unity.Notifications.EmailNotifications;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.MultiTenancy;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.Intakes.BackgroundWorkers
{
    [DisallowConcurrentExecution]
    public class IntakeSyncWorker : QuartzBackgroundWorkerBase
    {
        private readonly ICurrentTenant _currentTenant;
        private readonly ITenantRepository _tenantRepository;
        private readonly IApplicationFormSycnronizationService _applicationFormSynchronizationService;
        private readonly string _numberOfDaysToCheck;
        private readonly IEmailNotificationService _emailNotificationService;

        public IntakeSyncWorker(ICurrentTenant currentTenant,
            ITenantRepository tenantRepository,
            ISettingManager settingManager,
            IApplicationFormSycnronizationService applicationFormSynchronizationService,
            IEmailNotificationService emailNotificationService)
        {
            _currentTenant = currentTenant;
            _tenantRepository = tenantRepository;
            _applicationFormSynchronizationService = applicationFormSynchronizationService;
            _emailNotificationService = emailNotificationService;

            string intakeResyncExpression = SettingDefinitions.GetSettingsValue(settingManager, SettingsConstants.BackgroundJobs.IntakeResync_Expression);
            _numberOfDaysToCheck = SettingDefinitions.GetSettingsValue(settingManager, SettingsConstants.BackgroundJobs.IntakeResync_NumDaysToCheck);

            JobDetail = JobBuilder
                .Create<IntakeSyncWorker>()
                .WithIdentity(nameof(IntakeSyncWorker))
                .Build();

            Trigger = TriggerBuilder
                .Create()
                .WithIdentity(nameof(IntakeSyncWorker))
                .WithSchedule(CronScheduleBuilder.CronSchedule(intakeResyncExpression)
                .WithMisfireHandlingInstructionIgnoreMisfires())
                .Build();
        }

        public override async Task Execute(IJobExecutionContext context)
        {
            Logger.LogInformation("Executing IntakeSyncWorker...");
            var tenants = await _tenantRepository.GetListAsync();

            if (!int.TryParse(_numberOfDaysToCheck, out int numberDaysBack))
            {
                Logger.LogInformation("IntakeSyncWorker - Could not parse number of Days...");
                return;
            }

            bool sendEmail = false;
            var emailBodyBuilder = new System.Text.StringBuilder();
            emailBodyBuilder.AppendLine("Hello,<br><br>This email is to inform you about recent CHEFS submissions that failed to be imported into Unity Production.");

            foreach (var tenant in tenants)
            {
                using (_currentTenant.Change(tenant.Id, tenant.Name))
                {
                    var (missingSubmissions, submissionReport) = await _applicationFormSynchronizationService.GetMissingSubmissions(numberDaysBack);
                    if (missingSubmissions?.Count > 0) 
                    {
                        emailBodyBuilder.AppendLine($"<br><br>Tenant: {tenant.Name}<br>{submissionReport}");
                        sendEmail = true;
                    }
                }
            }

            emailBodyBuilder.AppendLine("<br> Bests");
            string emailBody = emailBodyBuilder.ToString();

            if (sendEmail) {
                string htmlBody = $@"
                <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <p>{emailBody}</p>
                        <br />
                        <p style='font-size: 12px; color: #999;'>*Note - Please do not reply to this email as it is an automated notification.</p>
                    </body>
                </html>";

                await _emailNotificationService.SendEmailNotification(
                    "grantmanagementsupport@gov.bc.ca",
                    htmlBody,
                    "Unity Failed Submissions Notification",
                    "NoReply@gov.bc.ca", "html",
                    ""); 

                Logger.LogInformation("Missing Submissions Email Sent...");
            }

            Logger.LogInformation("IntakeSyncWorker Executed...");
            await Task.CompletedTask;
        }
    }
}
