using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.GrantPrograms;

namespace Unity.GrantManager.Web.Pages.GrantPrograms;

public class UpdateModalModel : GrantManagerPageModel
{
    [HiddenInput]
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateGrantProgramDto GrantProgram { get; set; }

    private readonly IGrantProgramAppService _grantProgramAppService;

    public UpdateModalModel(IGrantProgramAppService grantProgramAppService)
    {
        _grantProgramAppService = grantProgramAppService;
    }

    public async Task OnGetAsync()
    {
        var grantProgramDto = await _grantProgramAppService.GetAsync(Id);
        GrantProgram = ObjectMapper.Map<GrantProgramDto, CreateUpdateGrantProgramDto>(grantProgramDto);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _grantProgramAppService.UpdateAsync(Id, GrantProgram);
        return NoContent();
    }
}
