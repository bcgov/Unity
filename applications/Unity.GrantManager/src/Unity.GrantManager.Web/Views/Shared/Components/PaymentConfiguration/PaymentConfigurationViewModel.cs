using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;

namespace Unity.GrantManager.Web.Views.Shared.Components.PaymentConfiguration
{
    public class PaymentConfigurationViewModel
    {
        public PaymentConfigurationViewModel()
        {
            
        }

        public List<SelectListItem> AccountCodeList { get; set; } = new List<SelectListItem>();

        [Display(Name = "Account Code")]
        [SelectItems(nameof(AccountCodeList))]
        public Guid? AccountCode { get; set; }

        public decimal? PaymentApprovalThreshold { get; set; }

        public bool Payable { get; set; }

        public bool PreventAutomaticPaymentToCAS { get; set; }

        public bool HasEditFormPaymentConfiguration { get; set; }
    }
}
