using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.Auditing;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Claims;

namespace Unity.Modules.Shared.Utils;

/// <summary>
/// Establishes the trusted execution context for every Quartz background worker.
/// </summary>
public abstract class UnityQuartzBackgroundWorkerBase : QuartzBackgroundWorkerBase
{
    protected virtual bool UsesTenantAuditScopes => false;

    public sealed override async Task Execute(IJobExecutionContext context)
    {
        var auditingManager = ServiceProvider.GetRequiredService<IAuditingManager>();
        var auditingStore = ServiceProvider.GetRequiredService<IAuditingStore>();
        var principalAccessor = ServiceProvider.GetRequiredService<ICurrentPrincipalAccessor>();
        var currentTenant = ServiceProvider.GetRequiredService<ICurrentTenant>();

        using (BackgroundJobExecutionContext.Use())
        {
            if (UsesTenantAuditScopes)
            {
                using (BackgroundJobContext.SetExecutionContext(principalAccessor, currentTenant, tenantId: null))
                {
                    await ExecuteInBackgroundAsync(context);
                }
                return;
            }

            using (BackgroundJobContext.Set(auditingManager, principalAccessor, currentTenant, tenantId: null))
            {
                AddWorkerAuditAction(auditingManager, context);
                await ExecuteInBackgroundAsync(context);

                if (auditingManager.Current?.Log is { EntityChanges.Count: > 0 } log)
                {
                    try
                    {
                        using (currentTenant.Change(null))
                        {
                            await auditingStore.SaveAsync(log);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Failed to persist audit log for background worker {WorkerName}; worker work was already committed.", GetType().Name);
                    }
                }
            }
        }
    }

    protected async Task ExecuteInTenantAuditScopeAsync(Guid? tenantId, Func<Task> action)
    {
        var auditingManager = ServiceProvider.GetRequiredService<IAuditingManager>();
        var auditingStore = ServiceProvider.GetRequiredService<IAuditingStore>();
        var currentTenant = ServiceProvider.GetRequiredService<ICurrentTenant>();
        var principalAccessor = ServiceProvider.GetRequiredService<ICurrentPrincipalAccessor>();

        using (BackgroundJobContext.Set(auditingManager, principalAccessor, currentTenant, tenantId))
        {
            AddWorkerAuditAction(auditingManager, null);
            await action();

            if (auditingManager.Current?.Log is { EntityChanges.Count: > 0 } log)
            {
                try
                {
                    await auditingStore.SaveAsync(log);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to persist audit log for tenant {TenantId}; worker work was already committed.", tenantId);
                }
            }
        }
    }

    private void AddWorkerAuditAction(IAuditingManager auditingManager, IJobExecutionContext? context)
    {
        if (auditingManager.Current?.Log == null)
        {
            return;
        }

        auditingManager.Current.Log.Actions.Add(new AuditLogActionInfo
        {
            ServiceName = GetType().FullName ?? GetType().Name,
            MethodName = nameof(ExecuteInBackgroundAsync),
            Parameters = context == null ? "{}" : System.Text.Json.JsonSerializer.Serialize(new { context.FireInstanceId }),
            ExecutionTime = DateTime.UtcNow
        });
    }

    protected abstract Task ExecuteInBackgroundAsync(IJobExecutionContext context);
}