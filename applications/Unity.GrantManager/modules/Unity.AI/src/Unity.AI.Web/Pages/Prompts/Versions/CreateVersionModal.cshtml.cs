using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.AI.Prompts;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.AI.Web.Pages.Prompts.Versions;

public class CreateVersionModalModel : AbpPageModel
{
    [BindProperty]
    public CreateUpdateAIPromptVersionDto Version { get; set; } = new();

    private readonly IAIPromptVersionAppService _versionAppService;

    public CreateVersionModalModel(IAIPromptVersionAppService versionAppService)
    {
        _versionAppService = versionAppService;
    }

    public void OnGet(Guid promptId)
    {
        Version = new CreateUpdateAIPromptVersionDto
        {
            PromptId = promptId,
            Temperature = 0.2
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _versionAppService.CreateAsync(Version);
        return NoContent();
    }
}
