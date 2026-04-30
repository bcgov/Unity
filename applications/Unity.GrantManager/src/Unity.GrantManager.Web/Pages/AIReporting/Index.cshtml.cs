using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Unity.GrantManager.Integrations;

namespace Unity.GrantManager.Web.Pages.AIReporting
{
    public class IndexModel(IEndpointManagementAppService endpointManagementAppService) : PageModel
    {
        public string ReportingAiApiBaseUrl { get; private set; } = string.Empty;

        public async Task OnGetAsync()
        {
            ReportingAiApiBaseUrl = await endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.REPORTING_AI);
        }
    }
}
