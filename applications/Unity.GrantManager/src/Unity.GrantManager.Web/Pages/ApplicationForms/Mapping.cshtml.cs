using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.Flex.Worksheets;
using Unity.Flex.Scoresheets;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Forms;
using Unity.GrantManager.Intakes;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Features;
using Unity.Modules.Shared.Correlation;
using Unity.Flex.Worksheets.Definitions;
using Unity.Flex;
using Unity.GrantManager.Applications;
using YamlDotNet.Core.Tokens;

namespace Unity.GrantManager.Web.Pages.ApplicationForms
{
    [Authorize]
    public class MappingModel : AbpPageModel
    {
        public List<SelectListItem> ScoresheetOptionsList { get; set; } = [];

        [BindProperty(SupportsGet = true)]
        public Guid ApplicationId { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid ChefsFormVersionGuid { get; set; }

        private readonly IApplicationFormAppService _applicationFormAppService;
        private readonly IApplicationFormVersionAppService _applicationFormVersionAppService;
        private readonly IWorksheetAppService _worksheetAppService;
        private readonly IScoresheetAppService _scoresheetAppService;
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

        [BindProperty]
        [Display(Name = "")]
        public Guid? ScoresheetId { get; set; }

        [BindProperty]
        public bool FlexEnabled { get; set; }

        public List<SelectListItem> ElectoralDistrictAddressTypes { get; set; } = [];
        public string? ElectoralDistrictAddressType { get; set; }

        public MappingModel(IApplicationFormAppService applicationFormAppService,
                            IApplicationFormVersionAppService applicationFormVersionAppService,
                            IWorksheetAppService worksheetAppService,
                            IScoresheetAppService scoresheetAppService,
                            IFeatureChecker featureChecker)
        {
            _applicationFormAppService = applicationFormAppService;
            _applicationFormVersionAppService = applicationFormVersionAppService;
            _worksheetAppService = worksheetAppService;
            _scoresheetAppService = scoresheetAppService;
            _featureChecker = featureChecker;
        }

        public async Task OnGetAsync()
        {
            ApplicationFormDto = await _applicationFormAppService.GetAsync(ApplicationId);
            ScoresheetId = ApplicationFormDto.ScoresheetId;
            ApplicationFormVersionDtoList = (List<ApplicationFormVersionDto>?)await _applicationFormAppService.GetVersionsAsync(ApplicationFormDto.Id);
            FlexEnabled = await _featureChecker.IsEnabledAsync("Unity.Flex");

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

            SetElectoralDistrictFields();
        }

        private void SetElectoralDistrictFields()
        {
            bool existingApplicationsForForm = false; // Query for existing application for this form type

            ElectoralDistrictAddressType = ApplicationFormDto?
                .ElectoralDistrictAddressType == null ? null
                : ((int)ApplicationFormDto.ElectoralDistrictAddressType).ToString();

            ElectoralDistrictAddressTypes = ApplicationForm
                .GetAvailableElectoralDistrictAddressTypes()
                .Select(x => new SelectListItem
                {
                    Value = ((int)x.AddressType).ToString(),
                    Text = x.DisplayName
                })
                    .ToList();

            // Insert default option at the top
            ElectoralDistrictAddressTypes.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "Please choose..."
            });
        }

        private async Task<List<MapField>> GenerateMappingFieldsAsync()
        {
            IntakeMapping intakeMapping = new();
            List<MapField> properties = [];

            foreach (var property in intakeMapping.GetType().GetProperties())
            {
                var browsable = property.GetCustomAttributes(typeof(BrowsableAttribute), true).Cast<BrowsableAttribute>().SingleOrDefault();
                var displayName = property.GetCustomAttributes(typeof(DisplayNameAttribute), true).Cast<DisplayNameAttribute>().SingleOrDefault();
                var fieldType = property.GetCustomAttributes(typeof(MapFieldTypeAttribute), true).Cast<MapFieldTypeAttribute>().SingleOrDefault();

                if (browsable != null && browsable.IsDefaultAttribute())
                {
                    properties.Add(new MapField()
                    {
                        Name = property.Name,
                        Type = fieldType?.Type ?? "String",
                        IsCustom = false,
                        Label = displayName?.DisplayName ?? property.Name
                    });
                }
            }

            if (await _featureChecker.IsEnabledAsync("Unity.Flex"))
            {
                var scoresheets = await _scoresheetAppService.GetAllPublishedScoresheetsAsync();
                ScoresheetOptionsList = [];

                foreach (var scoresheet in scoresheets)
                {
                    ScoresheetOptionsList.Add(new SelectListItem
                    {
                        Text = $"{scoresheet.Title} ({scoresheet.Name})",
                        Value = scoresheet.Id.ToString()
                    });
                }

                ScoresheetOptionsList = [.. ScoresheetOptionsList.OrderBy(item => item.Text)];

                // Get the available field from the worksheets for the current Form
                var formVersion = await _applicationFormVersionAppService.GetByChefsFormVersionId(ChefsFormVersionGuid);
                var worksheets = await _worksheetAppService.GetListByCorrelationAsync(formVersion?.Id ?? Guid.Empty, CorrelationConsts.FormVersion);

                foreach (var worksheet in worksheets)
                {
                    // Get worksheet name
                    var fields = worksheet
                        .Sections
                            .SelectMany(f => f.Fields)
                        .ToList();

                    properties.AddRange(from CustomFieldDto? field in fields
                                        where field.IsMappable()
                                        select new MapField()
                                        {
                                            Name = $"{field.Name}.{field.Type}",
                                            Type = ConvertCustomType(field.Type),
                                            IsCustom = true,
                                            Label = $"{field.Label} ({worksheet.Name})"
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
                CustomFieldType.BCAddress => "BCAddress",
                CustomFieldType.TextArea => "TextArea",
                CustomFieldType.DataGrid => "DataGrid",
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
