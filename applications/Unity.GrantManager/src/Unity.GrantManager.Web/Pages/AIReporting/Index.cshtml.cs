using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using Unity.GrantManager.Integrations;

namespace Unity.GrantManager.Web.Pages.AIReporting
{
    public class IndexModel(IEndpointManagementAppService endpointManagementAppService) : PageModel
    {
        public string ReportingAiUrl { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            ReportingAiUrl = await endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.REPORTING_AI);
        }
    }
}
