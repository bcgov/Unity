using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Unity.GrantManager.Locality;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantOrganizationInfo
{
    public class ApplicantOrganizationInfoViewModel
    {
        public Guid ApplicantId { get; set; }

        public List<SelectListItem> OrgStatusList { get; set; } = [];
        public List<SelectListItem> OrganizationTypeList { get; set; } = [];
        public List<SelectListItem> FiscalDayList { get; set; } = [];
        public List<SelectListItem> FiscalMonthList { get; set; } = [];
        public List<SelectListItem> SectorList { get; set; } = [];
        public List<SelectListItem> SubSectorList { get; set; } = [];        
        public List<SectorDto> Sectors { get; set; } = [];
        public string? SelectedOrgBookId { get; set; }

        // Organization Summary Section
        [Display(Name = "Applicant ID")]
        public string UnityApplicantId { get; set; } = string.Empty;

        [Display(Name = "Applicant Name")]
        public string ApplicantName { get; set; } = string.Empty;

        [Display(Name = "Registered Organization Name")]
        public string OrgName { get; set; } = string.Empty;

        [Display(Name = "Registered Organization Number")]
        public string OrgNumber { get; set; } = string.Empty;

        [Display(Name = "Business Number")]
        public string BusinessNumber { get; set; } = string.Empty;

        [Display(Name = "Org Book Status")]
        public string OrgStatus { get; set; } = string.Empty;

        [Display(Name = "Organization Type")]
        public string OrganizationType { get; set; } = string.Empty;

        [Display(Name = "Organization Size (Approximate Number of Employees)")]
        public string OrganizationSize { get; set; } = string.Empty;
        
        [Display(Name = "Non-Registered Organization Name")]
        public string NonRegOrgName { get; set; } = string.Empty;
              
        // Sector Information Section
        [Display(Name = "Sector")]
        public string Sector { get; set; } = string.Empty;

        [Display(Name = "Sub-Sector")]
        public string SubSector { get; set; } = string.Empty;

        [Display(Name = "Indigenous")]
        public bool IndigenousOrgInd { get; set; } = false;

        [Display(Name = "Other Sector/Sub/Industry Description")]
        [TextArea(Rows = 2)]
        public string SectorSubSectorIndustryDesc { get; set; } = string.Empty;

        // Financial Information Section
        [Display(Name = "Fiscal Year End Month")]
        public string FiscalMonth { get; set; } = string.Empty;

        [Display(Name = "Fiscal Year End Day")]
        public string FiscalDay { get; set; } = string.Empty;

        [Display(Name = "Started Operating Date")]
        public DateTime? StartedOperatingDate { get; set; }

        [Display(Name = "Red-Stop")]
        public bool RedStop { get; set; } = false;
    }
}
