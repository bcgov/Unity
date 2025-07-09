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

        [DisplayName("Site #")]
        [ReadOnlyInput]
        public string Number { get; set; } = null!;

        [Required]
        [DisplayName("Payment Group")]
        public PaymentGroup PaymentGroup { get; set; }

        [ReadOnlyInput]
        [DisplayName("Bank Account")]
        public string? BankAccount { get; set; }

        [ReadOnlyInput]
        [DisplayName("Mailing Address")]
        public string? MailingAddress { get; set; }

        [ReadOnlyInput]
        [DisplayName("Status")]
        public string? Status { get; set; }


        [HiddenInput]
        public Guid Id { get; set; }

        // Property to display a warning if PaymentGroup is 1 and BankAccount is not set
        [HiddenInput]
        public bool ShowBankAccountWarning =>
            (int)PaymentGroup == 1 && string.IsNullOrWhiteSpace(BankAccount);

        [HiddenInput]
        public string BankAccountWarningMessage =>
            "Warning: A bank account is required for EFT payments. If you select EFT without one, your payment will fail. Please contact CAS to add a bank account.";
    }
}
