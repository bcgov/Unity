using System.Threading.Tasks;
using Unity.GrantManager.Integrations;
using Volo.Abp.DependencyInjection;

namespace Unity.AspNetCore.Mvc.UI.Theme.UX2.Analytics
{
    public class AnalyticsUrlProvider(IEndpointManagementAppService endpointManagementAppService) : IAnalyticsUrlProvider, ITransientDependency
    {
        public async Task<string> GetMatomoUrlAsync()
        {
            return await endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.ANALYTICS_MATOMO_BASE);
        }
    }
}