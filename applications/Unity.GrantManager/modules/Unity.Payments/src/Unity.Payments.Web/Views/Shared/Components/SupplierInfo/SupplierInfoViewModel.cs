using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;

namespace Unity.Payments.Web.Views.Shared.Components.SupplierInfo;

public class SupplierInfoViewModel
{
    [Display(Name = "ApplicantInfoView:ApplicantInfo:SupplierNumber")]
    [MaxLength(30, ErrorMessage = "Must be a maximum of 30 characters")]
    public string? SupplierNumber { get; set; }
    [Display(Name = "Business Name")]
    [ReadOnlyInput]
    [DisabledInput]
    public string? SupplierName { get; set; }
    [Display(Name = "Status")]
    [ReadOnlyInput]
    [DisabledInput]
    public string? Status { get; set; }
    [HiddenInput]
    public string? OriginalSupplierNumber { get; set; }
    public Guid SupplierId { get; set; }
    public Guid SupplierCorrelationId { get; set; }
    public string SupplierCorrelationProvider { get; set; } = string.Empty;
}
