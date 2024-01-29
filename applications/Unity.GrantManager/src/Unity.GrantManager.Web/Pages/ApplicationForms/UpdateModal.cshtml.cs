using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Forms;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Web.Pages.ApplicationForms.ViewModels;

namespace Unity.GrantManager.Web.Pages.ApplicationForms;

public class UpdateModalModel : GrantManagerPageModel
{
    [HiddenInput]
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateApplicationFormViewModel? ApplicationForm { get; set; }

    private readonly IApplicationFormAppService _applicationFormAppService;
    private readonly IIntakeAppService _intakeAppService;

    public UpdateModalModel(IApplicationFormAppService applicationFormAppService,
        IIntakeAppService intakeAppService)
    {
        _applicationFormAppService = applicationFormAppService;
        _intakeAppService = intakeAppService;
    }

    public async Task OnGetAsync()
    {

        ApplicationForm = new();

        var applicationFormDto = await _applicationFormAppService.GetAsync(Id);
        ApplicationForm = ObjectMapper.Map<ApplicationFormDto, CreateUpdateApplicationFormViewModel>(applicationFormDto);

        var intakes = await _intakeAppService.GetListAsync(new Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto());
        ApplicationForm.IntakesList = intakes.Items.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.IntakeName }).ToList();
        var selected = ApplicationForm.IntakesList.Find(s => s.Value == applicationFormDto.IntakeId.ToString());
        
        if (selected != null)
        {
            selected.Selected = true;
        }                
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var updateDto = ObjectMapper.Map<CreateUpdateApplicationFormViewModel, CreateUpdateApplicationFormDto>(ApplicationForm!);
        await _applicationFormAppService.UpdateAsync(Id, updateDto);
        return NoContent();
    }
}
