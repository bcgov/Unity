using Microsoft.AspNetCore.Mvc;
using System;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Settings;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Zones;
using Volo.Abp.AspNetCore.Mvc.ApplicationConfigurations;
using Volo.Abp.EventBus.Local;

namespace Unity.GrantManager.Web.Pages.SettingManagement
{
    public class ZoneManagementModel : GrantManagerPageModel
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
        public ZoneGroupDefinitionDto GroupTemplate { get; set; } = new ZoneGroupDefinitionDto();

        protected IZoneManagementAppService UiSettingsAppService { get; }
        protected ILocalEventBus LocalEventBus { get; }
        private readonly IApplicationFormAppService _applicationFormAppService;

        public ZoneManagementModel(IZoneManagementAppService uiSettingsAppService, ILocalEventBus localEventBus, IApplicationFormAppService applicationFormAppService)
        {
            UiSettingsAppService = uiSettingsAppService;
            LocalEventBus = localEventBus;
            _applicationFormAppService = applicationFormAppService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            ValidateModel();
            if (ProviderName == FormSettingManagementProvider.ProviderName)
            {
                if (!Guid.TryParse(ProviderKey, out var formId))
                {
                    return BadRequest();
                }

                var form = await _applicationFormAppService.GetAsync(formId);
                if (form.IsHiddenFromList)
                {
                    return NotFound();
                }
            }

            GroupTemplate = await UiSettingsAppService.GetAsync(ProviderName, ProviderKey);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ValidateModel();
            if (ProviderName == FormSettingManagementProvider.ProviderName)
            {
                if (!Guid.TryParse(ProviderKey, out var formId))
                {
                    return BadRequest();
                }

                var form = await _applicationFormAppService.GetAsync(formId);
                if (form.IsHiddenFromList)
                {
                    return NotFound();
                }
            }

            if (GroupTemplate == null)
            {
                return BadRequest("GroupTemplate cannot be null.");
            }

            var updateZoneDtos = GroupTemplate.Tabs
                .SelectMany(tab => tab.Zones
                    .Where(zone => !zone.IsConfigurationDisabled)
                    .Select(zone => new UpdateZoneDto { Name = zone.Name, IsEnabled = zone.IsEnabled })
                    .Prepend(new UpdateZoneDto { Name = tab.Name, IsEnabled = tab.IsEnabled }))
                .ToList();

            if (updateZoneDtos.Count != 0)
            {
                await UiSettingsAppService.SetAsync(ProviderName, ProviderKey, updateZoneDtos);
            }

            await LocalEventBus.PublishAsync(
                new CurrentApplicationConfigurationCacheResetEventData()
            );

            return NoContent();
        }

    }
}
