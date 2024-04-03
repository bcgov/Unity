using System.ComponentModel.DataAnnotations;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System;
using System.Collections.Immutable;


namespace Unity.Payments.Web.Views.Shared.Components.SupplierInfo;

public class SupplierInfoViewModel
{
    [Display(Name = "ApplicantInfoView:ApplicantInfo.SupplierNumber")]
    public string? SupplierNumber { get; set; }
}
