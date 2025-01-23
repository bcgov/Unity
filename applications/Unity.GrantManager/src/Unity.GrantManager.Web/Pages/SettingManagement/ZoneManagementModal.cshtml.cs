using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.SettingManagement;
using Unity.GrantManager.Zones;
using Volo.Abp.AspNetCore.Mvc.ApplicationConfigurations;
using Volo.Abp.EventBus.Local;

namespace Unity.GrantManager.Web.Pages.SettingManagement
{
    public class ZoneManagementModalModel : GrantManagerPageModel
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
        public ZoneGroupDefinitionDto GroupTemplate { get; set; }

        protected IApplicationUiSettingsAppService UiSettingsAppService { get; }
        protected ILocalEventBus LocalEventBus { get; }

        public ZoneManagementModalModel(IApplicationUiSettingsAppService uiSettingsAppService, ILocalEventBus localEventBus)
        {
            UiSettingsAppService = uiSettingsAppService;
            LocalEventBus = localEventBus;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            ValidateModel();

            if (ProviderName == "F" && Guid.TryParse(ProviderKey, out Guid formId))
            {
                GroupTemplate = await UiSettingsAppService.GetForFormAsync(formId);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ValidateModel();

            var updateTabs = GroupTemplate.Zones
                .Select(t => new UpdateZoneDto
                {
                    Name = t.Name,
                    IsEnabled = t.IsEnabled
                });

            var updateZones = GroupTemplate.Zones
                .SelectMany(z => z.Zones)
                .Select(p => new UpdateZoneDto
                {
                    Name = p.Name,
                    IsEnabled = p.IsEnabled
                });

            var updateZoneDtos = updateTabs.Concat(updateZones).ToList();
            if (updateZoneDtos != null)
            {
                await UiSettingsAppService.UpdateAsync(
                  ProviderName,
                  ProviderKey,
                  updateZoneDtos
                  );
            }

            await LocalEventBus.PublishAsync(
                new CurrentApplicationConfigurationCacheResetEventData()
            );

            return NoContent();
        }

    }
}
