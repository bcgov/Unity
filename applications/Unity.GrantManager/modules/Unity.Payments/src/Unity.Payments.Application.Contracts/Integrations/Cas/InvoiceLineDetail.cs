using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Unity.Payments.Integrations.Cas
{
    public class InvoiceLineDetail : IValidatableObject
    {
        [Required]
        public int invoiceLineNumber { get; set; }

        [Required]
        [MaxLength(4, ErrorMessage = "invoiceLineType must be 'Item'")]
        public string invoiceLineType { get; set; } = "Item";

        [Required]
        [MaxLength(2, ErrorMessage = "lineCode must be 'DR' or 'CR'")]
        public string? lineCode { get; set; } = "DR";

        [Required]
        public decimal invoiceLineAmount { get; set; }  // Format: 9(12).99

        [Required]
        [MaxLength(40)]
        public string? defaultDistributionAccount { get; set; } = "";

        public string? description { get; set; } = "";

        [MaxLength(30)]
        public string? taxClassificationCode { get; set; } = "";

        [MaxLength(30)]
        public string? distributionSupplier { get; set; } = "";

        [MaxLength(25)]
        public string? info1 { get; set; } = "";

        [MaxLength(10)]
        public string? info2 { get; set; } = "";

        [MaxLength(8)]
        public string? info3 { get; set; } = "";

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!(lineCode == "DR" || lineCode == "CR"))
            {
                yield return new ValidationResult("Invalid LineCode");
            }
        }
    }
}