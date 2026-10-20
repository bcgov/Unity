using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;
using Unity.GrantManager.Settings;
using Unity.Modules.Shared.Utils;
using Volo.Abp.SettingManagement;

namespace Unity.GrantManager.Integrations.Metabase;

/// <summary>
/// Keeps every set-up tenant's Metabase group in line with Unity on a schedule (hourly by
/// default), through <see cref="MetabaseMembershipSyncService"/>. Off-boarding doesn't wait for
/// it - <see cref="MetabaseMemberOffboardingHandler"/> removes access straight away - but it is
/// what picks up new permission holders, people who have since signed in to Metabase, and
/// anything a removal job missed.
/// </summary>
[DisallowConcurrentExecution]
public class MetabaseMembershipSyncWorker : UnityQuartzBackgroundWorkerBase
{
    private readonly MetabaseMembershipSyncService _syncService;
    private readonly ILogger<MetabaseMembershipSyncWorker> _logger;

    public MetabaseMembershipSyncWorker(
        MetabaseMembershipSyncService syncService,
        ISettingManager settingManager,
        ILogger<MetabaseMembershipSyncWorker> logger)
    {
        _syncService = syncService;
        _logger = logger;

        // Hourly at 2 minutes past: every other scheduled job starts at minute 0.
        const string defaultCronExpression = "0 2 * 1/1 * ? *";
        string cronExpression = defaultCronExpression;

        try
        {
            var settingsValue = SettingDefinitions
                .GetSettingsValue(settingManager,
                    SettingsConstants.BackgroundJobs.MetabaseMembershipSync_Expression);

            if (!settingsValue.IsNullOrEmpty())
            {
                if (CronExpression.IsValidExpression(settingsValue))
                {
                    cronExpression = settingsValue;
                }
                else
                {
                    _logger.LogWarning("Invalid cron expression '{CronExpression}' for the Metabase members sync, reverting to default '{DefaultCronExpression}'",
                        settingsValue, defaultCronExpression);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading cron setting for the Metabase members sync, reverting to default '{CronExpression}'", defaultCronExpression);
        }

        JobDetail = JobBuilder
            .Create<MetabaseMembershipSyncWorker>()
            .WithIdentity(nameof(MetabaseMembershipSyncWorker))
            .Build();

        Trigger = TriggerBuilder
            .Create()
            .WithIdentity(nameof(MetabaseMembershipSyncWorker))
            .WithSchedule(CronScheduleBuilder.CronSchedule(cronExpression)
            .WithMisfireHandlingInstructionIgnoreMisfires())
            .Build();
    }

    protected override async Task ExecuteInBackgroundAsync(IJobExecutionContext context)
    {
        try
        {
            var (synced, skipped, failed) = await _syncService.SyncAllTenantsAsync();
            _logger.LogInformation("MetabaseMembershipSyncWorker completed. Synced: {Synced}, Skipped: {Skipped}, Failed: {Failed}",
                synced, skipped, failed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in MetabaseMembershipSyncWorker");
        }
    }
}
