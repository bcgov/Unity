using System.Threading.Tasks;
using Unity.GrantManager.Analytics;
using Unity.GrantManager.Integrations;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Features;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Web.Analytics;

/// <summary>
/// Supplies the Matomo base URL from the DynamicUrl store, but only when the
/// <c>Unity.Analytics</c> feature flag is enabled for the current tenant.
/// In host context (no tenant, e.g. local dev) the feature gate is skipped
/// and the URL is returned directly.
/// </summary>
[Dependency(ReplaceServices = true)]
public class MatomoUrlProvider(
    IEndpointManagementAppService endpointManagementAppService,
    IFeatureChecker featureChecker,
    ICurrentTenant currentTenant)
    : IAnalyticsUrlProvider, ITransientDependency
{
    public async Task<string> GetMatomoUrlAsync()
    {
        try
        {
            // Feature flag is a per-tenant toggle; skip check in host context (local dev)
            if (currentTenant.Id != null && !await featureChecker.IsEnabledAsync("Unity.Analytics"))
                return string.Empty;

            return await endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.ANALYTICS_MATOMO_BASE);
        }
        catch
        {
            return string.Empty;
        }
    }
}
