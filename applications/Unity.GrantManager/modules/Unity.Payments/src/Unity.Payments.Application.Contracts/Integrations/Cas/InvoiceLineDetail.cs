using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Unity.Payments.Integrations.Cas
{
    public class InvoiceLineDetail : IValidatableObject
    {
        [Required]
        [JsonPropertyName("invoiceLineNumber")]
        public int InvoiceLineNumber { get; set; }

        [Required]
        [MaxLength(4, ErrorMessage = "invoiceLineType must be 'Item'")]
        [JsonPropertyName("invoiceLineType")]
        public string InvoiceLineType { get; set; } = "Item";

        [Required]
        [MaxLength(2, ErrorMessage = "lineCode must be 'DR' or 'CR'")]
        [JsonPropertyName("lineCode")]
        public string? LineCode { get; set; } = "DR";

        [Required]
        [JsonPropertyName("invoiceLineAmount")]
        public decimal InvoiceLineAmount { get; set; }  // Format: 9(12).99

        [Required]
        [MaxLength(40)]
        [JsonPropertyName("defaultDistributionAccount")]
        public string? DefaultDistributionAccount { get; set; } = "";

        [JsonPropertyName("description")]
        public string? Description { get; set; } = "";

        [MaxLength(30)]
        public string? TaxClassificationCode { get; set; } = "";

        [MaxLength(30)]
        [JsonPropertyName("distributionSupplier")]
        public string? DistributionSupplier { get; set; } = "";

        [MaxLength(25)]
        [JsonPropertyName("info1")]
        public string? Info1 { get; set; } = "";

        [MaxLength(10)]
        [JsonPropertyName("info2")]
        public string? Info2 { get; set; } = "";

        [MaxLength(8)]
        [JsonPropertyName("info3")]
        public string? Info3 { get; set; } = "";

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!(LineCode == "DR" || LineCode == "CR"))
            {
                yield return new ValidationResult("Invalid LineCode");
            }
        }
    }
}