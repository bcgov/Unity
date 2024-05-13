using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Unity.Payments.Integrations.Cas
{
    public class InvoiceLineDetail : IValidatableObject
    {
        [Required]
        public int InvoiceLineNumber { get; set; }

        [Required]
        [MaxLength(4, ErrorMessage = "invoiceLineType must be 'Item'")]
        public string InvoiceLineType { get; set; } = "Item";

        [Required]
        [MaxLength(2, ErrorMessage = "lineCode must be 'DR' or 'CR'")]
        public string? LineCode { get; set; } = "DR";

        [Required]
        public decimal InvoiceLineAmount { get; set; }  // Format: 9(12).99

        [Required]
        [MaxLength(40)]
        public string? DefaultDistributionAccount { get; set; } = "";

        public string? Description { get; set; } = "";

        [MaxLength(30)]
        public string? TaxClassificationCode { get; set; } = "";

        [MaxLength(30)]
        public string? DistributionSupplier { get; set; } = "";

        [MaxLength(25)]
        public string? Info1 { get; set; } = "";

        [MaxLength(10)]
        public string? Info2 { get; set; } = "";

        [MaxLength(8)]
        public string? Info3 { get; set; } = "";

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!(lineCode == "DR" || lineCode == "CR"))
            {
                yield return new ValidationResult("Invalid LineCode");
            }
        }
    }
}