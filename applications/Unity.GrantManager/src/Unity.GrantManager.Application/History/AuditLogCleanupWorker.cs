using System;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;
using System.Threading;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Logs;
using Unity.GrantManager.Settings;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Settings;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.History;

[DisallowConcurrentExecution]
public class AuditLogCleanupWorker : QuartzBackgroundWorkerBase, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITenantRepository _tenantRepository;
    private readonly ISettingProvider _settingProvider;

    public AuditLogCleanupWorker(
        IServiceProvider serviceProvider,
        ITenantRepository tenantRepository,
        ISettingProvider settingProvider)
    {
        _serviceProvider = serviceProvider;
        _tenantRepository = tenantRepository;
        _settingProvider = settingProvider;

        JobDetail = JobBuilder
            .Create<AuditLogCleanupWorker>()
            .WithIdentity(nameof(AuditLogCleanupWorker))
            .Build();

        Trigger = TriggerBuilder
            .Create()
            .WithIdentity(nameof(AuditLogCleanupWorker))
            .WithSchedule(CronScheduleBuilder.CronSchedule("0 0 2 1/1 * ? *")
                .WithMisfireHandlingInstructionIgnoreMisfires())
            .Build();
    }

    public override async Task Execute(IJobExecutionContext context)
    {
        var enabled = await _settingProvider.GetAsync(
            SettingsConstants.Retention.CleanupEnabled,
            defaultValue: true);
        if (!enabled)
        {
            return;
        }

        var retentionDays = await _settingProvider.GetAsync(
            SettingsConstants.Retention.AuditLogRetentionDays,
            defaultValue: SettingsConstants.DefaultAuditLogRetentionDays);
        var exceptionRetentionDays = await _settingProvider.GetAsync(
            SettingsConstants.Retention.ExceptionLogRetentionDays,
            defaultValue: SettingsConstants.DefaultExceptionLogRetentionDays);
        var cancellationToken = context.CancellationToken;

        if (retentionDays > SettingsConstants.IndefiniteRetentionDays)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            await CleanupAuditLogsAsync(cutoffDate, cancellationToken);
        }

        if (exceptionRetentionDays > SettingsConstants.IndefiniteRetentionDays)
        {
            var exceptionCutoffDate = DateTime.UtcNow.AddDays(-exceptionRetentionDays);
            await CleanupExceptionLogsAsync(exceptionCutoffDate, cancellationToken);
        }
    }

    private async Task CleanupAuditLogsAsync(DateTime cutoffDate, CancellationToken cancellationToken)
    {
        await ForEachTenantAsync(async serviceProvider =>
        {
            var repository = serviceProvider.GetRequiredService<IExtendedAuditLogRepository>();
            var deleted = await repository.DeleteOlderThanAsync(cutoffDate, cancellationToken);
            if (deleted > 0)
            {
                Logger.LogInformation("Deleted {DeletedAuditLogCount} audit logs older than {CutoffDate:yyyy-MM-dd}", deleted, cutoffDate);
            }
        }, cancellationToken);
    }

    private async Task CleanupExceptionLogsAsync(DateTime cutoffDate, CancellationToken cancellationToken)
    {
        await ForEachTenantAsync(async serviceProvider =>
        {
            var repository = serviceProvider.GetRequiredService<IRepository<ExceptionLog, Guid>>();
            await repository.DeleteAsync(x => x.CreationTime < cutoffDate, autoSave: true, cancellationToken);
        }, cancellationToken);
    }

    private async Task ForEachTenantAsync(Func<IServiceProvider, Task> cleanup, CancellationToken cancellationToken)
    {
        await CleanupInScopeAsync(null, null, cleanup, cancellationToken);

        var tenants = await _tenantRepository.GetListAsync(cancellationToken: cancellationToken);
        foreach (var tenant in tenants)
        {
            await CleanupInScopeAsync(tenant.Id, tenant.Name, cleanup, cancellationToken);
        }
    }

    private async Task CleanupInScopeAsync(
        Guid? tenantId,
        string? tenantName,
        Func<IServiceProvider, Task> cleanup,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var scope = _serviceProvider.CreateScope();
        var currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
        using (currentTenant.Change(tenantId, tenantName))
        {
            try
            {
                await cleanup(scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while cleaning up audit data for tenant {TenantId}", tenantId);
            }
        }
    }
}
