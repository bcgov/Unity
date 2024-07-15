using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Unity.Flex.Web.Views.Shared.Components.NumericDefinitionWidget
{
    public class NumericDefinitionViewModel : WorksheetFieldDefinitionViewModelBase
    {
        public NumericDefinitionViewModel() : base()
        {
        }

        [DisplayName("Min")]
        [BindProperty]
        [Required]
        [Range(long.MinValue, long.MaxValue)]
        public long Min { get; set; } = 0;

        [DisplayName("Max")]
        [BindProperty]
        [Required]
        [Range(long.MinValue, long.MaxValue)]
        public long Max { get; set; } = long.MaxValue;
    }
}
