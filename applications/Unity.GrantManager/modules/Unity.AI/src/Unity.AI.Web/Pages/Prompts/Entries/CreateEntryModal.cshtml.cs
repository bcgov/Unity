using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.AI.Prompts;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.AI.Web.Pages.Prompts.Entries;

public class CreateEntryModalModel : AbpPageModel
{
    [BindProperty]
    public CreateUpdateAIPromptDto Prompt { get; set; } = new();

    private readonly IAIPromptAppService _promptAppService;

    public CreateEntryModalModel(IAIPromptAppService promptAppService)
    {
        _promptAppService = promptAppService;
    }

    public void OnGet(Guid promptId)
    {
        Prompt = new CreateUpdateAIPromptDto
        {
            PromptId = promptId
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _promptAppService.CreateAsync(Prompt);
        return NoContent();
    }
}
