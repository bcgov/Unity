using System.Text.Json.Serialization;

namespace Unity.GrantManager.Intakes
{
    public class ApplicantResult
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("applicant_name")]
        public string? ApplicantName { get; set; }

        [JsonPropertyName("unity_applicant_id")]
        public string? UnityApplicantId { get; set; }

        [JsonPropertyName("bc_society_number")]
        public string? BcSocietyNumber { get; set; }

        [JsonPropertyName("org_number")]
        public string? OrgNumber { get; set; }

        [JsonPropertyName("sector")]
        public string? Sector { get; set; }

        [JsonPropertyName("operating_start_date")]
        public string? OperatingStartDate { get; set; }

        [JsonPropertyName("fiscal_year_day")]
        public string? FiscalYearDay { get; set; }

        [JsonPropertyName("fiscal_year_month")]
        public string? FiscalYearMonth { get; set; }
    }
}
