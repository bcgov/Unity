using System;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantOrganizationInfo
{
    public class ApplicantOrganizationInfoViewModel
    {
        public Guid ApplicantId { get; set; }

        // Organization Summary Section
        [Display(Name = "Unity Applicant ID")]
        public string UnityApplicantId { get; set; } = string.Empty;

        [Display(Name = "Applicant Name")]
        public string ApplicantName { get; set; } = string.Empty;

        [Display(Name = "Organization Number")]
        public string OrgNumber { get; set; } = string.Empty;

        [Display(Name = "Business Number")]
        public string BusinessNumber { get; set; } = string.Empty;

        [Display(Name = "Organization Status")]
        public string OrgStatus { get; set; } = string.Empty;

        [Display(Name = "Organization Type")]
        public string OrganizationType { get; set; } = string.Empty;

        [Display(Name = "Organization Size")]
        public string OrganizationSize { get; set; } = string.Empty;

        [Display(Name = "Status")]
        public string Status { get; set; } = string.Empty;

        [Display(Name = "Non-Registered Business Name")]
        public string NonRegisteredBusinessName { get; set; } = string.Empty;

        [Display(Name = "Non-Registered Organization Name")]
        public string NonRegOrgName { get; set; } = string.Empty;

        [Display(Name = "Approximate Number of Employees")]
        public string ApproxNumberOfEmployees { get; set; } = string.Empty;

        // Sector Information Section
        [Display(Name = "Sector")]
        public string Sector { get; set; } = string.Empty;

        [Display(Name = "Sub-Sector")]
        public string SubSector { get; set; } = string.Empty;

        [Display(Name = "Indigenous Organization")]
        public bool IndigenousOrgInd { get; set; } = false;

        [Display(Name = "Industry Description")]
        public string SectorSubSectorIndustryDesc { get; set; } = string.Empty;

        // Financial Information Section
        [Display(Name = "Fiscal Year End Month")]
        public string FiscalMonth { get; set; } = string.Empty;

        [Display(Name = "Fiscal Year End Day")]
        public string FiscalDay { get; set; } = string.Empty;

        [Display(Name = "Started Operating Date")]
        public string StartedOperatingDate { get; set; } = string.Empty;

        // Payment Information Section
        [Display(Name = "Supplier ID")]
        public string SupplierId { get; set; } = string.Empty;

        [Display(Name = "Site ID")]
        public string SiteId { get; set; } = string.Empty;

        [Display(Name = "Electoral District")]
        public string ElectoralDistrict { get; set; } = string.Empty;

        // Status Information Section
        [Display(Name = "Match Percentage")]
        public string MatchPercentage { get; set; } = string.Empty;

        [Display(Name = "Is Duplicated")]
        public bool IsDuplicated { get; set; } = false;

        [Display(Name = "Red Stop")]
        public bool RedStop { get; set; } = false;
    }
}
