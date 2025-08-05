using System.ComponentModel;

namespace Unity.GrantManager.Intakes
{
    public class IntakeMapping
    {
        // Used by the CHEFS logon - token fields - mapped to Applicant Agent
        [Browsable(false)]
        public dynamic? ApplicantAgent { get; set; }

        [Browsable(false)]
        public string? UnityApplicantId { get; set; }

        [DisplayName("Acquisition")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? Acquisition { get; set; }

        [DisplayName("Applicant Name")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? ApplicantName { get; set; }

        [DisplayName("Applicant Electoral District")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? ApplicantElectoralDistrict { get; set; }

        [DisplayName("Approximate Number Of Employees")]
        [MapFieldType("Number")]
        [Browsable(true)]
        public string? ApproxNumberOfEmployees { get; set; }

        [DisplayName("Community")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? Community { get; set; }

        [DisplayName("Community Population")]
        [MapFieldType("Number")]
        [Browsable(true)]
        public string? CommunityPopulation { get; set; }

        [DisplayName("Confirmation ID")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? ConfirmationId { get; set; }

        [DisplayName("Contact Email")]
        [MapFieldType("Email")]
        [Browsable(true)]
        public string? ContactEmail { get; set; } = string.Empty;

        [DisplayName("Contact Name")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? ContactName { get; set; } = string.Empty;

        [DisplayName("Contact Phone")]
        [MapFieldType("Phone")]
        [Browsable(true)]
        public string? ContactPhone { get; set; } = string.Empty;

        [DisplayName("Contact Phone 2")]
        [MapFieldType("Phone")]
        [Browsable(true)]
        public string? ContactPhone2 { get; set; } = string.Empty;

        [DisplayName("Contact Title")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? ContactTitle { get; set; } = string.Empty;

        [DisplayName("Economic Region")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? EconomicRegion { get; set; }

        [DisplayName("Project Electoral District")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? ElectoralDistrict { get; set; }

        [DisplayName("Forestry")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? Forestry { get; set; }

        [DisplayName("Forestry Focus")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? ForestryFocus { get; set; }

        [DisplayName("Fiscal Year End(FYE) Day")]
        [MapFieldType("Number")]
        [Browsable(true)]
        public string? FiscalDay { get; set; }

        [DisplayName("Fiscal Year End(FYE) Month")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? FiscalMonth { get; set; }

        [DisplayName("Indigenous Organization Indicator")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? IndigenousOrgInd { get; set; }

        [DisplayName("Mailing City")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? MailingCity { get; set; }

        [DisplayName("Mailing Country")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? MailingCountry { get; set; }

        [DisplayName("Mailing Postal")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? MailingPostal { get; set; }

        [DisplayName("Mailing Province")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? MailingProvince { get; set; }

        [DisplayName("Mailing Street")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? MailingStreet { get; set; }

        [DisplayName("Mailing Street 2")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? MailingStreet2 { get; set; }

        [DisplayName("Mailing Unit")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? MailingUnit { get; set; }

        [DisplayName("Non-Registered Business Name")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? NonRegisteredBusinessName { get; set; }

        [DisplayName("Organization Type")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? OrganizationType { get; set; }

        [DisplayName("Registered Organization Name")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? OrgName { get; set; }

        [DisplayName("Registered Organization Number")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? OrgNumber { get; set; }

        [DisplayName("Organization Status")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? OrgStatus { get; set; }

        [DisplayName("Physical City")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? PhysicalCity { get; set; }

        [DisplayName("Physical Country")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? PhysicalCountry { get; set; }

        [DisplayName("Physical Postal")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? PhysicalPostal { get; set; }

        [DisplayName("Physical Province")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? PhysicalProvince { get; set; }

        [DisplayName("Physical Street")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? PhysicalStreet { get; set; }

        [DisplayName("Physical Street 2")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? PhysicalStreet2 { get; set; }

        [DisplayName("Physical Unit")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? PhysicalUnit { get; set; }

        [DisplayName("Place")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? Place { get; set; }

        [DisplayName("Project End Date")]
        [MapFieldType("Date")]
        [Browsable(true)]
        public string? ProjectEndDate { get; set; }

        [DisplayName("Project Name")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? ProjectName { get; set; }

        [DisplayName("Project Start Date")]
        [MapFieldType("Date")]
        [Browsable(true)]
        public string? ProjectStartDate { get; set; }

        [DisplayName("Regional District")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? RegionalDistrict { get; set; }

        [DisplayName("Requested Amount")]
        [MapFieldType("Currency")]
        [Browsable(true)]
        public string? RequestedAmount { get; set; }

        [DisplayName("Sector")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? Sector { get; set; }

        [DisplayName("Submission Date")]
        [MapFieldType("Date")]
        [Browsable(true)]
        public string? SubmissionDate { get; set; }

        [DisplayName("Submission ID")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? SubmissionId { get; set; }

        [DisplayName("Sub-Sector")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? SubSector { get; set; }

        [DisplayName("Total Project Budget")]
        [MapFieldType("Currency")]
        [Browsable(true)]
        public string? TotalProjectBudget { get; set; }

        [DisplayName("Sub-Sector Industry Description")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? SectorSubSectorIndustryDesc { get; set; }

        [DisplayName("Signing Authority Full Name")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? SigningAuthorityFullName { get; set; }

        [DisplayName("Signing Authority Title")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? SigningAuthorityTitle { get; set; }

        [DisplayName("Signing Authority Email")]
        [MapFieldType("Email")]
        [Browsable(true)]
        public string? SigningAuthorityEmail { get; set; }

        [DisplayName("Signing Authority Business Phone")]
        [MapFieldType("Phone")]
        [Browsable(true)]
        public string? SigningAuthorityBusinessPhone { get; set; }

        [DisplayName("Signing Authority Cell Phone")]
        [MapFieldType("Phone")]
        [Browsable(true)]
        public string? SigningAuthorityCellPhone { get; set; }

        [DisplayName("Risk Ranking")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? RiskRanking  { get; set; }

        [DisplayName("Project Summary")]
        [MapFieldType("String")]
        [Browsable(true)]
        public string? ProjectSummary { get; set; }
    }
}
