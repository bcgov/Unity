using System.Text.Json.Serialization;

namespace Unity.GrantManager.Intakes
{
    public class ApplicantResult
    {
        public string? Id { get; set; } = string.Empty;
        public string? ApplicantName { get; set; } = string.Empty;
        public string? UnityApplicantId { get; set; } = string.Empty;
        public string? BcSocietyNumber { get; set; } = string.Empty;
        public string? OrgNumber { get; set; } = string.Empty;
        public string? Sector { get; set; } = string.Empty;
        public string? OperatingStartDate { get; set; } = string.Empty;
        public string? FiscalYearDay { get; set; } = string.Empty;
        public string? FiscalYearMonth { get; set; } = string.Empty;
        public string? BusinessNumber { get; set; } = string.Empty;
        public string? PyhsicalAddressUnit { get; set; } = string.Empty;
        public string? PyhsicalAddressLine1 { get; set; } = string.Empty;
        public string? PyhsicalAddressLine2 { get; set; } = string.Empty;
        public string? PyhsicalAddressPostal { get; set; } = string.Empty;
        public string? PyhsicalAddressCity { get; set; } = string.Empty;
        public string? PyhsicalAddressProvince { get; set; } = string.Empty;
        public string? PyhsicalAddressCountry { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; } = string.Empty;
        public string? PhoneExtension { get; set; } = string.Empty;
    }
}
