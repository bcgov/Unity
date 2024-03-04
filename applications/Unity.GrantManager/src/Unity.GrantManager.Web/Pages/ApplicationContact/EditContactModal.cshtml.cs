using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.ApplicationContact;

public class EditContactModal : AbpPageModel
{
    [HiddenInput]
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public ContactModalViewModel? ContactForm { get; set; }
    private readonly IApplicationContactService _applicationContactService;
    
    public EditContactModal(IApplicationContactService applicationContactService)
    {
        _applicationContactService = applicationContactService;
    }

    public async Task OnGetAsync()
    {
        ApplicationContactDto applicationContactDto = await _applicationContactService.GetAsync(Id);
        ContactForm = ObjectMapper.Map<ApplicationContactDto, ContactModalViewModel>(applicationContactDto!);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ApplicationContactDto createDto = ObjectMapper.Map<ContactModalViewModel, ApplicationContactDto>(ContactForm!);
        await _applicationContactService.UpdateAsync(createDto);
        return NoContent();
    }
}
