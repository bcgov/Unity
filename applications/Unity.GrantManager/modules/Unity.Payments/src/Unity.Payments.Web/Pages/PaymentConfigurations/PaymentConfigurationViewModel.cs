﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Unity.Payments.Web.Pages.PaymentConfigurations
{
    public class PaymentConfigurationViewModel
    {        
        [DisplayName("Ministry Client")]
        [Required]
        [RegularExpression("([a-zA-Z0-9]+)", ErrorMessage = "{0} does not match the pattern of only non-special characters.")]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "{0} must have a minimum of {1} characters.")]
        [MaxLength(3, ErrorMessage = "{0} must have a max of {1} characters.")]
        public string? MinistryClient { get; set; } = string.Empty;

        [DisplayName("Responsibility")]
        [Required]
        [RegularExpression("([a-zA-Z0-9]+)", ErrorMessage = "{0} does not match the pattern of only non-special characters.")]
        [StringLength(5, MinimumLength = 5, ErrorMessage = "{0} must have a minimum of {1} characters.")]
        [MaxLength(5, ErrorMessage = "{0} must have a max of {1} characters.")]
        public string? Responsibility { get; set; } = string.Empty;

        [DisplayName("Service Line")]
        [Required]
        [RegularExpression("([a-zA-Z0-9]+)", ErrorMessage = "{0} does not match the pattern of only non-special characters.")]
        [StringLength(5, MinimumLength = 5, ErrorMessage = "{0} must have a minimum of {1} characters.")]
        [MaxLength(5, ErrorMessage = "{0} must have a minimum of {1} characters.")]
        public string? ServiceLine { get; set; } = string.Empty;

        [DisplayName("Stob")]
        [Required]
        [RegularExpression("([a-zA-Z0-9]+)", ErrorMessage = "{0} does not match the pattern of only non-special characters.")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "{0} must have a minimum of {1} characters.")]
        [MaxLength(4, ErrorMessage = "{0} must have a max of {1} characters.")]
        public string? Stob { get; set; } = string.Empty;

        [DisplayName("Project #")]
        [Required]
        [RegularExpression("([a-zA-Z0-9]+)", ErrorMessage = "{0} does not match the pattern of only non-special characters.")]
        [StringLength(7, MinimumLength = 7, ErrorMessage = "{0} must have a minimum of {1} characters.")]
        [MaxLength(7, ErrorMessage = "{0} must have a max of {1} characters.")]
        public string? ProjectNumber { get; set; } = string.Empty;
    }
}