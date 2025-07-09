using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
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
        if (Id == Guid.Empty)
        {
            throw new ArgumentException("Id cannot be empty", nameof(Id));
        }

        Site = new();
        var siteDto = await siteAppService.GetAsync(Id);
        Site = ObjectMapper.Map<SiteDto, CreateUpdateSiteViewModel>(siteDto);

        var addressParts = new[]
        {
            siteDto.AddressLine1,
            siteDto.AddressLine2,
            siteDto.AddressLine3,
            siteDto.City,
            siteDto.Province,
            siteDto.PostalCode,
            siteDto.Country
        };

        Site.MailingAddress = string.Join(", ", addressParts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var updateDto = ObjectMapper.Map<CreateUpdateSiteViewModel, SiteDto>(Site!);
        await siteAppService.UpdatePaygroupAsync(updateDto.PaymentGroup, updateDto.Id);
        return NoContent();
    }
}
