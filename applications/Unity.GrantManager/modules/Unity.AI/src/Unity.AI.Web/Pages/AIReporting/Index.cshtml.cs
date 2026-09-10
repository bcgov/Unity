using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Unity.AI.Settings;
using Unity.GrantManager.Integrations;
using Unity.Modules.Shared.Permissions;
using Volo.Abp;
using Volo.Abp.Features;
using Volo.Abp.Settings;

namespace Unity.AI.Web.Pages.AIReporting
{
    public class IndexModel(
        IEndpointManagementAppService endpointManagementAppService,
        IFeatureChecker featureChecker,
        ISettingProvider settingProvider,
        IAuthorizationService authorizationService,
        ILogger<IndexModel> logger) : PageModel
    {
        public bool CanViewAiReporting { get; private set; }
        public string ReportingAiUrl { get; private set; } = string.Empty;

        public async Task OnGetAsync()
        {
            var isItAdmin = (await authorizationService.AuthorizeAsync(User, IdentityConsts.ITAdminPolicyName)).Succeeded;
            var featureAndSettingEnabled = await featureChecker.IsEnabledAsync("Unity.AIReporting")
                && await settingProvider.GetAsync<bool>(AISettings.ReportingEnabled, defaultValue: false);

            CanViewAiReporting = featureAndSettingEnabled || isItAdmin;

            if (!CanViewAiReporting)
            {
                return;
            }

            try
            {
                ReportingAiUrl = await endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.REPORTING_AI);
            }
            catch (UserFriendlyException ex)
            {
                logger.LogWarning(ex, "AI Reporting endpoint is not configured.");
                ReportingAiUrl = string.Empty;
            }
        }
    }
}
