using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Unity.GrantManager.Integrations;
namespace Unity.GrantManager.Web.Pages.EndpointManagement;

public class UpdateModalModel(IEndpointManagementAppService endpointManagementAppService) : AbpPageModel
{
    [HiddenInput]
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateDynamicUrlDto Endpoint { get; set; }

    public async Task OnGetAsync()
    {
        var endpointDto = await endpointManagementAppService.GetAsync(Id);
        Endpoint = ObjectMapper.Map<DynamicUrlDto, CreateUpdateDynamicUrlDto>(endpointDto);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await endpointManagementAppService.UpdateAsync(Id, Endpoint!);
        await endpointManagementAppService.ClearCacheAsync();
        return NoContent();
    }
}
