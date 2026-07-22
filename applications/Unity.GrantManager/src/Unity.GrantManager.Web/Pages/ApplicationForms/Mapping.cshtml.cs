using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.Flex.Worksheets;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.ApplicationForms.Mapping;
using Unity.GrantManager.Forms;
using Unity.GrantManager.Intakes;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Features;
using Unity.Modules.Shared.Correlation;
using Unity.Flex.Worksheets.Definitions;
using Unity.AI.Settings;
using Unity.Flex;
using Volo.Abp.Settings;

namespace Unity.GrantManager.Web.Pages.ApplicationForms
{
    [Authorize]
    public class MappingModel(IApplicationFormAppService applicationFormAppService,
                        IApplicationFormVersionAppService applicationFormVersionAppService,
                        IApplicationFormVersionMappingReadService mappingReadService,
                        IFeatureChecker featureChecker,
                        ISettingProvider settingProvider) : AbpPageModel
    {

        [BindProperty(SupportsGet = true)]
        public Guid ApplicationId { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid ChefsFormVersionGuid { get; set; }

        [BindProperty]
        public ApplicationFormDto? ApplicationFormDto { get; set; }

        [BindProperty]
        public ApplicationFormVersionDto? ApplicationFormVersionDto { get; set; }

        [BindProperty]
        public List<ApplicationFormVersionDto>? ApplicationFormVersionDtoList { get; set; }

        [BindProperty]
        public string? ApplicationFormVersionDtoString { get; set; }

        [BindProperty]
        public string? IntakeProperties { get; set; }

        [BindProperty]
        public string? MappingSuggestionJson { get; set; }

        [BindProperty]
        public bool FlexEnabled { get; set; }

        public bool ShowAITab { get; set; }
        public bool ShowAutomatic { get; set; }
        public bool ShowManual { get; set; }

        public async Task OnGetAsync()
        {
            ApplicationFormDto = await applicationFormAppService.GetAsync(ApplicationId);
            ApplicationFormVersionDtoList = (List<ApplicationFormVersionDto>?) await applicationFormAppService.GetVersionsAsync(ApplicationFormDto.Id);
            FlexEnabled = await featureChecker.IsEnabledAsync("Unity.Flex");

            ShowAutomatic = await settingProvider.GetAsync<bool>(AISettings.AutomaticGenerationEnabled, defaultValue: false);
            ShowManual = await settingProvider.GetAsync<bool>(AISettings.ManualGenerationEnabled, defaultValue: false);
            ShowAITab = ShowAutomatic || ShowManual;

            if (ApplicationFormVersionDtoList != null)
            {
                foreach (ApplicationFormVersionDto applicationFormVersionDto in ApplicationFormVersionDtoList)
                {
                    if ((applicationFormVersionDto.ChefsFormVersionGuid != null && Guid.Parse(applicationFormVersionDto.ChefsFormVersionGuid) == ChefsFormVersionGuid)
                    || (ChefsFormVersionGuid.ToString() == "00000000-0000-0000-0000-000000000000" && applicationFormVersionDto.Published))
                    {
                        ApplicationFormVersionDto = applicationFormVersionDto;
                        if (ChefsFormVersionGuid.ToString() == "00000000-0000-0000-0000-000000000000" && applicationFormVersionDto.ChefsFormVersionGuid != null)
                        {
                            ChefsFormVersionGuid = Guid.Parse(applicationFormVersionDto.ChefsFormVersionGuid);
                        }
                        break;
                    }
                }

                if (ApplicationFormVersionDtoList.Count == 0 && ApplicationFormVersionDto == null)
                {
                    CreateUpdateApplicationFormVersionDto appFormVersion = new();
                    appFormVersion.ApplicationFormId = ApplicationFormDto.Id;
                    appFormVersion.ChefsApplicationFormGuid = ApplicationFormDto.ChefsApplicationFormGuid;
                    ApplicationFormVersionDto = await applicationFormVersionAppService.CreateAsync(appFormVersion);
                }
                else if (ApplicationFormVersionDto == null)
                {
                    ApplicationFormVersionDto = ApplicationFormVersionDtoList[0];
                }

                ApplicationFormVersionDtoString = JsonSerializer.Serialize(ApplicationFormVersionDto);
            }

            IntakeProperties = JsonSerializer.Serialize(await GenerateMappingFieldsAsync());
        }
        
        private async Task<List<MapField>> GenerateMappingFieldsAsync()
        {
            if (ApplicationFormVersionDto?.Id is not Guid formVersionId || formVersionId == Guid.Empty)
            {
                return [];
            }

            var readModel = await mappingReadService.GetAsync(formVersionId);
            return BuildMappingFields(readModel);
        }

        internal static List<MapField> BuildMappingFields(ApplicationFormMappingReadModelDto readModel)
        {
            var properties = readModel.UnityCoreFields
                .Select(field => new MapField
                {
                    Name = field.Name,
                    Type = field.Type,
                    IsCustom = field.IsCustom,
                    Label = field.Label
                })
                .Concat(readModel.Worksheets.SelectMany(worksheet => worksheet.Fields).Select(field => new MapField
                {
                    Name = field.Name,
                    Type = field.Type,
                    IsCustom = field.IsCustom,
                    Label = field.Label
                }))
                .ToList();

            return [.. properties.OrderBy(s => s.Label)];
        }

        public class MapField
        {
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public bool IsCustom { get; set; }
            public string Label { get; set; } = string.Empty;
        }
    }

    public static class MappingExtensionMethods
    {
        public static bool IsMappable(this CustomFieldDto? fieldDto)
        {
            if (fieldDto == null) return false;

            // This is not effecient, needs to be moved.
            return fieldDto.Type switch
            {
                CustomFieldType.DataGrid => IsDataGridMappable(fieldDto),
                _ => true // default
            };
        }

        private static bool IsDataGridMappable(CustomFieldDto fieldDto)
        {
            if (fieldDto.Definition == null) return true; // mappable by default
            var definition = (DataGridDefinition?)(fieldDto.Definition?.ConvertDefinition(CustomFieldType.DataGrid));
            return definition?.Dynamic ?? true;
        }
    }
}
