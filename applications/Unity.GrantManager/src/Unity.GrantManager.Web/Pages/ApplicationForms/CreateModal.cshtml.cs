using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Forms;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Web.Pages.ApplicationForms.ViewModels;

namespace Unity.GrantManager.Web.Pages.ApplicationForms;

public class CreateModalModel : GrantManagerPageModel
{
    [BindProperty]
    public CreateUpdateApplicationFormViewModel? ApplicationForm { get; set; }

    private readonly IApplicationFormAppService _applicationFormAppService;
    private readonly IIntakeAppService _intakeAppService;
    
    public CreateModalModel(IApplicationFormAppService applicationFormAppService,
        IIntakeAppService intakeAppService)
    {
        _applicationFormAppService = applicationFormAppService;
        _intakeAppService = intakeAppService;
    }

    public async Task OnGetAsync()
    {
        ApplicationForm = new();

        var intakes = await _intakeAppService.GetListAsync(new Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto());
        ApplicationForm.IntakesList = intakes.Items.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.IntakeName }).ToList();        
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var createDto = ObjectMapper.Map<CreateUpdateApplicationFormViewModel, CreateUpdateApplicationFormDto>(ApplicationForm!);
        await _applicationFormAppService.CreateAsync(createDto);
        return NoContent();
    }
}
