using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Unity.Flex.Web.Views.Shared.Components.CurrencyDefinitionWidget
{
    public class CurrencyDefinitionViewModel : WorksheetFieldDefinitionViewModelBase
    {
        public CurrencyDefinitionViewModel() : base()
        {
        }

        [DisplayName("Min")]
        [BindProperty]
        [Required]
        [Range(typeof(decimal), "-79228162514264337593543950335", "79228162514264337593543950335")]
        public decimal Min { get; set; } = 0m;

        [DisplayName("Max")]
        [BindProperty]
        [Required]
        [Range(typeof(decimal), "-79228162514264337593543950335", "79228162514264337593543950335")]
        public decimal Max { get; set; } = decimal.MaxValue;
    }
}
