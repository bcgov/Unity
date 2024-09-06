using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Unity.Flex.Worksheets.Definitions;

namespace Unity.Flex.Web.Views.Shared.Components.SelectListDefinitionWidget
{
    public class SelectListDefinitionViewModel : WorksheetFieldDefinitionViewModelBase
    {
        public SelectListDefinitionViewModel() : base()
        {
        }

        [BindProperty]
        public List<SelectListOption> Options { get; set; } = [];


        [BindProperty]
        public List<string> Keys { get; set; } = [];

        [BindProperty]
        public List<string> Values { get; set; } = [];
    }
}
