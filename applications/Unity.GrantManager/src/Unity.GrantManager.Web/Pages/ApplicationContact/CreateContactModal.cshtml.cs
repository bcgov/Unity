using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.ApplicationContact;

public class CreateContactModal : AbpPageModel
{
    [BindProperty]
    public ContactModalViewModel? ContactForm { get; set; }
    private readonly IApplicationContactService _applicationContactService;
    
    public CreateContactModal(IApplicationContactService applicationContactService)
    {
        _applicationContactService = applicationContactService;
    }

    public void OnGet(Guid applicationId)
    {
        ContactForm = new ContactModalViewModel{
            ApplicationId = applicationId
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ApplicationContactDto createDto = ObjectMapper.Map<ContactModalViewModel, ApplicationContactDto>(ContactForm!);
        await _applicationContactService.CreateAsync(createDto);
        return NoContent();
    }
}
