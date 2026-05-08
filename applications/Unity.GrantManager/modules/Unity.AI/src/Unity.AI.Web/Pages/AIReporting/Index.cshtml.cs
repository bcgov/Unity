using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Integrations;
using Unity.Modules.Shared.Permissions;
using Volo.Abp;
using Volo.Abp.Features;

namespace Unity.AI.Web.Pages.AIReporting
{
    public class IndexModel(
        IEndpointManagementAppService endpointManagementAppService,
        IFeatureChecker featureChecker,
        IAuthorizationService authorizationService,
        ILogger<IndexModel> logger) : PageModel
    {
        public bool CanViewAiReporting { get; private set; }
        public string ReportingAiUrl { get; private set; } = string.Empty;

        public async Task OnGetAsync()
        {
            CanViewAiReporting = await featureChecker.IsEnabledAsync("Unity.AIReporting")
                || (await authorizationService.AuthorizeAsync(User, IdentityConsts.ITAdminPolicyName)).Succeeded;

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
