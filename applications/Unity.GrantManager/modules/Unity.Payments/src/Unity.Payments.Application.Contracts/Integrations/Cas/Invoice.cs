using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;


namespace Unity.Payments.Integrations.Cas
{
    public class Invoice
    {
        [Required]
        [MaxLength(25)]
        [JsonPropertyName("invoiceType")]
        public string? InvoiceType { get; set; } = "Standard";

        [JsonPropertyName("poNumber")]
        public string? PoNumber { get; set; } = ""; // Must be NULL

        [JsonPropertyName("supplierName")]
        public string? SupplierName { get; set; } = "";

        [Required]
        [MaxLength(30)]
        [JsonPropertyName("supplierNumber")]
        public string? SupplierNumber { get; set; }

        [Required]
        [MaxLength(3)]
        [JsonPropertyName("supplierSiteNumber")]
        public string? SupplierSiteNumber { get; set; }

        [Required]
        [JsonPropertyName("invoiceDate")]
        public string? InvoiceDate { get; set; } // Format: 21-FEB-2017

        [Required]
        [MaxLength(40)]
        [JsonPropertyName("invoiceNumber")]
        public string? InvoiceNumber { get; set; }

        [Required]
        [JsonPropertyName("invoiceAmount")]
        public decimal InvoiceAmount { get; set; } // Format: 9(12).99

        [Required]
        [MaxLength(7)]
        [JsonPropertyName("payGroup")]
        public string? PayGroup { get; set; } = "GEN EFT"; 

        [Required]
        [JsonPropertyName("dateInvoiceReceived")]
        public string? DateInvoiceReceived { get; set; } // Format: 21-FEB-2017

        [JsonPropertyName("dateGoodsReceived")]
        public string? DateGoodsReceived { get; set; } = "";  // Format: 21-FEB-2017

        [Required]
        [MaxLength(2)]
        [JsonPropertyName("remittanceCode")]
        public string? RemittanceCode { get; set; } = "01"; // 01 - Your Invoice Reference

        [Required]
        [MaxLength(1)]
        [JsonPropertyName("specialHandling")]
        public string? SpecialHandling { get; set; } = "N";

        [MaxLength(4)]
        [JsonPropertyName("bankNumber")]
        public string? BankNumber { get; set; } = "";

        [MaxLength(5)]
        [JsonPropertyName("branchNumber")]
        public string? BranchNumber { get; set; } = "";

        [MaxLength(12)]
        [JsonPropertyName("accountNumber")]
        public string? AccountNumber { get; set; } = "";

        [MaxLength(1)]
        [JsonPropertyName("eftAdviceFlag")]
        public string? EftAdviceFlag { get; set; }= "";

        [MaxLength(35)]
        [JsonPropertyName("eftEmailAddress")]
        public string? EftEmailAddress { get; set; }= "";

        [MaxLength(40)]
        [JsonPropertyName("nameLine1")]
        public string? NameLine1 { get; set; }= "";

        [MaxLength(40)]
        [JsonPropertyName("nameLine2")]
        public string NameLine2 { get; set; }= "";

        [MaxLength(150)]
        [JsonPropertyName("qualifiedReceiver")]
        public string? QualifiedReceiver { get; set; } = "";

        [Required]
        [MaxLength(50)]
        [JsonPropertyName("terms")]
        public string? Terms { get; set; } = "Immediate";

        [Required]
        [MaxLength(1, ErrorMessage = "payAlone must be 'Y' or 'N'")]
        [JsonPropertyName("payAloneFlag")]
        public string? PayAloneFlag { get; set; } = "N";

        [MaxLength(40)]
        [JsonPropertyName("paymentAdviceComments")]
        public string? PaymentAdviceComments { get; set; } = "";

        [MaxLength(150)]
        [JsonPropertyName("remittanceMessage1")]
        public string? RemittanceMessage1 { get; set; } = "";

        [MaxLength(150)]
        [JsonPropertyName("remittanceMessage2")]
        public string? RemittanceMessage2 { get; set; } = "";

        [MaxLength(150)]
        [JsonPropertyName("remittanceMessage3")]
        public string? RemittanceMessage3 { get; set; } = "";

        [JsonPropertyName("termsDate")]
        public string? TermsDate { get; set; } = ""; // Format: 21-FEB-2017

        [Required]
        [JsonPropertyName("glDate")]
        public string? GlDate { get; set; }  // Format: 21-FEB-2017

        [Required]
        [MaxLength(50)]
        [JsonPropertyName("invoiceBatchName")]
        public string? InvoiceBatchName { get; set; } = "SNBATCH";

        [Required]
        [MaxLength(3)]
        [JsonPropertyName("currencyCode")]
        public string CurrencyCode { get; set; } = "CAD";

        [JsonPropertyName("invoiceLineDetails")]
        public List<InvoiceLineDetail>? InvoiceLineDetails { get; set; }
    }
}