using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace Unity.Payments.Integrations.Cas
{
    public class Invoice
    {
        [Required]
        [MaxLength(25)]
        public string? InvoiceType { get; set; } = "Standard";
        public string? PoNumber { get; set; } = ""; // Must be NULL
        public string? SupplierName { get; set; } = "";

        [Required]
        [MaxLength(30)]
        public string? SupplierNumber { get; set; }

        [Required]
        [MaxLength(3)]
        public string? SupplierSiteNumber { get; set; }

        [Required]
        public string? InvoiceDate { get; set; } // Format: 21-FEB-2017

        [Required]
        [MaxLength(40)]
        public string? InvoiceNumber { get; set; }

        [Required]
        public decimal InvoiceAmount { get; set; } // Format: 9(12).99

        [Required]
        [MaxLength(7)]
        public string? PayGroup { get; set; } = "GEN EFT"; 

        [Required]
        public string? DateInvoiceReceived { get; set; } // Format: 21-FEB-2017

        public string? dDateGoodsReceived { get; set; } = "";  // Format: 21-FEB-2017

        [Required]
        [MaxLength(2)]
        public string? RemittanceCode { get; set; } = "01"; // 01 - Your Invoice Reference

        [Required]
        [MaxLength(1)]
        public string? SpecialHandling { get; set; } = "N";

        [MaxLength(4)]
        public string? BankNumber { get; set; } = "";

        [MaxLength(5)]
        public string? BranchNumber { get; set; } = "";

        [MaxLength(12)]
        public string? AccountNumber { get; set; } = "";

        [MaxLength(1)]
        public string? EftAdviceFlag { get; set; }= "";

        [MaxLength(35)]
        public string? EftEmailAddress { get; set; }= "";

        [MaxLength(40)]
        public string? NameLine1 { get; set; }= "";

        [MaxLength(40)]
        public string NameLine2 { get; set; }= "";

        [MaxLength(150)]
        public string? QualifiedReceiver { get; set; } = "";

        [Required]
        [MaxLength(50)]
        public string? Terms { get; set; } = "Immediate";

        [Required]
        [MaxLength(1, ErrorMessage = "payAlone must be 'Y' or 'N'")]
        public string? payAloneFlag { get; set; } = "N";

        [MaxLength(40)]
        public string? PaymentAdviceComments { get; set; } = "";

        [MaxLength(150)]
        public string? RemittanceMessage1 { get; set; } = "";

        [MaxLength(150)]
        public string? RemittanceMessage2 { get; set; } = "";

        [MaxLength(150)]
        public string? RemittanceMessage3 { get; set; } = "";

        public string? TermsDate { get; set; } = ""; // Format: 21-FEB-2017

        [Required]
        public string? GlDate { get; set; }  // Format: 21-FEB-2017

        [Required]
        [MaxLength(50)]
        public string? InvoiceBatchName { get; set; } = "SNBATCH";

        [Required]
        [MaxLength(3)]
        public string CurrencyCode { get; set; } = "CAD";

        public List<InvoiceLineDetail>? invoiceLineDetails { get; set; }
    }
}