using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile;
using Unity.GrantManager.Settings;
using Unity.Modules.Shared.Utils;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.SettingManagement;

namespace Unity.GrantManager.Applicants.BackgroundWorkers
{
    [DisallowConcurrentExecution]
    public class ApplicantTenantMapReconciliationWorker : QuartzBackgroundWorkerBase
    {
        private readonly IApplicantProfileAppService _applicantProfileAppService;
        private readonly ILogger<ApplicantTenantMapReconciliationWorker> _logger;             

        /// <summary>
        /// Initializes a new instance of the ApplicantTenantMapReconciliationWorker class with the specified services
        /// and logger.
        /// </summary>
        /// <remarks>The scheduling behavior of the worker is determined by a cron expression retrieved
        /// from application settings. If the setting is unavailable or cannot be read, a default schedule is used.
        /// Logging is performed for any issues encountered during initialization.</remarks>
        /// <param name="applicantProfileAppService">The service used to access and manage applicant profile data.</param>
        /// <param name="settingManager">The setting manager used to retrieve configuration values, including the cron expression for scheduling.</param>
        /// <param name="logger">The logger used to record diagnostic and operational information for this worker.</param>
        public ApplicantTenantMapReconciliationWorker(
            IApplicantProfileAppService applicantProfileAppService,
            ISettingManager settingManager,
            ILogger<ApplicantTenantMapReconciliationWorker> logger)
        {
            _applicantProfileAppService = applicantProfileAppService;
            _logger = logger;

            // 2 AM PST = 10 AM UTC
            const string defaultCronExpression = "0 0 10 1/1 * ? *";
            string cronExpression = defaultCronExpression;

            try
            {
                var settingsValue = SettingDefinitions
                    .GetSettingsValue(settingManager,
                        SettingsConstants.BackgroundJobs.ApplicantTenantMapReconciliation_Expression);

                if (!settingsValue.IsNullOrEmpty())
                {
                    if (CronExpression.IsValidExpression(settingsValue))
                    {
                        cronExpression = settingsValue;
                    }
                    else
                    {
                        _logger.LogWarning("Invalid cron expression '{CronExpression}' for tenant map reconciliation, reverting to default '{DefaultCronExpression}'",
                            settingsValue, defaultCronExpression);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error reading cron setting for tenant maps, reverting to default '{CronExpression}'", defaultCronExpression);
            }

            if (!cronExpression.IsNullOrEmpty())
            {

                JobDetail = JobBuilder
                    .Create<ApplicantTenantMapReconciliationWorker>()
                    .WithIdentity(nameof(ApplicantTenantMapReconciliationWorker))
                    .Build();

                Trigger = TriggerBuilder
                    .Create()
                    .WithIdentity(nameof(ApplicantTenantMapReconciliationWorker))
                    .WithSchedule(CronScheduleBuilder.CronSchedule(cronExpression)
                    .WithMisfireHandlingInstructionIgnoreMisfires())
                    .Build();
            }
        }

        /// <summary>
        /// Executes the reconciliation process for applicant-tenant mappings as part of a scheduled job.
        /// </summary>
        /// <remarks>This method is typically invoked by a job scheduler and should not be called directly
        /// in application code. Logging is performed to record the outcome of the reconciliation process and any errors
        /// encountered.</remarks>
        /// <param name="context">The execution context for the job, providing runtime information and job-specific data.</param>
        /// <returns>A task that represents the asynchronous execution of the job.</returns>
        public override async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Executing ApplicantTenantMapReconciliationWorker...");

            try
            {
                var (created, updated) = await _applicantProfileAppService.ReconcileApplicantTenantMapsAsync();
                _logger.LogInformation("ApplicantTenantMapReconciliationWorker completed. Created: {Created}, Updated: {Updated}",
                    created, updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ApplicantTenantMapReconciliationWorker");
            }
        }
    }
}
