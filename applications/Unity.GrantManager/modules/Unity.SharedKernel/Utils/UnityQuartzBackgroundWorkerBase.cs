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
        var principalAccessor = ServiceProvider.GetRequiredService<ICurrentPrincipalAccessor>();
        var currentTenant = ServiceProvider.GetRequiredService<ICurrentTenant>();

        using (BackgroundJobExecutionContext.Use())
        using (BackgroundJobContext.Set(auditingManager, principalAccessor, currentTenant, tenantId: null))
        {
            await ExecuteInBackgroundAsync(context);
        }
    }

    protected abstract Task ExecuteInBackgroundAsync(IJobExecutionContext context);
}