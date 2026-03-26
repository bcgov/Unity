using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.AI.Prompts;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.AI.Web.Pages.Prompts.Versions;

public class EditVersionModalModel : AbpPageModel
{
    [HiddenInput]
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateAIPromptVersionDto Version { get; set; } = new();

    private readonly IAIPromptVersionAppService _versionAppService;

    public EditVersionModalModel(IAIPromptVersionAppService versionAppService)
    {
        _versionAppService = versionAppService;
    }

    public async Task OnGetAsync()
    {
        var dto = await _versionAppService.GetAsync(Id);
        Version = new CreateUpdateAIPromptVersionDto
        {
            PromptId = dto.PromptId,
            VersionNumber = dto.VersionNumber,
            SystemPrompt = dto.SystemPrompt,
            UserPromptTemplate = dto.UserPromptTemplate,
            DeveloperNotes = dto.DeveloperNotes,
            TargetModel = dto.TargetModel,
            TargetProvider = dto.TargetProvider,
            Temperature = dto.Temperature,
            MaxTokens = dto.MaxTokens,
            IsPublished = dto.IsPublished,
            IsDeprecated = dto.IsDeprecated,
            MetadataJson = dto.MetadataJson
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _versionAppService.UpdateAsync(Id, Version);
        return NoContent();
    }
}
