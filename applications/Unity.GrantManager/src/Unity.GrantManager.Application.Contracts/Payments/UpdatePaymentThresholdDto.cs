using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;


namespace Unity.GrantManager.Payments
{
    public class UpdatePaymentThresholdDto
    {
        [Required]
        [HiddenInput(DisplayValue = false)]
        public Guid? UserId { get; set; }

        [DisplayName("User Name")]
        [ReadOnlyInput] // This attribute is now resolved with the added namespace  
        [DisabledInput]
        [ReadOnly(true)]
        public string? UserName { get; set; }

        [Required]
        [DisplayName("Approval Threshold")]
        [Range(0, 9999999999.99)]
        [DataType(DataType.Currency)]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Invalid amount format.")]
        public decimal? Threshold { get; set; }

        [DisplayName("Payment Threshold Description")]
        public string? Description { get; set; }
    }
}
