using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
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

    public async Task OnGetAsync()
    {
        if (Id is Guid id)
        {
            var dto = await modelAppService.GetAsync(id);
            Input = new ModelViewModel
            {
                Name = dto.Name,
                Provider = dto.Provider,
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

        [DisplayName("AISetup:IsActive")]
        public bool IsActive { get; set; } = true;
    }
}
