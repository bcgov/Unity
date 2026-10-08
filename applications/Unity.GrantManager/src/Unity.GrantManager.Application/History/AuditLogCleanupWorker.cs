using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Logs;
using Unity.GrantManager.Settings;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Settings;

namespace Unity.GrantManager.History;

[DisallowConcurrentExecution]
public class AuditLogCleanupWorker : QuartzBackgroundWorkerBase, ITransientDependency
{
    private readonly IExtendedAuditLogRepository _auditLogRepository;
    private readonly IRepository<ExceptionLog, Guid> _exceptionLogRepository;
    private readonly ISettingProvider _settingProvider;

    public AuditLogCleanupWorker(
        IExtendedAuditLogRepository auditLogRepository,
        IRepository<ExceptionLog, Guid> exceptionLogRepository,
        ISettingProvider settingProvider)
    {
        _auditLogRepository = auditLogRepository;
        _exceptionLogRepository = exceptionLogRepository;
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
            try
            {
                var deleted = await _auditLogRepository.DeleteOlderThanAsync(cutoffDate, cancellationToken);
                if (deleted > 0)
                {
                    Logger.LogInformation(
                        "Deleted {DeletedAuditLogCount} audit logs older than {CutoffDate:yyyy-MM-dd}",
                        deleted,
                        cutoffDate);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while cleaning up expired audit logs");
            }
        }

        if (exceptionRetentionDays > SettingsConstants.IndefiniteRetentionDays)
        {
            var exceptionCutoffDate = DateTime.UtcNow.AddDays(-exceptionRetentionDays);
            try
            {
                await _exceptionLogRepository.DeleteAsync(
                    exceptionLog => exceptionLog.CreationTime < exceptionCutoffDate,
                    autoSave: true,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while cleaning up expired exception logs");
            }
        }
    }
}
