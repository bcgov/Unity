using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Unity.Flex.Worksheets.Definitions;

namespace Unity.Flex.Web.Views.Shared.Components.CheckboxGroupDefinitionWidget
{
    public class CheckboxGroupDefinitionViewModel : WorksheetFieldDefinitionViewModelBase
    {
        public CheckboxGroupDefinitionViewModel() : base()
        {
        }

        [BindProperty]
        public List<CheckboxGroupDefinitionOption> CheckboxOptions { get; set; } = [];


        [BindProperty]
        public List<string> CheckBoxKeys { get; set; } = [];

        [BindProperty]
        public List<string> CheckboxLabels { get; set; } = [];
    }
}
