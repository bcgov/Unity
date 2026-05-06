using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Unity.GrantManager.Integrations;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.Features;

namespace Unity.GrantManager.Web.Pages.AIReporting
{
    public class IndexModel(
        IEndpointManagementAppService endpointManagementAppService,
        IFeatureChecker featureChecker,
        IAuthorizationService authorizationService) : PageModel
    {
        public bool CanViewAiReporting { get; private set; }
        public string ReportingAiApiBaseUrl { get; private set; } = string.Empty;

        public async Task OnGetAsync()
        {
            CanViewAiReporting = await featureChecker.IsEnabledAsync("Unity.AIReporting")
                || (await authorizationService.AuthorizeAsync(User, IdentityConsts.ITAdminPolicyName)).Succeeded;

            if (!CanViewAiReporting)
            {
                return;
            }

            ReportingAiApiBaseUrl = await endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.REPORTING_AI);
        }
    }
}
