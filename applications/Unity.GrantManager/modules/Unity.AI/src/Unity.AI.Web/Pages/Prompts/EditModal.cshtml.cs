using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.AI.Prompts;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.AI.Web.Pages.Prompts;

public class EditModalModel : AbpPageModel
{
    [HiddenInput]
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateAIPromptDto Prompt { get; set; } = new();

    private readonly IAIPromptAppService _promptAppService;

    public EditModalModel(IAIPromptAppService promptAppService)
    {
        _promptAppService = promptAppService;
    }

    public async Task OnGetAsync()
    {
        var dto = await _promptAppService.GetAsync(Id);
        Prompt = new CreateUpdateAIPromptDto
        {
            Name = dto.Name,
            Description = dto.Description,
            Type = dto.Type,
            IsActive = dto.IsActive
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _promptAppService.UpdateAsync(Id, Prompt);
        return NoContent();
    }
}
