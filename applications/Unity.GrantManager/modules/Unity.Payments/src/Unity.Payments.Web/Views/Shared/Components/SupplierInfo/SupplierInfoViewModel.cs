using System.ComponentModel.DataAnnotations;

namespace Unity.Payments.Web.Views.Shared.Components.SupplierInfo;

public class SupplierInfoViewModel
{
    [Display(Name = "ApplicantInfoView:ApplicantInfo:SupplierNumber")]
    public string? SupplierNumber { get; set; }
}
