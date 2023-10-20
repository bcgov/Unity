namespace Unity.GrantManager.Intakes
{
    public class IntakeMapping
    {
        // Application Fields
        public string? ProjectName { get; set; }
        public string? TotalProjectBudget { get; set; }
        public string? RequestedAmount { get; set; }
        public string? ConfirmationId { get; set; }
        public string? SubmissionId { get; set; }
        public string? SubmissionDate { get; set; }

        // Applicant
        public string? ApplicantName { get; set; } 
        public string? NonRegisteredBusinessName { get; set; }
        public string? ApproxNumberOfEmployees { get; set; }
        public string? IndigenousOrgInd { get; set; }

        public string? OrgName { get; set; }
        public string? OrgNumber { get; set; }
        public string? OrgStatus { get; set; }
        public string? OrganizationType { get; set; }
        public string? Sector { get; set; }
        public string? SubSector { get; set; } // Naics codes?

        // Address fields?
        public string? EconomicRegion { get; set; }
        public string? Community { get; set; }
        public string? ElectoralDistrict { get; set; }

        // Address (Mailing Address and Physical Address)
        public string? MailingCity { get; set; }
        public string? MailingCountry { get; set; }
        public string? MailingProvince { get; set; }
        public string? MailingPostal { get; set; }
        public string? MailingStreet { get; set; }
        public string? MailingStreet2 { get; set; }
        public string? MailingUnit { get; set; }

        public string? PhysicalCity { get; set; }
        public string? PhysicalProvince { get; set; }
        public string? PhysicalCountry { get; set; }
        public string? PhysicalPostal { get; set; }
        public string? PhysicalStreet { get; set; }
        public string? PhysicalStreet2 { get; set; }
        public string? PhysicalUnit { get; set; }
    }
}
