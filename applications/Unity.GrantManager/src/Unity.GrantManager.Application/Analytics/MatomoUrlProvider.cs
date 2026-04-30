using System.Threading.Tasks;
using Unity.GrantManager.Integrations;

namespace Unity.GrantManager.Analytics;

/// <summary>
/// Matomo URL provider implementation for Application layer, using IEndpointManagementAppService.
/// </summary>
public class MatomoUrlProvider : IAnalyticsUrlProvider
{
    private readonly IEndpointManagementAppService _endpointManagementAppService;

    public MatomoUrlProvider(IEndpointManagementAppService endpointManagementAppService)
    {
        _endpointManagementAppService = endpointManagementAppService;
    }

    public async Task<string> GetMatomoUrlAsync()
    {
        return await _endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.ANALYTICS_MATOMO_BASE) ?? string.Empty;
    }
}
