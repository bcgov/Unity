using System;
using System.ComponentModel.DataAnnotations;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantInfo;

public class ApplicantAddressViewModel
{
    public Guid ApplicantAddressId { get; set; }
    public Guid ApplicantId { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.AddressType")]
    public AddressType AddressType { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.Street")]
    public string Street { get; set; } = string.Empty;

    [Display(Name = "ApplicantInfoView:ApplicantInfo.Street2")]
    public string Street2 { get; set; } = string.Empty;

    [Display(Name = "ApplicantInfoView:ApplicantInfo.Unit")]
    public string? Unit { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.City")]
    public string? City { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.Province")]
    public string? Province { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.PostalCode")]
    public string? PostalCode { get; set; }
}

