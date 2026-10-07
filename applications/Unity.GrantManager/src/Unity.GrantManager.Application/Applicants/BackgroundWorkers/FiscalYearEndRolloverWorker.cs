using Microsoft.Extensions.DependencyInjection;
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
using Volo.Abp.Uow;

namespace Unity.GrantManager.Applicants.BackgroundWorkers;

[DisallowConcurrentExecution]
public class FiscalYearEndRolloverWorker : QuartzBackgroundWorkerBase
{
    private readonly ILogger<FiscalYearEndRolloverWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITenantRepository _tenantRepository;

    public FiscalYearEndRolloverWorker(
        ILogger<FiscalYearEndRolloverWorker> logger,
        IServiceProvider serviceProvider,
        ITenantRepository tenantRepository,
        ISettingManager settingManager)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _tenantRepository = tenantRepository;

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
                // Each tenant gets its own DI scope, UoW, and repository so the cached tenant
                // DbContext from a prior iteration cannot leak into the next tenant's connection.
                using var tenantScope = _serviceProvider.CreateScope();
                var currentTenant = tenantScope.ServiceProvider.GetRequiredService<ICurrentTenant>();
                var uowManager = tenantScope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

                // The rollover runs raw SQL directly against the tenant connection, which bypasses
                // ABP's EF Core query filters (soft-delete, multi-tenancy) entirely, so no explicit
                // IDataFilter bypass is required here - CurrentTenant.Change only selects the
                // correct tenant connection string.
                using (currentTenant.Change(tenant.Id, tenant.Name))
                {
                    using var uow = uowManager.Begin(requiresNew: true, isTransactional: false);
                    var applicantRepository = tenantScope.ServiceProvider.GetRequiredService<IApplicantRepository>();

                    var result = await applicantRepository.RollOverFiscalYearEndAsync();

                    if (!result.ColumnsAvailable)
                    {
                        _logger.LogWarning(
                            "Skipped fiscal year end rollover for tenant {TenantName} ({TenantId}) - missing column(s): {MissingColumns}",
                            tenant.Name, tenant.Id, string.Join(", ", result.MissingColumns));
                        continue;
                    }

                    await uow.CompleteAsync(context.CancellationToken);

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

