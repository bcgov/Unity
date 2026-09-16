using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Settings;
using Unity.Modules.Shared.Utils;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.MultiTenancy;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.Applicants.BackgroundWorkers;

[DisallowConcurrentExecution]
public class FiscalYearEndRolloverWorker : QuartzBackgroundWorkerBase
{
    private readonly ILogger<FiscalYearEndRolloverWorker> _logger;
    private readonly ICurrentTenant _currentTenant;
    private readonly ITenantRepository _tenantRepository;
    private readonly IApplicantRepository _applicantRepository;

    public FiscalYearEndRolloverWorker(
        ILogger<FiscalYearEndRolloverWorker> logger,
        ICurrentTenant currentTenant,
        ITenantRepository tenantRepository,
        IApplicantRepository applicantRepository,
        ISettingManager settingManager)
    {
        _logger = logger;
        _currentTenant = currentTenant;
        _tenantRepository = tenantRepository;
        _applicantRepository = applicantRepository;

        // Midnight January 1st every year (server local time)
        const string defaultCronExpression = "0 0 0 1 1 ? *";
        var cronExpression = defaultCronExpression;

        try
        {
            var settingsValue = SettingDefinitions.GetSettingsValue(
                settingManager, SettingsConstants.BackgroundJobs.FiscalYearEndRollover_Expression);

            if (!settingsValue.IsNullOrEmpty())
            {
                if (CronExpression.IsValidExpression(settingsValue))
                {
                    cronExpression = settingsValue;
                }
                else
                {
                    _logger.LogWarning("Invalid cron expression '{CronExpression}' for fiscal year end rollover, reverting to default '{DefaultCronExpression}'",
                        settingsValue, defaultCronExpression);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading cron setting for fiscal year end rollover, reverting to default '{CronExpression}'", defaultCronExpression);
        }

        JobDetail = JobBuilder
            .Create<FiscalYearEndRolloverWorker>()
            .WithIdentity(nameof(FiscalYearEndRolloverWorker))
            .Build();

        Trigger = TriggerBuilder
            .Create()
            .WithIdentity(nameof(FiscalYearEndRolloverWorker))
            .WithSchedule(CronScheduleBuilder.CronSchedule(cronExpression)
            .WithMisfireHandlingInstructionIgnoreMisfires())
            .Build();
    }

    public override async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Executing FiscalYearEndRolloverWorker...");

        var tenants = await _tenantRepository.GetListAsync(cancellationToken: context.CancellationToken);

        foreach (var tenant in tenants)
        {
            try
            {
                // The rollover runs raw SQL directly against the tenant connection, which bypasses
                // ABP's EF Core query filters (soft-delete, multi-tenancy) entirely, so no explicit
                // IDataFilter bypass is required here - CurrentTenant.Change only selects the
                // correct tenant connection string.
                using (_currentTenant.Change(tenant.Id, tenant.Name))
                {
                    var result = await _applicantRepository.RollOverFiscalYearEndAsync();

                    if (!result.ColumnsAvailable)
                    {
                        _logger.LogWarning(
                            "Skipped fiscal year end rollover for tenant {TenantName} ({TenantId}) - missing column(s): {MissingColumns}",
                            tenant.Name, tenant.Id, string.Join(", ", result.MissingColumns));
                        continue;
                    }

                    _logger.LogInformation(
                        "Fiscal year end rollover completed for tenant {TenantName} ({TenantId}). Rows affected: {RowsAffected}",
                        tenant.Name, tenant.Id, result.RowsAffected);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running fiscal year end rollover for tenant {TenantName} ({TenantId})", tenant.Name, tenant.Id);
            }
        }

        _logger.LogInformation("FiscalYearEndRolloverWorker Executed...");
    }
}

