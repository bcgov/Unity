using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;
using Unity.GrantManager.ApplicationForms;
using Unity.Payments.Enums;

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

        public List<SelectListItem> PaymentGroupList { get; set; } = new List<SelectListItem>();

        [Display(Name = "Default Payment Group")]
        [SelectItems(nameof(PaymentGroupList))]
        public int? DefaultPaymentGroup { get; set; } = (int)PaymentGroup.EFT;

        public List<SelectListItem> FormHierarchyList { get; set; } = new List<SelectListItem>();

        [Display(Name = "Form Hierarchy")]
        [SelectItems(nameof(FormHierarchyList))]
        public FormHierarchyType? FormHierarchy { get; set; }

        public Guid? ParentFormId { get; set; }
        public Guid? ParentFormVersionId { get; set; }
        public string ParentFormDisplayName { get; set; } = string.Empty;

        public decimal? PaymentApprovalThreshold { get; set; }

        public bool Payable { get; set; }

        public bool PreventAutomaticPaymentToCAS { get; set; }

        public bool HasEditFormPaymentConfiguration { get; set; }
    }
}
