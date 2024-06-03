using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;

namespace Unity.Payments.Web.Pages.PaymentConfigurations
{
    public class PaymentConfigurationViewModel
    {
        [DisplayName("Ministry Client")]
        [Required]
        [RegularExpression("([a-zA-Z0-9]+)", ErrorMessage = "{0} does not match the pattern of only non-special characters.")]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "{0} must have a minimum of {1} characters.")]
        [MaxLength(3, ErrorMessage = "{0} must have a max of {1} characters.")]
        [DisplayOrder(10004)]
        public string MinistryClient { get; set; } = string.Empty;

        [DisplayName("Responsibility")]
        [Required]
        [RegularExpression("([a-zA-Z0-9]+)", ErrorMessage = "{0} does not match the pattern of only non-special characters.")]
        [StringLength(5, MinimumLength = 5, ErrorMessage = "{0} must have a minimum of {1} characters.")]
        [MaxLength(5, ErrorMessage = "{0} must have a max of {1} characters.")]
        [DisplayOrder(10004)]
        public string Responsibility { get; set; } = string.Empty;

        [DisplayName("Service Line")]
        [Required]
        [RegularExpression("([0-9]+)", ErrorMessage = "{0} does not match the pattern of only digits.")]
        [StringLength(5, MinimumLength = 5, ErrorMessage = "{0} must have a minimum of {1} digits.")]
        [MaxLength(5, ErrorMessage = "{0} must have a minimum of {1} digits.")]
        [DisplayOrder(10004)]
        public string ServiceLine { get; set; } = string.Empty;

        [DisplayName("Stob")]
        [Required]
        [RegularExpression("([0-9]+)", ErrorMessage = "{0} does not match the pattern of only digits.")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "{0} must have a minimum of {1} digits.")]
        [MaxLength(4, ErrorMessage = "{0} must have a max of {1} digit.s")]
        [DisplayOrder(10005)]
        public string Stob { get; set; } = string.Empty;

        [DisplayName("Project #")]
        [Required]
        [RegularExpression("([0-9]+)", ErrorMessage = "{0} does not match the pattern of only digits.")]
        [StringLength(7, MinimumLength = 7, ErrorMessage = "{0} must have a minimum of {1} digits.")]
        [MaxLength(7, ErrorMessage = "{0} must have a max of {1} digits.")]
        [DisplayOrder(10005)]
        public string ProjectNumber { get; set; } = string.Empty;

        //Must be a valid and enabled accont combination in CFS.
        //Format: XXX.XXXXX.XXXXX.XXXX.XXXXXXX.XXXXXX.XXXX
        //        0TW.51OCG.00000.5717.5100000.000000.0000
        // BCGOV_CL.BCGOV_RSP.BCGOV_SRVC.BCGOV_STOB.BCGOV_PROJ + 000000.0000
    }
}

