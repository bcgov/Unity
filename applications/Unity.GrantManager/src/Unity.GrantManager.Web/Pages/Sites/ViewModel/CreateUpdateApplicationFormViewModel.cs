using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Unity.Payments.Enums;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;

namespace Unity.GrantManager.Web.Pages.Sites.ViewModels
{
    public class CreateUpdateSiteViewModel
    {
        [HiddenInput]
        public Guid Id { get; set; }

        [DisplayName("Site #")]
        [ReadOnlyInput]
        public string Number { get; set; } = null!;

        [Required]
        [DisplayName("Payment Group")]
        public PaymentGroup PaymentGroup { get; set; }

        [ReadOnlyInput]
        [DisplayName("Email Address")]
        public string? EmailAddress { get; set; }
        [ReadOnlyInput]
        [DisplayName("Status")]
        public string? Status { get; set; }

        [ReadOnlyInput]
        [DisplayName("Bank Account")]
        public string? BankAccount { get; set; }

        // Property to display a warning if PaymentGroup is 1 and BankAccount is not set
        [HiddenInput]
        public bool ShowBankAccountWarning =>
            (int)PaymentGroup == 1 && string.IsNullOrWhiteSpace(BankAccount);

        [HiddenInput]
        public string BankAccountWarningMessage =>
            "Warning: Bank Account is required when Payment Group is set to EFT.";
    }
}
