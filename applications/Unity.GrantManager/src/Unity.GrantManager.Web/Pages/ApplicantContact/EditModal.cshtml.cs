using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.ApplicantContact;

public class EditModal : AbpPageModel
{
    private readonly IApplicantContactAppService _applicantContactAppService;

    [BindProperty]
    public ApplicantContactModalViewModel? ContactForm { get; set; }

    public EditModal(IApplicantContactAppService applicantContactAppService)
    {
        _applicantContactAppService = applicantContactAppService;
    }

    public async Task<IActionResult> OnGetAsync(Guid id, Guid applicantId)
    {
        var contactInfo = await _applicantContactAppService.GetByApplicantIdAsync(applicantId);
        var contact = contactInfo.Contacts.FirstOrDefault(c => c.ContactId == id);

        if (contact is null || !contact.IsEditable)
        {
            return NotFound();
        }

        ContactForm = ObjectMapper.Map<ApplicantProfile.ProfileData.ContactInfoItemDto, ApplicantContactModalViewModel>(contact);
        ContactForm.ApplicantId = applicantId;
        ContactForm.Id = contact.ContactId;
        ContactForm.EnsureRoleOptions();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ContactForm is null)
        {
            return BadRequest();
        }

        ContactForm.EnsureRoleOptions();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var updateDto = ObjectMapper.Map<ApplicantContactModalViewModel, UpdateApplicantContactDto>(ContactForm);
        await _applicantContactAppService.UpdateAsync(ContactForm.ApplicantId, ContactForm.Id, updateDto);

        return NoContent();
    }
}
