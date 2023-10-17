using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.GrantManager.Intakes;

namespace Unity.GrantManager.Web.Pages.Intakes;

public class CreateModalModel : GrantManagerPageModel
{
    [BindProperty]
    public CreateUpdateIntakeDto? Intake { get; set; }

    private readonly IIntakeAppService _intakeAppService;

    public CreateModalModel(IIntakeAppService intakeAppService)
    {
        _intakeAppService = intakeAppService;
    }    

    public void OnGet()
    {
        Intake = new();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _intakeAppService.CreateAsync(Intake!);
        return NoContent();
    }
}
