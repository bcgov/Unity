using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Intakes;

namespace Unity.GrantManager.Web.Pages.Intakes;

public class UpdateModalModel : GrantManagerPageModel
{
    [HiddenInput]
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateIntakeDto? Intake { get; set; }

    private readonly IIntakeAppService _intakeAppService;

    public UpdateModalModel(IIntakeAppService intakeAppService)
    {
        _intakeAppService = intakeAppService;
    }

    public async Task OnGetAsync()
    {
        var intakeDto = await _intakeAppService.GetAsync(Id);
        Intake = ObjectMapper.Map<IntakeDto, CreateUpdateIntakeDto>(intakeDto);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _intakeAppService.UpdateAsync(Id, Intake!);
        return NoContent();
    }
}
