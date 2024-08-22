using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Unity.Flex.Web.Views.Shared.Components.RadioDefinitionWidget
{
    public class RadioDefinitionViewModel : WorksheetFieldDefinitionViewModelBase
    {
        public RadioDefinitionViewModel() : base()
        {
        }

        [DisplayName("Radio Group Label")]
        [BindProperty]
        [Required]
        public string GroupLabel { get; set; } = string.Empty;


        [BindProperty]
        public List<string> Options { get; set; } = [];
    }
}
