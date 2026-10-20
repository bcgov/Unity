using System.Threading.Tasks;
using Unity.GrantManager.Integrations;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Unity.AspNetCore.Mvc.UI.Theme.UX2.Analytics
{
    public class AnalyticsUrlProvider(IEndpointManagementAppService endpointManagementAppService) : IAnalyticsUrlProvider, ITransientDependency
    {
        public async Task<string> GetMatomoUrlAsync()
        {
            try
            {
                return await endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.ANALYTICS_MATOMO_BASE);
            }
            catch (UserFriendlyException)
            {
                // ANALYTICS_MATOMO_BASE is seeded blank; an unconfigured URL means analytics is off
                return string.Empty;
            }
        }
    }
}
