using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Auditing;
using Volo.Abp.Modularity;

namespace Unity.Modules.Shared.Auditing;

/// <summary>
/// ABP module that overrides default auditing behavior to support background job entity change tracking.
/// Registers custom implementations that force auditing when BackgroundJobExecutionContext is active.
/// </summary>
[DependsOn(
    typeof(AbpAuditingModule)
)]
public class UnityAuditingOverideModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Override audit property setter to handle readonly audit properties in background jobs
        context.Services.Replace(
            ServiceDescriptor.Transient<IAuditPropertySetter, BackgroundJobAuditPropertySetter>()
        );

        // Override auditing helper to force entity change tracking for background jobs
        context.Services.Replace(
            ServiceDescriptor.Transient<IAuditingHelper, UnityAuditingHelper>()
        );
    }
}
