using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace Unity.Payments.Integrations.Cas
{
    public class Invoice
    {
        [Required]
        [MaxLength(25)]
        public string? invoiceType { get; set; } = "Standard";
        public string? poNumber { get; set; } = ""; // Must be NULL
        public string? supplierName { get; set; } = "";

        [Required]
        [MaxLength(30)]
        public string? supplierNumber { get; set; }

        [Required]
        [MaxLength(3)]
        public string? supplierSiteNumber { get; set; }

        [Required]
        public string? invoiceDate { get; set; } // Format: 21-FEB-2017

        [Required]
        [MaxLength(40)]
        public string? invoiceNumber { get; set; }

        [Required]
        public decimal invoiceAmount { get; set; } // Format: 9(12).99

        [Required]
        [MaxLength(7)]
        public string? payGroup { get; set; } = "GEN EFT"; 

        [Required]
        public string? dateInvoiceReceived { get; set; } // Format: 21-FEB-2017

        public string? dateGoodsReceived { get; set; } = "";  // Format: 21-FEB-2017

        [Required]
        [MaxLength(2)]
        public string? remittanceCode { get; set; } = "01"; // 01 - Your Invoice Reference

        [Required]
        [MaxLength(1)]
        public string? specialHandling { get; set; } = "N";

        [MaxLength(4)]
        public string? bankNumber { get; set; } = "";

        [MaxLength(5)]
        public string? branchNumber { get; set; } = "";

        [MaxLength(12)]
        public string? accountNumber { get; set; } = "";

        [MaxLength(1)]
        public string? eftAdviceFlag { get; set; }= "";

        [MaxLength(35)]
        public string? eftEmailAddress { get; set; }= "";

        [MaxLength(40)]
        public string? nameLine1 { get; set; }= "";

        [MaxLength(40)]
        public string nameLine2 { get; set; }= "";

        [MaxLength(150)]
        public string? qualifiedReceiver { get; set; } = "";

        [Required]
        [MaxLength(50)]
        public string? terms { get; set; } = "Immediate";

        [Required]
        [MaxLength(1, ErrorMessage = "payAlone must be 'Y' or 'N'")]
        public string? payAloneFlag { get; set; } = "N";

        [MaxLength(40)]
        public string? paymentAdviceComments { get; set; } = "";

        [MaxLength(150)]
        public string? remittanceMessage1 { get; set; } = "";

        [MaxLength(150)]
        public string? remittanceMessage2 { get; set; } = "";

        [MaxLength(150)]
        public string? remittanceMessage3 { get; set; } = "";

        public string? termsDate { get; set; } = ""; // Format: 21-FEB-2017

        [Required]
        public string? glDate { get; set; }  // Format: 21-FEB-2017

        [Required]
        [MaxLength(50)]
        public string? invoiceBatchName { get; set; } = "SNBATCH";

        [Required]
        [MaxLength(3)]
        public string currencyCode { get; set; } = "CAD";

        public List<InvoiceLineDetail>? invoiceLineDetails { get; set; }
    }
}