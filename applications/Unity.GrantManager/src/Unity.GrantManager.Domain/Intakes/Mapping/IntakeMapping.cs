using System.ComponentModel;

namespace Unity.GrantManager.Intakes
{
    public class IntakeMapping
    {
        [DisplayName("Acquisition")]
        [MapFieldType("String")]
        public string? Acquisition { get; set; }

        [DisplayName("Applicant Name")]
        [MapFieldType("String")]
        public string? ApplicantName { get; set; }

        [DisplayName("Approximate Number Of Employees")]
        [MapFieldType("Number")]
        public string? ApproxNumberOfEmployees { get; set; }

        [DisplayName("Community")]
        [MapFieldType("String")]
        public string? Community { get; set; }

        [DisplayName("Community Population")]
        [MapFieldType("Number")]
        public string? CommunityPopulation { get; set; }

        [DisplayName("Confirmation ID")]
        [MapFieldType("String")]
        public string? ConfirmationId { get; set; }

        [DisplayName("Contact Email")]
        [MapFieldType("Email")]
        public string? ContactEmail { get; set; } = string.Empty;

        [DisplayName("Contact Name")]
        [MapFieldType("String")]
        public string? ContactName { get; set; } = string.Empty;

        [DisplayName("Contact Phone")]
        [MapFieldType("Phone")]
        public string? ContactPhone { get; set; } = string.Empty;

        [DisplayName("Contact Phone 2")]
        [MapFieldType("Phone")]
        public string? ContactPhone2 { get; set; } = string.Empty;

        [DisplayName("Contact Title")]
        [MapFieldType("String")]
        public string? ContactTitle { get; set; } = string.Empty;

        [DisplayName("Economic Region")]
        [MapFieldType("String")]
        public string? EconomicRegion { get; set; }

        [DisplayName("Electoral District")]
        [MapFieldType("String")]
        public string? ElectoralDistrict { get; set; }

        [DisplayName("Forestry")]
        [MapFieldType("String")]
        public string? Forestry { get; set; }

        [DisplayName("Forestry Focus")]
        [MapFieldType("String")]
        public string? ForestryFocus { get; set; }

        [DisplayName("Indigenous Organization Indicator")]
        [MapFieldType("String")]
        public string? IndigenousOrgInd { get; set; }

        [DisplayName("Mailing City")]
        [MapFieldType("String")]
        public string? MailingCity { get; set; }

        [DisplayName("Mailing Country")]
        [MapFieldType("String")]
        public string? MailingCountry { get; set; }

        [DisplayName("Mailing Postal")]
        [MapFieldType("String")]
        public string? MailingPostal { get; set; }

        [DisplayName("Mailing Province")]
        [MapFieldType("String")]
        public string? MailingProvince { get; set; }

        [DisplayName("Mailing Street")]
        [MapFieldType("String")]
        public string? MailingStreet { get; set; }

        [DisplayName("Mailing Street 2")]
        [MapFieldType("String")]
        public string? MailingStreet2 { get; set; }

        [DisplayName("Mailing Unit")]
        [MapFieldType("String")]
        public string? MailingUnit { get; set; }

        [DisplayName("Non-Registered Business Name")]
        [MapFieldType("String")]
        public string? NonRegisteredBusinessName { get; set; }

        [DisplayName("Organization Type")]
        [MapFieldType("String")]
        public string? OrganizationType { get; set; }

        [DisplayName("Organization Name")]
        [MapFieldType("String")]
        public string? OrgName { get; set; }

        [DisplayName("Organization Number")]
        [MapFieldType("String")]
        public string? OrgNumber { get; set; }

        [DisplayName("Organization Status")]
        [MapFieldType("String")]
        public string? OrgStatus { get; set; }

        [DisplayName("Physical City")]
        [MapFieldType("String")]
        public string? PhysicalCity { get; set; }

        [DisplayName("Physical Country")]
        [MapFieldType("String")]
        public string? PhysicalCountry { get; set; }

        [DisplayName("Physical Postal")]
        [MapFieldType("String")]
        public string? PhysicalPostal { get; set; }

        [DisplayName("Physical Province")]
        [MapFieldType("String")]
        public string? PhysicalProvince { get; set; }

        [DisplayName("Physical Street")]
        [MapFieldType("String")]
        public string? PhysicalStreet { get; set; }

        [DisplayName("Physical Street 2")]
        [MapFieldType("String")]
        public string? PhysicalStreet2 { get; set; }

        [DisplayName("Physical Unit")]
        [MapFieldType("String")]
        public string? PhysicalUnit { get; set; }

        [DisplayName("Place")]
        [MapFieldType("String")]
        public string? Place { get; set; }

        [DisplayName("Project End Date")]
        [MapFieldType("Date")]
        public string? ProjectEndDate { get; set; }

        [DisplayName("Project Name")]
        [MapFieldType("String")]
        public string? ProjectName { get; set; }

        [DisplayName("Project Start Date")]
        [MapFieldType("Date")]
        public string? ProjectStartDate { get; set; }

        [DisplayName("Regional District")]
        [MapFieldType("String")]
        public string? RegionalDistrict { get; set; }

        [DisplayName("Requested Amount")]
        [MapFieldType("Currency")]
        public string? RequestedAmount { get; set; }

        [DisplayName("Sector")]
        [MapFieldType("String")]
        public string? Sector { get; set; }

        [DisplayName("Submission Date")]
        [MapFieldType("Date")]
        public string? SubmissionDate { get; set; }

        [DisplayName("Submission ID")]
        [MapFieldType("String")]
        public string? SubmissionId { get; set; }

        [DisplayName("Sub-Sector")]
        [MapFieldType("String")]
        public string? SubSector { get; set; }

        [DisplayName("Total Project Budget")]
        [MapFieldType("Currency")]
        public string? TotalProjectBudget { get; set; }

        [DisplayName("Sub-Sector Industry Description")]
        [MapFieldType("String")]
        public string? SectorSubSectorIndustryDesc { get; set; }

        [DisplayName("Signing Authority Full Name")]
        [MapFieldType("String")]
        public string? SigningAuthorityFullName { get; set; }

        [DisplayName("Signing Authority Title")]
        [MapFieldType("String")]
        public string? SigningAuthorityTitle { get; set; }

        [DisplayName("Signing Authority Email")]
        [MapFieldType("Email")]
        public string? SigningAuthorityEmail { get; set; }

        [DisplayName("Signing Authority Business Phone")]
        [MapFieldType("Phone")]
        public string? SigningAuthorityBusinessPhone { get; set; }

        [DisplayName("Signing Authority Cell Phone")]
        [MapFieldType("Phone")]
        public string? SigningAuthorityCellPhone { get; set; }

        [DisplayName("Risk Ranking")]
        [MapFieldType("String")]
        public string? RiskRanking  { get; set; }

        [DisplayName("Project Summary")]
        [MapFieldType("String")]
        public string? ProjectSummary { get; set; }
    }
}
