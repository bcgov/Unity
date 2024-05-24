using System.ComponentModel;

namespace Unity.GrantManager.Intakes
{
    public class IntakeMapping
    {
        [DisplayName("Acquisition")]
        public string? Acquisition { get; set; }

        [DisplayName("Applicant Name")]
        public string? ApplicantName { get; set; }

        [DisplayName("Approximate Number Of Employees")]
        public string? ApproxNumberOfEmployees { get; set; }

        [DisplayName("Community")]
        public string? Community { get; set; }

        [DisplayName("Community Population")]
        public string? CommunityPopulation { get; set; }

        [DisplayName("Confirmation ID")]
        public string? ConfirmationId { get; set; }

        [DisplayName("Contact Email")]
        public string? ContactEmail { get; set; } = string.Empty;

        [DisplayName("Contact Name")]
        public string? ContactName { get; set; } = string.Empty;

        [DisplayName("Contact Phone")]
        public string? ContactPhone { get; set; } = string.Empty;

        [DisplayName("Contact Phone 2")]
        public string? ContactPhone2 { get; set; } = string.Empty;

        [DisplayName("Contact Title")]
        public string? ContactTitle { get; set; } = string.Empty;

        [DisplayName("Economic Region")]
        public string? EconomicRegion { get; set; }

        [DisplayName("Electoral District")]
        public string? ElectoralDistrict { get; set; }

        [DisplayName("Forestry")]
        public string? Forestry { get; set; }

        [DisplayName("Forestry Focus")]
        public string? ForestryFocus { get; set; }

        [DisplayName("Indigenous Organization Indicator")]
        public string? IndigenousOrgInd { get; set; }

        [DisplayName("Mailing City")]
        public string? MailingCity { get; set; }

        [DisplayName("Mailing Country")]
        public string? MailingCountry { get; set; }

        [DisplayName("Mailing Postal")]
        public string? MailingPostal { get; set; }

        [DisplayName("Mailing Province")]
        public string? MailingProvince { get; set; }

        [DisplayName("Mailing Street")]
        public string? MailingStreet { get; set; }

        [DisplayName("Mailing Street 2")]
        public string? MailingStreet2 { get; set; }

        [DisplayName("Mailing Unit")]
        public string? MailingUnit { get; set; }

        [DisplayName("Non-Registered Business Name")]
        public string? NonRegisteredBusinessName { get; set; }

        [DisplayName("Organization Type")]
        public string? OrganizationType { get; set; }

        [DisplayName("Organization Name")]
        public string? OrgName { get; set; }

        [DisplayName("Organization Number")]
        public string? OrgNumber { get; set; }

        [DisplayName("Organization Status")]
        public string? OrgStatus { get; set; }

        [DisplayName("Physical City")]
        public string? PhysicalCity { get; set; }

        [DisplayName("Physical Country")]
        public string? PhysicalCountry { get; set; }

        [DisplayName("Physical Postal")]
        public string? PhysicalPostal { get; set; }

        [DisplayName("Physical Province")]
        public string? PhysicalProvince { get; set; }

        [DisplayName("Physical Street")]
        public string? PhysicalStreet { get; set; }

        [DisplayName("Physical Street 2")]
        public string? PhysicalStreet2 { get; set; }

        [DisplayName("Physical Unit")]
        public string? PhysicalUnit { get; set; }

        [DisplayName("Place")]
        public string? Place { get; set; }

        [DisplayName("Project End Date")]
        public string? ProjectEndDate { get; set; }

        [DisplayName("Project Name")]
        public string? ProjectName { get; set; }

        [DisplayName("Project Start Date")]
        public string? ProjectStartDate { get; set; }

        [DisplayName("Regional District")]
        public string? RegionalDistrict { get; set; }

        [DisplayName("Requested Amount")]
        public string? RequestedAmount { get; set; }

        [DisplayName("Sector")]
        public string? Sector { get; set; }

        [DisplayName("Submission Date")]
        public string? SubmissionDate { get; set; }

        [DisplayName("Submission ID")]
        public string? SubmissionId { get; set; }

        [DisplayName("Sub-Sector")]
        public string? SubSector { get; set; }

        [DisplayName("Total Project Budget")]
        public string? TotalProjectBudget { get; set; }

        [DisplayName("Sub-Sector Industry Description")]
        public string? SectorSubSectorIndustryDesc { get; set; }

        [DisplayName("Signing Authority Full Name")]
        public string? SigningAuthorityFullName { get; set; }

        [DisplayName("Signing Authority Title")]
        public string? SigningAuthorityTitle { get; set; }

        [DisplayName("Signing Authority Email")]
        public string? SigningAuthorityEmail { get; set; }

        [DisplayName("Signing Authority Business Phone")]
        public string? SigningAuthorityBusinessPhone { get; set; }

        [DisplayName("Signing Authority Cell Phone")]
        public string? SigningAuthorityCellPhone { get; set; }
    }
}
