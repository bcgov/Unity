using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Unity.GrantManager.GrantsPortal.Configuration;
using Unity.GrantManager.Messaging;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantsPortal;

/// <summary>
/// Periodically deletes processed/failed messages older than the configured retention period.
/// Runs once per hour against the central host database.
/// </summary>
public class GrantsPortalMessageCleanupService(
    IServiceProvider serviceProvider,
    IOptions<GrantsPortalRabbitMqOptions> options,
    ILogger<GrantsPortalMessageCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);
    private readonly int _retentionDays = options.Value.MessageRetentionDays;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Integration message cleanup service starting (retention={RetentionDays} days)", _retentionDays);

        // Wait for the application to fully start
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOldMessagesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during integration message cleanup");
            }

            await Task.Delay(CleanupInterval, stoppingToken);
        }
    }

    private async Task CleanupOldMessagesAsync()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-_retentionDays);

        using var scope = serviceProvider.CreateScope();
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
            logger.LogInformation(
                "Cleaned up {Total} messages older than {CutoffDate:yyyy-MM-dd} (inbox={InboxCount}, outbox={OutboxCount})",
                total, cutoffDate, inboxDeleted, outboxDeleted);
        }
    }
}
