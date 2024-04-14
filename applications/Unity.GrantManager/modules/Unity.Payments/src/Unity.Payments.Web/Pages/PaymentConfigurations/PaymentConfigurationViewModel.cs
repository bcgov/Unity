using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;

namespace Unity.Payments.Web.Pages.PaymentConfigurations
{
    public class PaymentConfigurationViewModel
    {
        [DisplayName("Ministry Client")]
        [Required]
        [RegularExpression("([0-9]+)", ErrorMessage = "{0} does not match the pattern of only digits.")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "{0} must have a minimum of {1} digits.")]
        [MaxLength(4, ErrorMessage = "{0} must have a max of {1} digits.")]
        [DisplayOrder(10004)]
        public string MinistryClient { get; set; } = string.Empty;

        [DisplayName("Responsibility")]
        [Required]
        [RegularExpression("([0-9]+)", ErrorMessage = "{0} does not match the pattern of only digits.")]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "{0} must have a minimum of {1} digits.")]
        [MaxLength(3, ErrorMessage = "{0} must have a max of {1} digits.")]
        [DisplayOrder(10004)]
        public string Responsibility { get; set; } = string.Empty;

        [DisplayName("Service Line")]
        [Required]
        [RegularExpression("([0-9]+)", ErrorMessage = "{0} does not match the pattern of only digits.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "{0} must have a minimum of {1} digits.")]
        [MaxLength(6, ErrorMessage = "{0} must have a minimum of {1} digits.")]
        [DisplayOrder(10004)]
        public string ServiceLine { get; set; } = string.Empty;

        [DisplayName("Stob")]
        [Required]
        [RegularExpression("([0-9]+)", ErrorMessage = "{0} does not match the pattern of only digits.")]
        [StringLength(8, MinimumLength = 8, ErrorMessage = "{0} must have a minimum of {1} digits.")]
        [MaxLength(8, ErrorMessage = "{0} must have a max of {1} digit.s")]
        [DisplayOrder(10005)]
        public string Stob { get; set; } = string.Empty;

        [DisplayName("Project #")]
        [Required]
        [RegularExpression("([0-9]+)", ErrorMessage = "{0} does not match the pattern of only digits.")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "{0} must have a minimum of {1} digits.")]
        [MaxLength(10, ErrorMessage = "{0} must have a max of {1} digits.")]
        [DisplayOrder(10005)]
        public string ProjectNumber { get; set; } = string.Empty;
    }
}

