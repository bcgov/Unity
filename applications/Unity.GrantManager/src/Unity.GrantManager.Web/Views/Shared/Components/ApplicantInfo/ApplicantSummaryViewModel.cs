using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantInfo;

public class ApplicantSummaryViewModel
{
    [HiddenInput]
    public Guid ApplicantId { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.OrgName")]
    public string? OrgName { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.OrgNumber")]
    public string? OrgNumber { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.OrgBookStatus")]
    [SelectItems(nameof(ApplicantInfoViewModel.OrgBookStatusList))]
    public string? OrgStatus { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.OrganizationType")]
    [SelectItems(nameof(ApplicantInfoViewModel.OrganizationTypeList))]
    public string? OrganizationType { get; set; }


    [Display(Name = "ApplicantInfoView:ApplicantInfo.NonRegOrgName")]
    public string? NonRegOrgName { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.OrganizationSize")]
    public string? OrganizationSize { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.IndigenousOrgInd")]
    public bool IndigenousOrgInd { get; set; } = false;

    [Display(Name = "ApplicantInfoView:ApplicantInfo.UnityApplicant")]
    public string? UnityApplicantId { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.FiscalMonth")]
    [SelectItems(nameof(ApplicantInfoViewModel.FiscalMonthList))]
    public string? FiscalMonth { get; set; }
    [Display(Name = "ApplicantInfoView:ApplicantInfo.FiscalDay")]
    [SelectItems(nameof(ApplicantInfoViewModel.FiscalDayList))]
    public string? FiscalDay { get; set; }


    [Display(Name = "ApplicantInfoView:ApplicantInfo.Sector")]
    [SelectItems(nameof(ApplicantInfoViewModel.ApplicationSectorsList))]
    public string? Sector { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.SubSector")]
    [SelectItems(nameof(ApplicantInfoViewModel.ApplicationSubSectorsList))]
    public string? SubSector { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.SectorSubSectorIndustryDesc")]
    [TextArea(Rows = 2)]
    public string? SectorSubSectorIndustryDesc { get; set; }

    public bool RedStop { get; set; }

    public string? ApplicantName { get; set; }
}

