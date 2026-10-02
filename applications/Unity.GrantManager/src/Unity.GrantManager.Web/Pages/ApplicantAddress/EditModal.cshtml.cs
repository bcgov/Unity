using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile.Addresses;
using Unity.GrantManager.GrantApplications;
using Unity.Modules.Shared;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.ApplicantAddress;

[Authorize(UnitySelector.ApplicantManagement.Addresses.Update)]
public class EditModal(IApplicantAddressAppService applicantAddressAppService) : AbpPageModel
{
    [BindProperty]
    public ApplicantAddressModalViewModel? AddressForm { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id, Guid applicantId)
    {
        var address = await applicantAddressAppService.GetAsync(applicantId, id);

        if (!address.IsEditable)
        {
            return NotFound();
        }

        AddressForm = new ApplicantAddressModalViewModel
        {
            ApplicantId = applicantId,
            Id = address.Id,
            AddressType = AddressTypeMapper.FromPortalValue(address.AddressType),
            Street = address.Street,
            Street2 = address.Street2,
            Unit = address.Unit,
            City = address.City,
            Province = address.Province,
            PostalCode = address.PostalCode,
            IsPrimary = address.IsPrimary
        };
        AddressForm.EnsureAddressTypeOptions();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (AddressForm is null)
        {
            return BadRequest();
        }

        AddressForm.EnsureAddressTypeOptions();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        await applicantAddressAppService.UpdateAsync(
            AddressForm.ApplicantId,
            AddressForm.Id,
            new UpdateApplicantProfileAddressDto
            {
                AddressType = AddressForm.AddressType,
                Street = AddressForm.Street,
                Street2 = AddressForm.Street2,
                Unit = AddressForm.Unit,
                City = AddressForm.City,
                Province = AddressForm.Province,
                PostalCode = AddressForm.PostalCode,
                IsPrimary = AddressForm.IsPrimary
            });

        return NoContent();
    }
}
