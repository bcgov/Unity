using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.AI.Prompts;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.AI.Web.Pages.Prompts;

public class CreateModalModel : AbpPageModel
{
    [BindProperty]
    public CreateUpdateAIPromptDto Prompt { get; set; } = new();

    private readonly IAIPromptAppService _promptAppService;

    public CreateModalModel(IAIPromptAppService promptAppService)
    {
        _promptAppService = promptAppService;
    }

    public void OnGet()
    {
        Prompt = new CreateUpdateAIPromptDto { IsActive = true };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _promptAppService.CreateAsync(Prompt);
        return NoContent();
    }
}
