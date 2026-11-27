using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.GrantManager.Web.Identity;

internal sealed class StatusWorkflowPermissionCacheCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StatusWorkflowPermissionCacheCleanupService> _logger;
    private readonly TimeSpan _retryDelay;

    public StatusWorkflowPermissionCacheCleanupService(IServiceScopeFactory scopeFactory, IConfiguration configuration,
        ILogger<StatusWorkflowPermissionCacheCleanupService> logger)
        : this(scopeFactory, configuration, logger, TimeSpan.FromSeconds(30))
    {
    }

    internal StatusWorkflowPermissionCacheCleanupService(IServiceScopeFactory scopeFactory, IConfiguration configuration,
        ILogger<StatusWorkflowPermissionCacheCleanupService> logger, TimeSpan retryDelay)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
        _retryDelay = retryDelay;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Cache connections and timeouts must not delay the host's startup path.
        await Task.Yield();
        if (!_configuration.GetValue<bool>("Redis:IsEnabled"))
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<StatusWorkflowPermissionCacheInvalidator>()
                    .InvalidateAsync(stoppingToken);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                _logger.LogWarning(exception,
                    "Status Workflow permission-cache cleanup failed. Cached permissions may be stale; retrying in {RetryDelaySeconds} seconds.",
                    _retryDelay.TotalSeconds);
            }

            try
            {
                await Task.Delay(_retryDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }
}
