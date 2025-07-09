using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Web.Pages.Sites.ViewModels;
using Unity.Payments.Suppliers;

namespace Unity.GrantManager.Web.Pages.Sites;

public class UpdateModalModel(ISiteAppService siteAppService) : GrantManagerPageModel
{
    [HiddenInput]
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateSiteViewModel? Site { get; set; }

    public async Task OnGetAsync()
    {
        Site = new();
        var siteDto = await siteAppService.GetAsync(Id);
        Site = ObjectMapper.Map<SiteDto, CreateUpdateSiteViewModel>(siteDto);                         
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var updateDto = ObjectMapper.Map<CreateUpdateSiteViewModel, SiteDto>(Site!);
        await siteAppService.UpdatePaygroupAsync(updateDto.PaymentGroup, updateDto.Id);
        return NoContent();
    }
}
