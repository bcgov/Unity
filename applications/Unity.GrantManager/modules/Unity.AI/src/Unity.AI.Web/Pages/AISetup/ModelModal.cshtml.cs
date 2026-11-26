using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Unity.AI.Setup;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.AI.Web.Pages.AISetup;

[Authorize(IdentityConsts.ITAdminPolicyName)]
public class ModelModalModel(IAIModelAppService modelAppService) : AbpPageModel
{
    [HiddenInput]
    [BindProperty(SupportsGet = true)]
    public Guid? Id { get; set; }

    [BindProperty]
    public ModelViewModel Input { get; set; } = new();

    public bool IsNew => Id is null;

    public List<SelectListItem> TypeOptions { get; } = AISetupConsts.ModelTypes
        .Select(type => new SelectListItem(type, type))
        .ToList();

    public async Task OnGetAsync()
    {
        if (Id is Guid id)
        {
            var dto = await modelAppService.GetAsync(id);
            Input = new ModelViewModel
            {
                Name = dto.Name,
                Provider = dto.Provider,
                Type = dto.Type,
                ApiVersion = dto.ApiVersion,
                IsActive = dto.IsActive
            };
        }

    }

    public async Task<IActionResult> OnPostAsync()
    {
        var input = new CreateUpdateAIModelDto
        {
            Name = Input.Name,
            Provider = Input.Provider,
            Type = Input.Type,
            ApiVersion = Input.ApiVersion,
            IsActive = Input.IsActive
        };

        if (Id is Guid id)
        {
            await modelAppService.UpdateAsync(id, input);
        }
        else
        {
            await modelAppService.CreateAsync(input);
        }

        return NoContent();
    }

    public class ModelViewModel
    {
        [Required]
        [StringLength(AISetupConsts.NameMaxLength)]
        [DisplayName("AISetup:DeploymentName")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [DisplayName("AISetup:Provider")]
        public string Provider { get; set; } = AISetupConsts.OpenAIProvider;

        [Required]
        [DisplayName("AISetup:Type")]
        public string Type { get; set; } = AISetupConsts.ChatModelType;

        [Required]
        [DisplayName("AISetup:ApiVersion")]
        public string ApiVersion { get; set; } = string.Empty;

        [DisplayName("AISetup:IsActive")]
        public bool IsActive { get; set; } = true;
    }
}
