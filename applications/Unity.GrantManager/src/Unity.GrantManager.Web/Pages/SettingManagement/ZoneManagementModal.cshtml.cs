using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.SettingManagement;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.SettingManagement
{
    public class ZoneManagementModalModel : AbpPageModel
    {
        [Required]
        [HiddenInput]
        [BindProperty(SupportsGet = true)]
        public string ProviderName { get; set; } = string.Empty;

        [Required]
        [HiddenInput]
        [BindProperty(SupportsGet = true)]
        public string ProviderKey { get; set; } = string.Empty;

        [BindProperty]
        public ZoneGroupDefinitionDto? GroupTemplate { get; set; }

        protected IApplicationUiSettingsAppService UiSettingsAppService { get; }

        public ZoneManagementModalModel(IApplicationUiSettingsAppService uiSettingsAppService)
        {
            UiSettingsAppService = uiSettingsAppService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            ValidateModel();

            if (ProviderName == "F" && Guid.TryParse(ProviderKey, out Guid formId)) {
                GroupTemplate = await UiSettingsAppService.GetForFormAsync(formId);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ValidateModel();

            // NOTE: Cleaning up from JS submission approach
            var updateZoneDtos = GroupTemplate?.Zones
                .SelectMany(z => z.Zones)
                .Select(p => new UpdateZoneDto
                {
                    Name = p.Name,
                    IsEnabled = p.IsEnabled
                }).ToList();

            if (updateZoneDtos != null)
            {
                await UiSettingsAppService.UpdateAsync(
                  ProviderName,
                  ProviderKey,
                  updateZoneDtos
                  );
            }

            return NoContent();
        }

    }
}
