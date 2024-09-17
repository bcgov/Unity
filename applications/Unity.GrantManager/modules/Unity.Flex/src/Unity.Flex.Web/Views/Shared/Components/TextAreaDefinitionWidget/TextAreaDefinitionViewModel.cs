using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Unity.Flex.Web.Views.Shared.Components.TextAreaDefinitionWidget
{
    public class TextAreaDefinitionViewModel : WorksheetFieldDefinitionViewModelBase
    {
        public TextAreaDefinitionViewModel() : base()
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

        [DisplayName("Rows")]
        [BindProperty]
        [Required]
        [Range(1, uint.MaxValue)]
        public uint Rows { get; set; } = 1;
    }
}
