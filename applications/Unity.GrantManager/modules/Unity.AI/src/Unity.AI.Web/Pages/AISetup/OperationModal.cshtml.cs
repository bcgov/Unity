using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI.Localization;
using Unity.AI.Setup;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.AI.Web.Pages.AISetup;

[Authorize(IdentityConsts.ITOperationsPolicyName)]
public class OperationModalModel(
    IAIOperationAppService operationAppService,
    IAIModelAppService modelAppService) : AbpPageModel
{
    [HiddenInput]
    [BindProperty(SupportsGet = true)]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public OperationViewModel Operation { get; set; } = new();

    public List<SelectListItem> ModelOptions { get; private set; } = [];

    public List<SelectListItem> ExecutionModeOptions { get; } = AISetupConsts.ExecutionModes
        .Select(mode => new SelectListItem(mode, mode))
        .ToList();

    /// <summary>False when the operation always runs Sequential; the form then shows no choice.</summary>
    public bool UsesExecutionMode { get; private set; }

    /// <summary>Chat, or Embedding for AI Reporting's embedding row.</summary>
    public string RequiredModelType { get; private set; } = AISetupConsts.ChatModelType;

    /// <summary>AI Reporting's rows: always Sequential and active, so those fields are hidden.</summary>
    public bool IsAIReporting { get; private set; }

    public bool IsAIReportingEmbedding =>
        string.Equals(Name, AISetupConsts.AIReportingEmbeddingOperation, StringComparison.OrdinalIgnoreCase);

    public async Task OnGetAsync()
    {
        LocalizationResourceType = typeof(AIResource);
        var dto = await operationAppService.GetAsync(Name);
        Name = dto.Name;
        UsesExecutionMode = dto.UsesExecutionMode;
        RequiredModelType = dto.RequiredModelType;
        IsAIReporting = AISetupConsts.AIReportingOperations.Contains(dto.Name, StringComparer.OrdinalIgnoreCase);
        var models = (await modelAppService.GetListAsync()).Items;

        // Active models only; the current model stays selectable (marked) so the form
        // shows what is saved even after that model was retired.
        ModelOptions = models
            // Only the model type this row needs (Chat, or Embedding for AI Reporting's embedding row).
            .Where(model => model.Type == dto.RequiredModelType)
            .Where(model => model.IsActive || model.Id == dto.AIModelId)
            .Select(model => new SelectListItem(
                model.IsActive ? model.Name : $"{model.Name} {L["AISetup:InactiveSuffix"]}",
                model.Id.ToString()))
            .ToList();

        Operation = new OperationViewModel
        {
            AIModelId = dto.AIModelId
                ?? models.FirstOrDefault(model => model.IsActive && model.Type == dto.RequiredModelType)?.Id
                ?? Guid.Empty,
            ExecutionMode = dto.UsesExecutionMode ? dto.ExecutionMode : AISetupConsts.ExecutionModes[0],
            IsActive = !dto.IsConfigured || dto.IsActive
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await operationAppService.UpdateAsync(Name, new UpdateAIOperationDto
        {
            AIModelId = Operation.AIModelId,
            ExecutionMode = Operation.ExecutionMode,
            IsActive = Operation.IsActive
        });

        return NoContent();
    }

    public class OperationViewModel
    {
        [Required]
        [DisplayName("AISetup:Model")]
        public Guid AIModelId { get; set; }

        [Required]
        [DisplayName("AISetup:ExecutionMode")]
        public string ExecutionMode { get; set; } = AISetupConsts.ExecutionModes[0];

        [DisplayName("AISetup:IsActive")]
        public bool IsActive { get; set; } = true;
    }
}
