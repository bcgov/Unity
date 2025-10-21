using System;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantOrganizationInfo
{
    public class ApplicantOrganizationInfoViewModel
    {
        public Guid ApplicantId { get; set; }

        // Organization Summary Section
        public string UnityApplicantId { get; set; } = string.Empty;
        public string ApplicantDisplayName { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public string OrgNumber { get; set; } = string.Empty;
        public string BusinessNumber { get; set; } = string.Empty;
        public string OrgStatus { get; set; } = string.Empty;
        public string OrganizationType { get; set; } = string.Empty;
        public string OrganizationSize { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string NonRegisteredBusinessName { get; set; } = string.Empty;
        public string NonRegOrgName { get; set; } = string.Empty;
        public string ApproxNumberOfEmployees { get; set; } = string.Empty;

        // Sector Information Section
        public string Sector { get; set; } = string.Empty;
        public string SubSector { get; set; } = string.Empty;
        public bool IndigenousOrgInd { get; set; } = false;
        public string SectorSubSectorIndustryDesc { get; set; } = string.Empty;

        // Financial Information Section
        public string FiscalMonth { get; set; } = string.Empty;
        public string FiscalDay { get; set; } = string.Empty;
        public string StartedOperatingDate { get; set; } = string.Empty;

        // Payment Information Section
        public string SupplierId { get; set; } = string.Empty;
        public string SiteId { get; set; } = string.Empty;
        public string ElectoralDistrict { get; set; } = string.Empty;

        // Status Information Section
        public string MatchPercentage { get; set; } = string.Empty;
        public bool IsDuplicated { get; set; } = false;
        public bool RedStop { get; set; } = false;
    }
}
