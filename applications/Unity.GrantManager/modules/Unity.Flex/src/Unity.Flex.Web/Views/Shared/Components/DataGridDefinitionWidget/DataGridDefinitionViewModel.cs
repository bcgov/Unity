using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Unity.Flex.Worksheets;
using Unity.Flex.Worksheets.Definitions;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;

namespace Unity.Flex.Web.Views.Shared.Components.DataGridDefinitionWidget
{
    public class DataGridDefinitionViewModel : WorksheetFieldDefinitionViewModelBase
    {
        [DisplayName("Dynamic")]
        public bool Dynamic { get; set; }

        public List<DataGridDefinitionColumn> Columns { get; set; } = [];


        private readonly List<SelectListItem> _supportedFieldTypes;
        public List<SelectListItem> SupportedFieldTypes { get { return _supportedFieldTypes; } }

        private readonly List<SelectListItem> _summaryOptions;
        public List<SelectListItem> SummaryOptions { get { return _summaryOptions; } }


        [SelectItems(nameof(SummaryOptions))]
        [DisplayName("Summary Option")]
        public string SummaryOption { get; set; } = DataGridDefinitionSummaryOption.None.ToString();


        [BindProperty]
        public string SupportedFieldsList => GetSupportedFieldsList();

        private string GetSupportedFieldsList()
        {
            return string.Join(",", _supportedFieldTypes.Select(s => s.Text));
        }

        public DataGridDefinitionViewModel() : base()
        {
            _supportedFieldTypes =
            [
                AddGridSupportedCustomFieldType(CustomFieldType.Text),
                AddGridSupportedCustomFieldType(CustomFieldType.TextArea),
                AddGridSupportedCustomFieldType(CustomFieldType.Currency),
                AddGridSupportedCustomFieldType(CustomFieldType.Numeric),
                AddGridSupportedCustomFieldType(CustomFieldType.Date),
                AddGridSupportedCustomFieldType(CustomFieldType.DateTime),
                AddGridSupportedCustomFieldType(CustomFieldType.YesNo),
                AddGridSupportedCustomFieldType(CustomFieldType.Checkbox),
                AddGridSupportedCustomFieldType(CustomFieldType.Phone),
                AddGridSupportedCustomFieldType(CustomFieldType.Email)
            ];

            _summaryOptions =
            [
                AddSummaryOption(DataGridDefinitionSummaryOption.None),
                AddSummaryOption(DataGridDefinitionSummaryOption.Above),
                AddSummaryOption(DataGridDefinitionSummaryOption.Below)
            ];
        }

        private static SelectListItem AddSummaryOption(DataGridDefinitionSummaryOption type)
        {
            var typeStr = type.ToString();
            return new SelectListItem(typeStr, typeStr);
        }

        private static SelectListItem AddGridSupportedCustomFieldType(CustomFieldType type)
        {
            var typeStr = type.ToString();
            return new SelectListItem(typeStr, typeStr);
        }
    }
}
