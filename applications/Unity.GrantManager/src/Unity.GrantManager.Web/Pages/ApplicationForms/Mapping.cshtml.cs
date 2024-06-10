
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
using Unity.GrantManager.Forms;
using Unity.GrantManager.Intakes;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Features;

namespace Unity.GrantManager.Web.Pages.ApplicationForms
{
    [Authorize]
    public class MappingModel : AbpPageModel
    {

        [BindProperty(SupportsGet = true)]
        public Guid ApplicationId { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid ChefsFormVersionGuid { get; set; }

        private readonly IApplicationFormAppService _applicationFormAppService;
        private readonly IApplicationFormVersionAppService _applicationFormVersionAppService;
        private readonly IWorksheetAppService _worksheetAppService;
        private readonly IFeatureChecker _featureChecker;

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

        public MappingModel(IApplicationFormAppService applicationFormAppService,
                            IApplicationFormVersionAppService applicationFormVersionAppService,
                            IWorksheetAppService worksheetAppService,
                            IFeatureChecker featureChecker)
        {
            _applicationFormAppService = applicationFormAppService;
            _applicationFormVersionAppService = applicationFormVersionAppService;
            _worksheetAppService = worksheetAppService;
            _featureChecker = featureChecker;
        }

        public async Task OnGetAsync()
        {
            ApplicationFormDto = await _applicationFormAppService.GetAsync(ApplicationId);
            ApplicationFormVersionDtoList = (List<ApplicationFormVersionDto>?)await _applicationFormAppService.GetVersionsAsync(ApplicationFormDto.Id);

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
                    CreateUpdateApplicationFormVersionDto appFormVersion = new CreateUpdateApplicationFormVersionDto();
                    appFormVersion.ApplicationFormId = ApplicationFormDto.Id;
                    appFormVersion.ChefsApplicationFormGuid = ApplicationFormDto.ChefsApplicationFormGuid;
                    ApplicationFormVersionDto = await _applicationFormVersionAppService.CreateAsync(appFormVersion);
                }
                else if (ApplicationFormVersionDto == null)
                {
                    ApplicationFormVersionDto = ApplicationFormVersionDtoList.First();
                }

                ApplicationFormVersionDtoString = JsonSerializer.Serialize(ApplicationFormVersionDto);
            }

            IntakeProperties = JsonSerializer.Serialize(await GenerateMappingFieldsAsync());
        }

        private async Task<List<MapField>> GenerateMappingFieldsAsync()
        {
            IntakeMapping intakeMapping = new();
            List<MapField> properties = [];

            foreach (var property in intakeMapping.GetType().GetProperties())
            {
                var displayName = property.GetCustomAttributes(typeof(DisplayNameAttribute), true).Cast<DisplayNameAttribute>().SingleOrDefault();
                var fieldType = property.GetCustomAttributes(typeof(MapFieldTypeAttribute), true).Cast<MapFieldTypeAttribute>().SingleOrDefault();

                properties.Add(new MapField()
                {
                    Name = property.Name,
                    Type = fieldType?.Type ?? "String",
                    IsCustom = false,
                    Label = displayName?.DisplayName ?? property.Name
                });
            }

            if (await _featureChecker.IsEnabledAsync("Unity.Flex"))
            {
                // Get the available field from the worksheets
                var worksheets = await _worksheetAppService.GetListAsync();

                var fields = worksheets.SelectMany(s => s.Sections).SelectMany(s => s.Fields).ToList();

                foreach (var field in fields)
                {
                    properties.Add(new MapField()
                    {
                        Name = $"{field.Name}.{field.Type}",
                        Type = ConvertCustomType(field.Type),
                        IsCustom = true,
                        Label = field.Label
                    });
                }
            }

            return [.. properties.OrderBy(s => s.Label)];
        }

        private static string ConvertCustomType(CustomFieldType type)
        {
            return type switch
            {
                CustomFieldType.Text => "String",
                CustomFieldType.Date => "Date",
                CustomFieldType.Email => "Email",
                CustomFieldType.Phone => "Phone",
                CustomFieldType.DateTime => "Date",
                CustomFieldType.YesNo => "YesNo",
                CustomFieldType.Currency => "Currency",
                CustomFieldType.Numeric => "Number",
                CustomFieldType.Radio => "Radio",
                CustomFieldType.Checkbox => "Checkbox",
                CustomFieldType.CheckboxGroup => "CheckboxGroup",
                CustomFieldType.SelectList => "SelectList",
                _ => "",
            };
        }

        public class MapField
        {
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public bool IsCustom { get; set; }
            public string Label { get; set; } = string.Empty;
        }
    }
}
