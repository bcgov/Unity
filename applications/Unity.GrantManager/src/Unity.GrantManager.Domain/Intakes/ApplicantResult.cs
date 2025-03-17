using System.Text.Json.Serialization;

namespace Unity.GrantManager.Intakes
{
    public class ApplicantResult
    {
        public string? Id { get; set; } = string.Empty;
        public string? ApplicantName { get; set; } = string.Empty;
        public string? UnityApplicantId { get; set; } = string.Empty;
        public string? BcSocietyNumber { get; set; } = string.Empty;
        public string? OrgName { get; set; } = string.Empty;
        public string? OrgNumber { get; set; } = string.Empty;
        public string? BusinessNumber { get; set; } = string.Empty;
        public string? Sector { get; set; } = string.Empty;
        public string? OperatingStartDate { get; set; } = string.Empty;
        public string? FiscalYearDay { get; set; } = string.Empty;
        public string? FiscalYearMonth { get; set; } = string.Empty;
        public bool? RedStop { get; set; }
        public string? IndigenousOrgInd { get; set; } = string.Empty;
        public string? PhysicalAddressUnit { get; set; } = string.Empty;
        public string? PhysicalAddressLine1 { get; set; } = string.Empty;
        public string? PhysicalAddressLine2 { get; set; } = string.Empty;
        public string? PhysicalAddressPostal { get; set; } = string.Empty;
        public string? PhysicalAddressCity { get; set; } = string.Empty;
        public string? PhysicalAddressProvince { get; set; } = string.Empty;
        public string? PhysicalAddressCountry { get; set; } = string.Empty;
        public string? MailingAddressUnit { get; set; } = string.Empty;
        public string? MailingAddressLine1 { get; set; } = string.Empty;
        public string? MailingAddressLine2 { get; set; } = string.Empty;
        public string? MailingAddressPostal { get; set; } = string.Empty;
        public string? MailingAddressCity { get; set; } = string.Empty;
        public string? MailingAddressProvince { get; set; } = string.Empty;
        public string? MailingAddressCountry { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; } = string.Empty;
        public string? PhoneExtension { get; set; } = string.Empty;
    }
}
