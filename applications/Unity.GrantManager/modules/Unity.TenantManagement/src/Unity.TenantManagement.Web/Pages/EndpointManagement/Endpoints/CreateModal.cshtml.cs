using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.GrantManager.Integrations;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.EndpointManagement;

[Authorize(IdentityConsts.ITOperationsPolicyName)]
public class CreateModalModel(IEndpointManagementAppService endpointManagementAppService) : AbpPageModel
{
    [BindProperty]
    public CreateUpdateDynamicUrlDto Endpoint { get; set; }


    public void OnGet()
    {
        Endpoint = new();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await endpointManagementAppService.CreateAsync(Endpoint!);
        return NoContent();
    }
}
