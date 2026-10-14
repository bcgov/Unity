using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
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
    public sealed override async Task Execute(IJobExecutionContext context)
    {
        var auditingManager = ServiceProvider.GetRequiredService<IAuditingManager>();
        var auditingStore = ServiceProvider.GetRequiredService<IAuditingStore>();
        var principalAccessor = ServiceProvider.GetRequiredService<ICurrentPrincipalAccessor>();
        var currentTenant = ServiceProvider.GetRequiredService<ICurrentTenant>();

        using (BackgroundJobExecutionContext.Use())
        using (BackgroundJobContext.Set(auditingManager, principalAccessor, currentTenant, tenantId: null))
        {
            AddWorkerAuditAction(auditingManager, context);
            await ExecuteInBackgroundAsync(context);

            if (auditingManager.Current?.Log is { EntityChanges.Count: > 0 } log)
            {
                using (currentTenant.Change(null))
                {
                    await auditingStore.SaveAsync(log);
                }
            }
        }
    }

    private void AddWorkerAuditAction(IAuditingManager auditingManager, IJobExecutionContext context)
    {
        if (auditingManager.Current?.Log == null)
        {
            return;
        }

        auditingManager.Current.Log.Actions.Add(new AuditLogActionInfo
        {
            ServiceName = GetType().FullName ?? GetType().Name,
            MethodName = nameof(ExecuteInBackgroundAsync),
            Parameters = System.Text.Json.JsonSerializer.Serialize(new { context.FireInstanceId }),
            ExecutionTime = DateTime.UtcNow
        });
    }

    protected abstract Task ExecuteInBackgroundAsync(IJobExecutionContext context);
}