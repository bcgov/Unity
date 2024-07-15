using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Unity.Flex.Web.Views.Shared.Components.TextDefinitionWidget
{
    public class TextDefinitionViewModel : WorksheetFieldDefinitionViewModelBase
    {
        public TextDefinitionViewModel() : base()
        {
        }

        [DisplayName("Min Length")]
        [BindProperty]
        [Required]
        [Range(uint.MinValue, uint.MaxValue)]
        public uint MinLength { get; set; } = uint.MinValue;

        [DisplayName("Max Length")]
        [BindProperty]
        [Required]
        [Range(uint.MinValue, uint.MaxValue)]
        public uint MaxLength { get; set; } = uint.MaxValue;
    }
}
