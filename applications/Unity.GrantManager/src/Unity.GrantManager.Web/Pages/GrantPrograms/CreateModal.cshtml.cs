using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.GrantManager.GrantPrograms;

namespace Unity.GrantManager.Web.Pages.GrantPrograms;

public class CreateModalModel : GrantManagerPageModel
{
    [BindProperty]
    public CreateUpdateGrantProgramDto GrantProgram { get; set; }

    private readonly IGrantProgramAppService _grantProgramAppService;

    public CreateModalModel(IGrantProgramAppService grantProgramAppService)
    {
        _grantProgramAppService = grantProgramAppService;
    }

    public void OnGet()
    {
        GrantProgram = new CreateUpdateGrantProgramDto();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _grantProgramAppService.CreateAsync(GrantProgram);
        return NoContent();
    }
}
