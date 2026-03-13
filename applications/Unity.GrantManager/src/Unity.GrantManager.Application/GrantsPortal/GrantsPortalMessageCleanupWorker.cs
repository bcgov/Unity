using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using Unity.GrantManager.GrantsPortal.Configuration;
using Unity.GrantManager.Messaging;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantsPortal;

/// <summary>
/// Periodically deletes processed/failed messages older than the configured retention period.
/// Runs against the central host database.
/// </summary>
[DisallowConcurrentExecution]
public class GrantsPortalMessageCleanupWorker : QuartzBackgroundWorkerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly int _retentionDays;

    public GrantsPortalMessageCleanupWorker(
        IServiceProvider serviceProvider,
        IOptions<GrantsPortalRabbitMqOptions> options)
    {
        _serviceProvider = serviceProvider;
        _retentionDays = options.Value.MessageRetentionDays;

        var cronExpression = options.Value.MessageCleanupCron;

        JobDetail = JobBuilder
            .Create<GrantsPortalMessageCleanupWorker>()
            .WithIdentity(nameof(GrantsPortalMessageCleanupWorker))
            .Build();

        Trigger = TriggerBuilder
            .Create()
            .WithIdentity(nameof(GrantsPortalMessageCleanupWorker))
            .WithSchedule(CronScheduleBuilder.CronSchedule(cronExpression)
            .WithMisfireHandlingInstructionIgnoreMisfires())
            .Build();
    }

    public override async Task Execute(IJobExecutionContext context)
    {
        Logger.LogDebug("GrantsPortalMessageCleanupWorker executing (retention={RetentionDays} days)...", _retentionDays);

        try
        {
            await CleanupOldMessagesAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during integration message cleanup");
        }
    }

    private async Task CleanupOldMessagesAsync()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-_retentionDays);

        using var scope = _serviceProvider.CreateScope();
        var inboxRepo = scope.ServiceProvider.GetRequiredService<IInboxMessageRepository>();
        var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();
        var unitOfWorkManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

        using var uow = unitOfWorkManager.Begin(requiresNew: true);
        var inboxDeleted = await inboxRepo.DeleteProcessedOlderThanAsync(cutoffDate);
        var outboxDeleted = await outboxRepo.DeleteProcessedOlderThanAsync(cutoffDate);
        await uow.CompleteAsync();

        var total = inboxDeleted + outboxDeleted;
        if (total > 0)
        {
            Logger.LogInformation(
                "Cleaned up {Total} messages older than {CutoffDate:yyyy-MM-dd} (inbox={InboxCount}, outbox={OutboxCount})",
                total, cutoffDate, inboxDeleted, outboxDeleted);
        }
    }
}
