using System;
using System.ComponentModel.DataAnnotations;

namespace Unity.Payments.Web.Views.Shared.Components.SupplierInfo;

public class SupplierInfoViewModel
{
    [Display(Name = "ApplicantInfoView:ApplicantInfo:SupplierNumber")]
    public string? SupplierNumber { get; set; }
    public Guid SupplierId { get; set; }
    public Guid SupplierCorrelationId { get; set; }
    public string SupplierCorrelationProvider { get; set; } = string.Empty;
}
